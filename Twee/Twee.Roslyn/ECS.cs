using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Twee.Roslyn;

[Generator]
public sealed class ECSSourceGen : IIncrementalGenerator
{
    const string TweeCoreAssemblyName = "Twee.Core";
    const string ComponentAttributeFullName = "Twee.Core.ComponentAttribute";

    class Component
    {
        public string Name { get; }
        public string RootName { get; }

        public Component(string name) =>
            (Name, RootName) =
                (name, Regex.Match(name, @"^.*?([^.]+?)(?:Component)?$") is { Success: true } m ? m.Groups[1].Value : name);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var componentDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node.IsKind(SyntaxKind.StructDeclaration) && ((TypeDeclarationSyntax)node).AttributeLists.Count > 0,
                static (ctx, _) =>
                {
                    //Debugger.Launch();

                    var structDeclarationSyntax = (TypeDeclarationSyntax)ctx.Node;

                    if (ctx.SemanticModel.GetDeclaredSymbol(structDeclarationSyntax) is { } structDeclarationSymbol)
                        foreach (var attributeListSyntax in structDeclarationSyntax.AttributeLists)
                            foreach (var attributeSyntax in attributeListSyntax.Attributes)
                                if (ctx.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol
                                    && attributeSymbol.ContainingType.ToDisplayString() is ComponentAttributeFullName
                                    && attributeSymbol.ContainingAssembly.Name is TweeCoreAssemblyName)
                                {
                                    return new Component(structDeclarationSymbol.ToDisplayString());
                                }

                    return null;
                })
            .Where(static m => m is not null);

        context.RegisterSourceOutput(componentDeclarations.Collect(), static (spc, input) =>
        {
            //Debugger.Launch();

            var sb = new StringBuilder();

            var components = input;

            sb.AppendLine($$"""
                namespace Twee.Ecs;

                {{Common.GeneratedCodeAttributeText}}
                [Flags]
                internal enum Components: ulong { {{string.Join(", ",
                    components.Select((c, idx) => $"{c!.RootName} = 1ul << {idx}"))}} }
                """);

            spc.AddSource("ECS.g.cs", sb.ToString());
        });
    }
}
