using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
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
    const string ArchetypeAttributeFullName = "Twee.Ecs.EcsArchetypesAttribute";

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
        public string? UsedArchetypeName { get; }
        public ImmutableArray<(string FullReturnTypeName, string Name, string RootName, ImmutableArray<(string FullTypeName, string Name)> Parameters)> Messages { get; }

        public EcsSystemClass(string name, string? usedArchetypeName,
            IEnumerable<(string FullReturnTypeName, string Name, IEnumerable<(string FullTypeName, string Name)> Parameters)> messages)
            : base(name)
        {
            UsedArchetypeName = usedArchetypeName;

            Messages = ImmutableArray.CreateRange(messages.Select(m => (m.FullReturnTypeName, m.Name, GetTypeRootName(m.Name), ImmutableArray.CreateRange(m.Parameters))));
        }
    }

    class EcsArchetypesClass : EcsClass
    {
        public ImmutableArray<(string Name, ImmutableArray<string> Components)> Archetypes { get; }

        public EcsArchetypesClass(string name, IEnumerable<(string Name, IEnumerable<string> Components)> archetypes)
            : base(name)
        {
            Archetypes = ImmutableArray.CreateRange(archetypes.Select(w => (w.Name, ImmutableArray.CreateRange(w.Components))));
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // System.Diagnostics.Debugger.Launch();

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
            string? usedArchetypeName = null;
            foreach (var attributeListSyntax in tds.AttributeLists)
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol
                        && attributeSymbol.ContainingType.ToDisplayString() is SystemAttributeFullName
                        && attributeSymbol.Parameters.Length == 1
                        && attributeSyntax.ArgumentList?.Arguments.Count == 1
                        && attributeSyntax.ArgumentList.Arguments[0] is { } argumentSyntax
                        && argumentSyntax.Expression is MemberAccessExpressionSyntax argumentExpressionSyntax
                        && argumentExpressionSyntax.Name is IdentifierNameSyntax { } argumentIdentifierNameSyntax)
                    {
                        usedArchetypeName = argumentIdentifierNameSyntax.Identifier.ValueText;
                        break;
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

            return new EcsSystemClass(className, usedArchetypeName, messages);
        });

        var archetypeDeclarations = getDeclarations(ArchetypeAttributeFullName, static (className, semanticModel, tds) =>
        {
            var archetypes = new List<(string Name, IEnumerable<string> Components)>();
            foreach (var fieldSyntax in tds.Members.OfType<FieldDeclarationSyntax>())
                foreach (var variableDeclaratorSyntax in fieldSyntax.Declaration.Variables)
                {
                    // decode a string of the form EcsComponents.Resource | EcsComponents.Location | EcsComponents.Inventory
                    // at this point we didn't inject the EcsComponents enum yet, so we can't rely on Rosly to do it for us

                    var components = new List<string>();
                    if (variableDeclaratorSyntax.Initializer?.Value is { } expression)
                    {
                        while (expression is BinaryExpressionSyntax binaryExpressionSyntax && binaryExpressionSyntax.IsKind(SyntaxKind.BitwiseOrExpression))
                        {
                            if (binaryExpressionSyntax.Right is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                                components.Add(memberAccessExpressionSyntax.Name.Identifier.Text);
                            expression = binaryExpressionSyntax.Left;
                        }
                        if (expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax0)
                            components.Add(memberAccessExpressionSyntax0.Name.Identifier.Text);
                    }

                    archetypes.Add((variableDeclaratorSyntax.Identifier.ValueText, components));
                }

            return new EcsArchetypesClass(className, archetypes);
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

                    public static Entity Invalid = new() { ID = -1 };

                    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
                    public bool Equals(Entity other) => ID == other.ID;
                    public static bool operator ==(Entity a, Entity b) => a.Equals(b);
                    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
                    public override int GetHashCode() => ID;

                    public static implicit operator int(Entity entity) => entity.ID;
                }
                
                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Class)]
                internal sealed class EcsSystemAttribute : Attribute
                {
                    public EcsSystemAttribute(EcsComponents components) { }
                }
                
                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class MessageAttribute : Attribute
                {
                }
                
                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Class)]
                internal sealed class EcsArchetypesAttribute : Attribute
                {
                }

                {{Common.GeneratedCodeAttributeText}}
                internal static partial class EcsCoordinator
                {
                    static int maxGeneratedEntityID;
                    static readonly SortedSet<int> extraAvailableEntityIDs = new();
                    static readonly List<EcsComponents> entityComponents = new();

                    static readonly HashSet<Entity> entities = new();
                    public static IReadOnlyCollection<Entity> Entities { get; } = entities;

                    static void EnsureEntityComponentsListEntityExists(Entity entity)
                    {
                        int idx = entity;
                        while(entityComponents.Count <= idx)
                            entityComponents.Add(0);
                    }
                }

                class EntityJsonConverter : JsonConverter<Entity>
                {
                    public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                    {
                        if (!reader.Read())
                            throw new JsonException();
                        return new() { ID = reader.GetInt32() };
                    }
                    
                    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options) =>
                        writer.WriteNumberValue(value.ID);
                }
                """);
            });

        context.RegisterSourceOutput(componentDeclarations.Collect().Combine(systemDeclarations.Collect()).Combine(archetypeDeclarations.Collect()),
            static (spc, input) =>
        {
            //Debugger.Launch();

            var sb = new StringBuilder();

            var components = input.Left.Left;
            var systems = input.Left.Right;
            var rawArchetypes = input.Right.FirstOrDefault();
            var archetypes = rawArchetypes?.Archetypes.Select(at => (at.Name,
                Components: at.Components.Select(c => components.First(w => w.TypeRootName == c)).ToList())).ToList();

            static int findIndex<T>(IEnumerable<T> enumerable, Func<T, bool> test)
            {
                int index = 0;
                foreach (var item in enumerable)
                    if (!test(item))
                        ++index;
                    else
                        return index;
                return -1;
            }

            ulong getComponentsRawValue(IEnumerable<string> selectedComponents)
            {
                ulong result = 0;
                foreach (var component in selectedComponents)
                    result += 1ul << findIndex(components, w => w.TypeRootName == component);
                return result;
            }

            sb.AppendLine($$"""
                #nullable enable

                // systems
                {{string.Join(Environment.NewLine, systems.Select(s => $$"""
                    {{(s!.Namespace is not null ? $"namespace {s!.Namespace} {{" : "")}}
                    sealed partial class {{s!.TypeName}}
                    {
                        internal HashSet<Entity> Entities { get; } = 
                            EcsCoordinator.{{s.UsedArchetypeName}}Entities;

                        readonly ref struct IterationResult
                        {
                            public readonly Entity Entity;
                            {{string.Join(Environment.NewLine, archetypes.First(at => at.Name == s.UsedArchetypeName).Components.Select(c => $$"""
                                public readonly ref {{c!.FullName}} {{c!.TypeRootName}}Component;
                                """))}}

                            public IterationResult(Entity entity {{string.Concat(archetypes.First(at => at.Name == s.UsedArchetypeName).Components.Select(c =>
                                $", ref {c!.FullName} {c!.TypeRootName}Component"))}})
                            {
                                Entity = entity;
                                {{string.Join(Environment.NewLine, archetypes.First(at => at.Name == s.UsedArchetypeName).Components.Select(c => $$"""
                                    this.{{c!.TypeRootName}}Component = ref {{c!.TypeRootName}}Component;
                                    """))}}
                            }
                        }
                        delegate void IterateComponentsProcessDelegate(in IterationResult iterationResult);

                        void IterateComponents(IterateComponentsProcessDelegate process)
                        {
                            foreach(var entity in Entities)
                                process(new(entity
                                    {{string.Concat(archetypes.First(at => at.Name == s.UsedArchetypeName).Components.Select(c => $", ref EcsCoordinator.Get{c!.TypeRootName}Component(entity)"))}}
                                ));
                        }

                        public partial void Run();
                    }
                    {{(s!.Namespace is not null ? "}" : "")}}
                    """))}}

                namespace Twee.Ecs {

                {{Common.GeneratedCodeAttributeText}}
                [Flags]
                internal enum EcsComponents : ulong { {{string.Join(", ",
                    components.Select((c, idx) => $"{c!.TypeRootName} = 1ul << {idx}"))}} }

                internal static partial class EcsCoordinator
                {
                    static EcsMapping entityComponentMapping = new({{components.Length}});
                    
                    // components
                    {{string.Join(Environment.NewLine, components.Select(c => $$"""
                        static readonly List<{{c!.FullName}}> {{c!.TypeName}}s = new();
                        static readonly SortedSet<int> extraAvailable{{c!.TypeName}}IDs = new();
                        """))}}
                        
                    // archetypes
                    {{(rawArchetypes is null ? null : string.Join(Environment.NewLine, rawArchetypes.Archetypes.Select(at => $$"""
                        internal static readonly HashSet<Entity> {{at.Name}}Entities = new();

                        internal readonly ref struct {{at.Name}}IterationResult
                        {
                            public readonly Entity Entity;
                            {{string.Join(Environment.NewLine, archetypes.First(at2 => at2.Name == at.Name).Components.Select(c => $$"""
                                public readonly ref {{c!.FullName}} {{c!.TypeRootName}}Component;
                                """))}}

                            public {{at.Name}}IterationResult(Entity entity {{string.Concat(archetypes.First(at2 => at2.Name == at.Name).Components.Select(c =>
                                $", ref {c!.FullName} {c!.TypeRootName}Component"))}})
                            {
                                Entity = entity;
                                {{string.Join(Environment.NewLine, archetypes.First(at2 => at2.Name == at.Name).Components.Select(c => $$"""
                                    this.{{c!.TypeRootName}}Component = ref {{c!.TypeRootName}}Component;
                                    """))}}
                            }
                        }

                        internal delegate bool Iterate{{at.Name}}ArchetypeProcessDelegateWithInterruption(in {{at.Name}}IterationResult iterationResult);
                        public static void Iterate{{at.Name}}Archetype(Iterate{{at.Name}}ArchetypeProcessDelegateWithInterruption process)
                        {
                            foreach(var entity in {{at.Name}}Entities)
                                if(!process(new(entity
                                    {{string.Concat(archetypes.First(at2 => at2.Name == at.Name).Components.Select(c => $", ref EcsCoordinator.Get{c!.TypeRootName}Component(entity)"))}}
                                )))
                                {
                                    break;
                                }
                        }
                        
                        internal delegate void Iterate{{at.Name}}ArchetypeProcessDelegate(in {{at.Name}}IterationResult iterationResult);
                        public static void Iterate{{at.Name}}Archetype(Iterate{{at.Name}}ArchetypeProcessDelegate process)
                        {
                            foreach(var entity in {{at.Name}}Entities)
                                process(new(entity
                                    {{string.Concat(archetypes.First(at2 => at2.Name == at.Name).Components.Select(c => $", ref EcsCoordinator.Get{c!.TypeRootName}Component(entity)"))}}
                                ));
                        }
                        """)))}}

                    public static Entity CreateEntity()
                    {
                        if (extraAvailableEntityIDs.Count > 0)
                        {
                            var id = extraAvailableEntityIDs.Min;
                            extraAvailableEntityIDs.Remove(id);
                
                            var entity = new Entity() { ID = id };
                
                            EnsureEntityComponentsListEntityExists(entity);
                            entityComponents[entity] = 0;
                            {{string.Join(Environment.NewLine, components.Select(c => $$"""
                                entityComponentMapping[entity, {{findIndex(components, w => w == c)}}] = -1;
                                """))}}
                            
                            entities.Add(entity);
                            return entity;
                        }
                
                        var entity0 = new Entity() { ID = maxGeneratedEntityID++ };
                
                        EnsureEntityComponentsListEntityExists(entity0);
                        entityComponents[entity0] = 0;
                        {{string.Join(Environment.NewLine, components.Select(c => $$"""
                            entityComponentMapping[entity0, {{findIndex(components, w => w == c)}}] = -1;
                            """))}}
                        
                        entities.Add(entity0);
                        return entity0;
                    }

                    public static bool DeleteEntity(Entity entity)
                    {
                        extraAvailableEntityIDs.Add(entity);
                        return entities.Remove(entity);
                    }

                    // component functions
                    {{string.Join(Environment.NewLine, components.Select(c => $$"""
                        public static ref {{c!.FullName}} Add{{c!.TypeRootName}}Component(Entity entity)
                        {
                            int componentId;
                            if (extraAvailable{{c!.TypeName}}IDs.Count > 0)
                            {
                                componentId = extraAvailable{{c!.TypeName}}IDs.Min;
                                extraAvailable{{c!.TypeName}}IDs.Remove(componentId);
                                {{c!.TypeName}}s[(int)componentId] = new();
                                entityComponentMapping[entity, {{findIndex(components, w => w == c)}}] = componentId;
                                entityComponents[entity] |= EcsComponents.{{c!.TypeRootName}};
                            }
                            else
                            {
                                entityComponentMapping[entity, {{findIndex(components, w => w == c)}}] = componentId = {{c!.TypeName}}s.Count;
                                entityComponents[entity] |= EcsComponents.{{c!.TypeRootName}};
                                {{c!.TypeName}}s.Add(new());
                            }

                            {{(rawArchetypes is null ? null : string.Join(Environment.NewLine, rawArchetypes.Archetypes.Select(at =>
                                !at.Components.Contains(c.TypeRootName) ? null : $$"""
                                    if((((ulong)entityComponents[entity]) & {{getComponentsRawValue(at.Components)}}) == {{getComponentsRawValue(at.Components)}})
                                        {{at.Name}}Entities.Add(entity);
                                    """)))}}

                            return ref CollectionsMarshal.AsSpan({{c!.TypeName}}s)[componentId]; 
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
                            entityComponents[entity].HasFlag(EcsComponents.{{c!.TypeRootName}});

                        public static ref {{c!.FullName}} Get{{c!.TypeRootName}}Component(Entity entity) 
                        {
                            return ref CollectionsMarshal.AsSpan({{c!.TypeName}}s)[entityComponentMapping[entity, {{findIndex(components, w => w == c)}}]];
                        }
                        
                        public static void Remove{{c!.TypeRootName}}Component(Entity entity) 
                        {
                            entityComponentMapping[entity, {{findIndex(components, w => w == c)}}] = -1;
                            entityComponents[entity] &= ~EcsComponents.{{c!.TypeRootName}};

                            {{(rawArchetypes is null ? null : string.Join(Environment.NewLine, rawArchetypes.Archetypes.Select(at =>
                                !at.Components.Contains(c.TypeRootName) ? null : $$"""
                                    {{at.Name}}Entities.Remove(entity);
                                    """)))}}
                        }
                        """))}}

                    // system data
                    {{string.Join(Environment.NewLine, systems.Select(s => $$"""
                        static {{s!.FullName}}? {{s!.TypeRootName}}System;
                        public static void Construct{{s!.TypeRootName}}System(Func<{{s!.FullName}}> generator) => 
                            {{s!.TypeRootName}}System = generator();

                        public static bool Is{{s!.TypeRootName}}SystemConstructed => {{s!.TypeRootName}}System is not null;

                        {{string.Join(Environment.NewLine, s.Messages.Select(m => $$"""
                            public static {{m.FullReturnTypeName}} Send{{m.RootName}}MessageTo{{s.TypeRootName}}System(
                                {{string.Join(", ", m.Parameters.Select(p => $"{p.FullTypeName} {p.Name}"))}}) =>
                                {{s!.TypeRootName}}System!.{{m.Name}}({{string.Join(", ", m.Parameters.Select(p => p.Name))}});
                            """))}}
                        """))}}

                    public static void RunSystems()
                    {
                        {{string.Join(Environment.NewLine, systems.Select(s => $$"""
                            {{s!.TypeRootName}}System?.Run();
                            """))}}
                    }

                    public static EcsDataDump DumpAllData() 
                    {
                        var dump = new EcsDataDump { MaxGeneratedEntityID = maxGeneratedEntityID };
                        dump.ExtraAvailableEntitiesIDs.AddRange(extraAvailableEntityIDs);

                        foreach(var entity in Entities)
                        {
                            {{string.Join(Environment.NewLine, components.Select(c => $$"""
                                if(Has{{c!.TypeRootName}}Component(entity))
                                    dump.{{c!.TypeRootName}}s.Add((entity, Get{{c!.TypeRootName}}Component(entity)));
                                """))}}
                        }

                        return dump;
                    }
                }

                internal class EcsDataDump
                {
                    public int MaxGeneratedEntityID { get; set; }
                    public List<int> ExtraAvailableEntitiesIDs { get; } = new();
                    {{string.Join(Environment.NewLine, components.Select(c => $$"""
                        public List<(Entity Entity, {{c!.FullName}} Component)> {{c!.TypeRootName}}s { get; } = new();
                        """))}}
                }
                }
                """);

            spc.AddSource("ECS.g.cs", sb.ToString());
        });
    }
}
