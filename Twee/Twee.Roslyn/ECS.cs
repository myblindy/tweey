using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Twee.Roslyn;

[Generator]
public sealed class ECSSourceGen : IIncrementalGenerator
{
    const string TweeCoreAssemblyName = "Twee.Core";
    const string ComponentAttributeFullName = "Twee.Ecs.EcsComponentAttribute";
    const string SystemAttributeFullName = "Twee.Ecs.EcsSystemAttribute";
    const string MessageAttributeFullName = "Twee.Ecs.MessageAttribute";

    static readonly Regex typeRootNameRegex = new(@"^.*?([^.]+?)(?:Component|System|Message)?$");
    static string GetTypeRootName(string name) =>
        typeRootNameRegex.Match(name) is { Success: true } m ? m.Groups[1].Value : name;

    class EcsClass
    {
        public string FullName { get; }
        public string TypeRootName { get; }
        public string TypeName { get; }
        public string? Namespace { get; }

        public EcsClass(string name)
        {
            (FullName, TypeRootName) = (name, GetTypeRootName(name));

            (Namespace, TypeName) = Regex.Match(name, @"^(?:(.*?)\.)?([^.]+)$") is { Success: true } m2
                ? (m2.Groups[1].Value, m2.Groups[2].Value) : (null, name);
        }
    }

    class EcsComponentClass : EcsClass
    {
        public ImmutableArray<(string FullTypeName, string Name, string? Default)> Parameters { get; }

        public EcsComponentClass(string name, IEnumerable<(string FullTypeName, string Name, string? Default)> parameters)
            : base(name)
        {
            Parameters = ImmutableArray.CreateRange(parameters);
        }
    }

    class EcsSystemClass : EcsClass
    {
        public ImmutableHashSet<string> UsedComponents { get; }
        public ImmutableArray<(string FullReturnTypeName, string Name, string RootName, ImmutableArray<(string FullTypeName, string Name)> Parameters)> Messages { get; }

        public EcsSystemClass(string name, IEnumerable<string> usedComponents,
            IEnumerable<(string FullReturnTypeName, string Name, IEnumerable<(string FullTypeName, string Name)> Parameters)> messages)
            : base(name)
        {
            UsedComponents = ImmutableHashSet.CreateRange(usedComponents.Select(c =>
                typeRootNameRegex.Match(c) is { Success: true } match ? match.Groups[1].Value : c));

            Messages = ImmutableArray.CreateRange(messages.Select(m => (m.FullReturnTypeName, m.Name, GetTypeRootName(m.Name), ImmutableArray.CreateRange(m.Parameters))));
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();

        IncrementalValuesProvider<TEcsClass> getDeclarations<TEcsClass>(string fullAttributeTypeName, Func<string, SemanticModel, TypeDeclarationSyntax, TEcsClass> generator)
            where TEcsClass : EcsClass =>
                context.SyntaxProvider
                    .CreateSyntaxProvider(
                        static (node, _) => (node.IsKind(SyntaxKind.StructDeclaration) || node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.RecordStructDeclaration))
                            && ((TypeDeclarationSyntax)node).AttributeLists.Count > 0,
                        (ctx, _) =>
                        {
                            // Debugger.Launch();

                            var structDeclarationSyntax = (TypeDeclarationSyntax)ctx.Node;

                            if (ctx.SemanticModel.GetDeclaredSymbol(structDeclarationSyntax) is { } structDeclarationSymbol)
                                foreach (var attributeListSyntax in structDeclarationSyntax.AttributeLists)
                                    foreach (var attributeSyntax in attributeListSyntax.Attributes)
                                        if (ctx.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol
                                            && attributeSymbol.ContainingType.ToDisplayString() == fullAttributeTypeName)
                                        {
                                            return generator(structDeclarationSymbol.ToDisplayString(), ctx.SemanticModel, structDeclarationSyntax);
                                        }

                            return null;
                        })
                    .Where(static m => m is not null)
                    .Select(static (m, _) => m!);

        var componentDeclarations = getDeclarations(ComponentAttributeFullName, static (structName, semanticModel, sds) =>
        {
            static string fixDefaultValue(object? s) => s switch
            {
                true => "true",
                false => "false",
                null => "default",
                _ => s.ToString()
            };

            var constructorParameters = new List<(string FullTypeName, string Name, string? Default)>();
            if (semanticModel.GetDeclaredSymbol(sds) is { } classSymbol)
                if (classSymbol.Constructors.OrderByDescending(w => w.Parameters.Length).FirstOrDefault()?.Parameters is { } parameterSymbols)
                    foreach (var parameterSymbol in parameterSymbols)
                        constructorParameters.Add((parameterSymbol.Type.ToDisplayString(), parameterSymbol.Name,
                            parameterSymbol.HasExplicitDefaultValue ? fixDefaultValue(parameterSymbol.ExplicitDefaultValue) : null));

            return new EcsComponentClass(structName, constructorParameters);
        });

