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
    const string UsesAttributeFullName = "Twee.Ecs.UsesAttribute";

    static readonly Regex typeRootNameRegex = new(@"^.*?([^.]+?)(?:Component|System)?$");

    class EcsClass
    {
        public string FullName { get; }
        public string TypeRootName { get; }
        public string TypeName { get; }
        public string? Namespace { get; }

        public EcsClass(string name)
        {
            (FullName, TypeRootName) =
                (name, typeRootNameRegex.Match(name) is { Success: true } m ? m.Groups[1].Value : name);

            (Namespace, TypeName) = Regex.Match(name, @"^(?:(.*?)\.)?([^.]+)$") is { Success: true } m2
                ? (m2.Groups[1].Value, m2.Groups[2].Value) : (null, name);
        }
    }

    class EcsSystemClass : EcsClass
    {
        public ImmutableHashSet<string> UsedComponents { get; }

        public EcsSystemClass(string name, IEnumerable<string> usedComponents) : base(name) =>
            UsedComponents = ImmutableHashSet.CreateRange(usedComponents.Select(c =>
                typeRootNameRegex.Match(c) is { Success: true } match ? match.Groups[1].Value : c));
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Debugger.Launch();

        IncrementalValuesProvider<TEcsClass> getDeclarations<TEcsClass>(string fullAttributeTypeName, Func<string, SemanticModel, IEnumerable<AttributeListSyntax>, TEcsClass> generator)
            where TEcsClass : EcsClass =>
                context.SyntaxProvider
                    .CreateSyntaxProvider(
                        static (node, _) => (node.IsKind(SyntaxKind.StructDeclaration) || node.IsKind(SyntaxKind.ClassDeclaration))
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
                                            return generator(structDeclarationSymbol.ToDisplayString(), ctx.SemanticModel, structDeclarationSyntax.AttributeLists);
                                        }

                            return null;
                        })
                    .Where(static m => m is not null)
                    .Select(static (m, _) => m!);

        var componentDeclarations = getDeclarations(ComponentAttributeFullName, static (structName, _, _) => new EcsClass(structName));
        var systemDeclarations = getDeclarations(SystemAttributeFullName, static (className, semanticModel, attrs) =>
        {
            var components = new List<string>();
            foreach (var attributeListSyntax in attrs)
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol
                        && Regex.Match(attributeSymbol.ContainingType.ToDisplayString(), @"^Twee\.Ecs\.UsesAttribute<(.*)>$") is { Success: true } match)
                    {
                        components.Add(match.Groups[1].Value);
                    }

            return new EcsSystemClass(className, components);
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
                [AttributeUsage(AttributeTargets.Class)]
                internal sealed class UsesAttribute<T> : Attribute
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
                                this.Entity = Entity;
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
                        public static void Add{{c!.TypeRootName}}Component(Entity entity)
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
                        }

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
                        """))}}

                    public static void RunSystems(double deltaSec)
                    {
                        {{string.Join(Environment.NewLine, systems.Select(s => $$"""
                            {{s!.TypeRootName}}System?.Run(deltaSec);
                            """))}}
                    }
                }
                }
                """);

            spc.AddSource("ECS.g.cs", sb.ToString());
        });
    }
}
