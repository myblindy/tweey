using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace tweey.roslyn;

[Generator]
public class VertexSourceGen : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Debugger.Launch();

        var inputs = context.CompilationProvider;
        context.RegisterSourceOutput(inputs, static (context, compilationProvider) =>
        {
            var sb = new StringBuilder(@"
using System;
using OpenTK.Graphics.OpenGL;

[AttributeUsage(AttributeTargets.Struct)]
public class VertexDefinitionAttribute: Attribute { }

public static class VertexDefinitionSetup
{
    public static void Setup(Type t, VertexArrayHandle va) 
    {");

            foreach (var tree in compilationProvider.SyntaxTrees)
                if (tree.TryGetRoot(out var rootNode))
                {
                    var semanticModel = compilationProvider.GetSemanticModel(tree);
                    foreach (var sd in rootNode.DescendantNodes().OfType<StructDeclarationSyntax>())
                        if (sd.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name is IdentifierNameSyntax ins && ins.Identifier.Text is "VertexDefinition" or "VertexDefinitionAttribute"))
                        {
                            sb.AppendLine($"if(t == typeof({sd.Identifier})) {{");
                            int idx = 0;
                            var offset = 0;
                            foreach (var field in sd.Members.OfType<FieldDeclarationSyntax>())
                                if (semanticModel.GetSymbolInfo(field.Declaration.Type).Symbol is INamedTypeSymbol namedTypeSymbol)
                                {
                                    var fullTypeName = namedTypeSymbol.ToDisplayString();
                                    var fieldCounts = fullTypeName switch
                                    {
                                        "System.Single" => 1,
                                        "System.Numerics.Vector2" => 2,
                                        "System.Numerics.Vector3" => 3,
                                        "System.Numerics.Vector4" => 4,
                                        _ => throw new NotImplementedException()
                                    };
                                    var fieldTypes = fullTypeName switch
                                    {
                                        "System.Single" => "VertexAttribType.Float",
                                        "System.Numerics.Vector2" => "VertexAttribType.Float",
                                        "System.Numerics.Vector3" => "VertexAttribType.Float",
                                        "System.Numerics.Vector4" => "VertexAttribType.Float",
                                        _ => throw new NotImplementedException()
                                    };
                                    sb.AppendLine($"GL.EnableVertexArrayAttrib(va, {idx});");
                                    sb.AppendLine($"GL.VertexArrayAttribFormat(va, {idx}, {fieldCounts}, {fieldTypes}, false, {offset});");

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