        var systemDeclarations = getDeclarations(SystemAttributeFullName, static (className, semanticModel, tds) =>
        {
            var components = new List<string>();
            foreach (var attributeListSyntax in tds.AttributeLists)
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol
                        && Regex.Match(attributeSymbol.ContainingType.ToDisplayString(), @"^Twee\.Ecs\.UsesAttribute<(.*)>$") is { Success: true } match)
                    {
                        components.Add(match.Groups[1].Value);
                    }

            var messages = new List<(string FullReturnTypeName, string Name, IEnumerable<(string FullTypeName, string Name)> Parameters)>();
            if (semanticModel.GetDeclaredSymbol(tds) is { } classSymbol)
                foreach (var memberSymbol in classSymbol.GetMembers().OfType<IMethodSymbol>())
                    foreach (var attribute in memberSymbol.GetAttributes())
                        if (attribute.AttributeClass?.ToDisplayString() is MessageAttributeFullName)
                        {
                            messages.Add((memberSymbol.ReturnType.ToDisplayString(), memberSymbol.Name,
                                memberSymbol.Parameters.Select(p => (p.Type.ToDisplayString(), p.Name))));
                            break;
                        }

            return new EcsSystemClass(className, components, messages);
        });

        context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource("ECS.gg.cs", $$"""
                namespace Twee.Ecs;

                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Struct)]
                internal sealed class EcsComponentAttribute : Attribute
                {
                }

                {{Common.GeneratedCodeAttributeText}}
                internal readonly struct Entity : IEquatable<Entity>
                {
                    public required int ID { get; init; }

                    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
                    public bool Equals(Entity other) => ID == other.ID;
                    public override int GetHashCode() => ID;
                }
                
                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Class)]
                internal sealed class EcsSystemAttribute : Attribute
                {
                }
                
                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                internal sealed class UsesAttribute<T> : Attribute
                {
                }

                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class MessageAttribute : Attribute
                {
                }
                
                {{Common.GeneratedCodeAttributeText}}
                internal static partial class EcsCoordinator
                {
                    static int maxGeneratedEntityID;
                    static readonly SortedSet<int> extraAvailableEntityIDs = new();
                    static readonly Dictionary<(Entity, Twee.Ecs.Components), int> entityComponentMapping = new();
                }
                """);
            });

        context.RegisterSourceOutput(componentDeclarations.Collect().Combine(systemDeclarations.Collect()), static (spc, input) =>
        {
            //Debugger.Launch();

            var sb = new StringBuilder();

            var components = input.Left;
            var systems = input.Right;

            sb.AppendLine($$"""
                {{string.Join(Environment.NewLine, systems.Select(s => $$"""
                    {{(s!.Namespace is not null ? $"namespace {s!.Namespace} {{" : "")}}
                    partial class {{s!.TypeName}}
                    {
                        internal HashSet<Entity> Entities { get; } = new();

                        readonly ref struct IterationResult
                        {
                            public readonly Entity Entity;
                            {{string.Join(Environment.NewLine, components.Select(c => $$"""
                                public readonly ref {{c!.FullName}} {{c!.TypeRootName}}Component;
                                """))}}

                            public IterationResult(Entity entity {{string.Concat(components.Select(c =>
                                $", ref {c!.FullName} {c!.TypeRootName}Component"))}})
                            {
                                Entity = entity;
                                {{string.Join(Environment.NewLine, components.Select(c => $$"""
                                    this.{{c!.TypeRootName}}Component = ref {{c!.TypeRootName}}Component;
                                    """))}}
                            }
                        }
                        delegate void IterateComponentsProcessDelegate(in IterationResult iterationResult);

                        void IterateComponents(IterateComponentsProcessDelegate process)
                        {
                            foreach(var entity in Entities)
                                process(new(entity
                                    {{string.Concat(components.Select(c => $", ref EcsCoordinator.Get{c!.TypeRootName}Component(entity)"))}}
                                ));
                        }
                    }
                    {{(s!.Namespace is not null ? "}" : "")}}
                    """))}}

                namespace Twee.Ecs {

                {{Common.GeneratedCodeAttributeText}}
                [Flags]
                internal enum Components: ulong { {{string.Join(", ",
                    components.Select((c, idx) => $"{c!.TypeRootName} = 1ul << {idx}"))}} }

                internal static partial class EcsCoordinator
                {
                    {{string.Join(Environment.NewLine, components.Select(c => $$"""
                        static readonly List<{{c!.FullName}}> {{c!.TypeName}}s = new();
                        static readonly SortedSet<int> extraAvailable{{c!.TypeName}}IDs = new();
                        """))}}

                    public static Entity CreateEntity()
                    {
                        if (extraAvailableEntityIDs.Count > 0)
                        {
                            var id = extraAvailableEntityIDs.Min;
                            extraAvailableEntityIDs.Remove(id);
                
                            var entity = new Entity() { ID = id };
                
                            {{string.Join(Environment.NewLine, components.Select(c => $$"""
                                entityComponentMapping.Remove((entity, Twee.Ecs.Components.{{c!.TypeRootName}}));
                                """))}}
                                
                            return entity;
                        }
                
                        var entity0 = new Entity() { ID = maxGeneratedEntityID++ };
                
                        {{string.Join(Environment.NewLine, components.Select(c => $$"""
                            entityComponentMapping.Remove((entity0, Twee.Ecs.Components.{{c!.TypeRootName}}));
                            """))}}
                                
                        return entity0;
                    }

                    {{string.Join(Environment.NewLine, components.Select(c => $$"""
                        public static ref {{c!.FullName}} Add{{c!.TypeRootName}}Component(Entity entity)
                        {
                            if (extraAvailable{{c!.TypeName}}IDs.Count > 0)
                            {
                                var componentId = extraAvailable{{c!.TypeName}}IDs.Min;
                                extraAvailable{{c!.TypeName}}IDs.Remove(componentId);
                                {{c!.TypeName}}s[(int)componentId] = new();
                                entityComponentMapping[(entity, Twee.Ecs.Components.{{c!.TypeRootName}})] = componentId;
                            }
                            else
                            {
                                entityComponentMapping[(entity, Twee.Ecs.Components.{{c!.TypeRootName}})] = {{c!.TypeName}}s.Count;
                                {{c!.TypeName}}s.Add(new());
                            }

                            {{string.Join(Environment.NewLine, systems.Select(s => !s.UsedComponents.Contains(c.TypeRootName) ? null : $$"""
                                if(true {{string.Concat(s.UsedComponents.Select(uc => $" && Has{uc}Component(entity)"))}})
                                    {{s!.TypeRootName}}System.Entities.Add(entity);
                                """))}}

                            return ref CollectionsMarshal.AsSpan({{c!.TypeName}}s)[{{c!.TypeName}}s.Count]; 
                        }

                        {{(c.Parameters.Length == 0 ? "" : $$"""
                            public static ref {{c!.FullName}} Add{{c!.TypeRootName}}Component(Entity entity,
                                {{string.Join(", ", c.Parameters.Select(p =>
                                    $"{p.FullTypeName} {p.Name} {(p.Default is null ? "" : $" = {p.Default}")}"))}})
                            {
                                ref var component = ref Add{{c!.TypeRootName}}Component(entity);
                                {{string.Join(Environment.NewLine, c.Parameters.Select(p => $"component.{p.Name} = {p.Name};"))}}
                                return ref component;
                            }
                            """)}}

                        public static bool Has{{c!.TypeRootName}}Component(Entity entity) =>
                            entityComponentMapping.ContainsKey((entity, Twee.Ecs.Components.{{c!.TypeRootName}}));

                        public static ref {{c!.FullName}} Get{{c!.TypeRootName}}Component(Entity entity) 
                        {
                            if(!entityComponentMapping.TryGetValue((entity, Twee.Ecs.Components.{{c!.TypeRootName}}), out var componentId))
                                throw new InvalidOperationException();
                            return ref CollectionsMarshal.AsSpan({{c!.TypeName}}s)[componentId];
                        }
                        
                        public static bool Remove{{c!.TypeRootName}}Component(Entity entity) 
                        {
                            var result = entityComponentMapping.Remove((entity, Twee.Ecs.Components.{{c!.TypeRootName}}));

                            {{string.Join(Environment.NewLine, systems.Select(s => !s.UsedComponents.Contains(c.TypeRootName) ? null : $$"""
                                {{s!.TypeRootName}}System.Entities.Remove(entity);
                                """))}}

                            return result;
                        }
                        """))}}

                    {{string.Join(Environment.NewLine, systems.Select(s => $$"""
                        static {{s!.FullName}} {{s!.TypeRootName}}System;
                        public static void Construct{{s!.TypeRootName}}System(Func<{{s!.FullName}}> generator) => 
                            {{s!.TypeRootName}}System = generator();

                        {{string.Join(Environment.NewLine, s.Messages.Select(m => $$"""
                            public static {{m.FullReturnTypeName}} Send{{m.RootName}}MessageTo{{s.TypeRootName}}System(
                                {{string.Join(", ", m.Parameters.Select(p => $"{p.FullTypeName} {p.Name}"))}}) =>
                                {{s!.TypeRootName}}System.{{m.Name}}({{string.Join(", ", m.Parameters.Select(p => p.Name))}});
                            """))}}
                        """))}}

                    public static void RunSystems(double deltaSec, double updateDeltaSec, double renderDeltaSec)
                    {
                        {{string.Join(Environment.NewLine, systems.Select(s => $$"""
                            {{s!.TypeRootName}}System?.Run(deltaSec, updateDeltaSec, renderDeltaSec);
                            """))}}
                    }
                }
                }
                """);

            spc.AddSource("ECS.g.cs", sb.ToString());
        });
    }
}
