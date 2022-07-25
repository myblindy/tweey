using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace tweey.roslyn;

[Generator]
public class VertexSourceGen : IIncrementalGenerator
{
    internal static readonly DiagnosticDescriptor InvalidFieldTypeDiagnosticDescriptor =
        new("MBVS001", "InvalidField", "Invalid field type for vertex definition type {0}", "Functionality", DiagnosticSeverity.Error, true);
    internal static readonly DiagnosticDescriptor NoPackingDiagnosticDescriptor =
        new("MBVS002", "NoPacking", "No packing defined for vertex definition", "Functionality", DiagnosticSeverity.Error, true);

    public static string GetFullName(TypeDeclarationSyntax source)
    {
        var items = new List<string>();
        var parent = source.Parent;
        while (parent.IsKind(SyntaxKind.ClassDeclaration) || parent.IsKind(SyntaxKind.StructDeclaration))
        {
            var parentClass = parent as TypeDeclarationSyntax;
            items.Add(parentClass.Identifier.Text);
            parent = parent.Parent;
        }

        var sb = new StringBuilder();
        if (parent is BaseNamespaceDeclarationSyntax baseNamespaceDeclarationSyntax)
            sb.Append(baseNamespaceDeclarationSyntax.Name).Append('.');

        items.Reverse();
        items.ForEach(i => sb.Append(i).Append('.'));
        sb.Append(source.Identifier.Text);

        return sb.ToString();
    }

    static bool GetTypeDetails(string typeName, out int fieldCount, out string fieldType, out int byteSize)
    {
        switch (typeName)
        {
            case "System.Single":
                (fieldCount, fieldType, byteSize) = (1, "VertexAttribType.Float", sizeof(float));
                return true;
            case "System.Numerics.Vector2":
                (fieldCount, fieldType, byteSize) = (2, "VertexAttribType.Float", 2 * sizeof(float));
                return true;
            case "System.Numerics.Vector3":
                (fieldCount, fieldType, byteSize) = (3, "VertexAttribType.Float", 3 * sizeof(float));
                return true;
            case "System.Numerics.Vector4":
                (fieldCount, fieldType, byteSize) = (4, "VertexAttribType.Float", 4 * sizeof(float));
                return true;
        };

        (fieldCount, fieldType, byteSize) = (default, default, default);
        return false;
    }

    public class VertexStructure
    {
        public StructDeclarationSyntax StructDeclarationSyntax { get; internal set; }
        public bool PackingAttributeCorrect { get; internal set; }
        public List<(int fieldCount, string fieldType, int byteSize, bool error, Location location)> Fields { get; } = new();
    }

    public static IEnumerable<VertexStructure> EnumerateVertexStructures(SyntaxNode rootNode, SemanticModel semanticModel)
    {
        foreach (StructDeclarationSyntax sd in rootNode.DescendantNodes().Where(n => n.IsKind(SyntaxKind.StructDeclaration)))
            if (sd.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name is IdentifierNameSyntax ins && ins.Identifier.Text is "VertexDefinition" or "VertexDefinitionAttribute"))
            {
                var result = new VertexStructure { StructDeclarationSyntax = sd };

                // ensure this structure is packed
                result.PackingAttributeCorrect = sd.AttributeLists.SelectMany(a => a.Attributes)
                    .Any(a => a.Name is NameSyntax ns
                        && semanticModel.GetSymbolInfo(ns).Symbol is IMethodSymbol methodSymbol && methodSymbol.ReceiverType.ToDisplayString() is "System.Runtime.InteropServices.StructLayoutAttribute"
                        && a.ArgumentList.Arguments.Count >= 2
                        && a.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax mes0
                        && semanticModel.GetSymbolInfo(mes0.Expression).Symbol is ITypeSymbol mes0TypeSymbol && mes0TypeSymbol.ToDisplayString() is "System.Runtime.InteropServices.LayoutKind"
                        && mes0.Name is IdentifierNameSyntax mes0ins && mes0ins.Identifier.Text is "Sequential"
                        && a.ArgumentList.Arguments.Any(w => w.NameEquals?.Name.Identifier.Text is "Pack" && semanticModel.GetConstantValue(w.Expression).Value is 1));

                foreach (FieldDeclarationSyntax field in sd.Members.Where(n => n.IsKind(SyntaxKind.FieldDeclaration)))
                    if (semanticModel.GetSymbolInfo(field.Declaration.Type).Symbol is INamedTypeSymbol namedTypeSymbol)
                    {
                        var fullTypeName = namedTypeSymbol.ToDisplayString();
                        var error = !GetTypeDetails(fullTypeName, out var fieldCount, out var fieldType, out var byteSize);
                        result.Fields.Add((fieldCount, fieldType, byteSize, error, field.Declaration.Type.GetLocation()));
                    }

                yield return result;
            }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();

        var inputs = context.CompilationProvider;
        context.RegisterSourceOutput(inputs, static (context, compilationProvider) =>
        {
            var sb = new StringBuilder($$"""
                using System;
                using OpenTK.Graphics.OpenGL;

                {{Common.GeneratedCodeAttributeText}}
                [AttributeUsage(AttributeTargets.Struct)]
                public sealed class VertexDefinitionAttribute: Attribute { }

                {{Common.GeneratedCodeAttributeText}}
                public static class VertexDefinitionSetup
                {
                    public static void Setup(Type t, VertexArrayHandle va) 
                    {
                """);

            foreach (var tree in compilationProvider.SyntaxTrees)
                if (tree.TryGetRoot(out var rootNode))
                {
                    var semanticModel = compilationProvider.GetSemanticModel(tree);

                    foreach (var vertexStructure in EnumerateVertexStructures(rootNode, semanticModel))
                    {
                        var fullName = GetFullName(vertexStructure.StructDeclarationSyntax);
                        sb.AppendLine($"if(t == typeof({fullName})) {{");
                        int idx = 0;
                        var offset = 0;
                        foreach (var (fieldCount, fieldType, byteSize, error, location) in vertexStructure.Fields)
                            if (!error)
                            {
                                sb.AppendLine($"GL.EnableVertexArrayAttrib(va, {idx});");
                                sb.AppendLine($"GL.VertexArrayAttribFormat(va, {idx}, {fieldCount}, {fieldType}, false, {offset});");
                                offset += byteSize;
                                sb.AppendLine($"GL.VertexArrayAttribBinding(va, {idx}, 0);");

                                ++idx;
                            }
                        sb.AppendLine("}");
                    }
                }

            sb.AppendLine("}}");
            context.AddSource("VertexSourceGen.cs", sb.ToString());
        });
    }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VertexAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(VertexSourceGen.NoPackingDiagnosticDescriptor, VertexSourceGen.InvalidFieldTypeDiagnosticDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        //Debugger.Launch();

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSemanticModelAction(semanticModelAnalysisContext =>
        {
            foreach (var vertexStructure in VertexSourceGen.EnumerateVertexStructures(semanticModelAnalysisContext.SemanticModel.SyntaxTree.GetRoot(), semanticModelAnalysisContext.SemanticModel))
            {
                if (!vertexStructure.PackingAttributeCorrect)
                    semanticModelAnalysisContext.ReportDiagnostic(Diagnostic.Create(VertexSourceGen.NoPackingDiagnosticDescriptor, vertexStructure.StructDeclarationSyntax.Identifier.GetLocation()));
                foreach (var (fieldCount, fieldType, byteSize, error, location) in vertexStructure.Fields)
                    if (error)
                        semanticModelAnalysisContext.ReportDiagnostic(Diagnostic.Create(VertexSourceGen.InvalidFieldTypeDiagnosticDescriptor, location, vertexStructure.StructDeclarationSyntax.Identifier.Text));
            }
        });
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VertexCodeFixProvider)), Shared]
public class VertexCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(VertexSourceGen.NoPackingDiagnosticDescriptor.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        //Debugger.Launch();

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            const string packingFixTitle = "Set vertex definition type packing";
            if (diagnostic.Id == VertexSourceGen.NoPackingDiagnosticDescriptor.Id)
                context.RegisterCodeFix(CodeAction.Create(packingFixTitle, createChangedDocument: ct =>
                {
                    var structNode = (StructDeclarationSyntax)root.FindNode(diagnostic.Location.SourceSpan);

                    var correctAttribute = Attribute(ParseName("System.Runtime.InteropServices.StructLayoutAttribute"),
                        AttributeArgumentList(SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]
                        {
                            AttributeArgument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseName("System.Runtime.InteropServices.LayoutKind"), IdentifierName("Sequential"))),
                            Token(SyntaxKind.CommaToken),
                            AttributeArgument(NameEquals("Pack"), null, LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                        }))).WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

                    // find the StructLayout attribute, if it's there
                    Document newDocument;
                    if (structNode.AttributeLists.SelectMany(a => a.Attributes).FirstOrDefault(a => a.Name is NameSyntax ns
                         && semanticModel.GetSymbolInfo(ns).Symbol is IMethodSymbol methodSymbol
                         && methodSymbol.ReceiverType.ToDisplayString() is "System.Runtime.InteropServices.StructLayoutAttribute") is { } attributeSyntax)
                    {
                        newDocument = context.Document.WithSyntaxRoot(root.ReplaceNode(attributeSyntax, correctAttribute));
                    }
                    else if (structNode.AttributeLists.SelectMany(a => a.Attributes).LastOrDefault() is { } lastAttribute)
                        newDocument = context.Document.WithSyntaxRoot(root.InsertNodesAfter(lastAttribute, new[] { correctAttribute }));
                    else
                        throw new InvalidOperationException();  // it should have at least [VertexDefinition] to get in this code fix in the first place

                    return Task.FromResult(newDocument);
                }, packingFixTitle), diagnostic);
        }
    }
}
