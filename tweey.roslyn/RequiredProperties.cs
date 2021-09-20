using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tweey.roslyn;

[Generator]
public class RequiredPropertiesSourceGen : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) =>
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("RequiredPropertiesSourceGen.cs", @"
using System;

[AttributeUsage(AttributeTargets.Property)]
public class RequiredPropertyAttribute: Attribute { }");
        });
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiredPropertiesAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor RequiredPropertyMissingDiagnosticDescriptor =
        new("MBRP001", "RequiredPropertyMissing", "Missing required properties: {0}", "Functionality", DiagnosticSeverity.Error, true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(RequiredPropertyMissingDiagnosticDescriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(context =>
        {
            var op = (IObjectCreationOperation)context.Operation;
            StringBuilder sb = null;

            foreach (var requiredMember in op.Type.GetMembers().Where(m => m.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == "RequiredPropertyAttribute")))
                if (!(op.Initializer?.Initializers.Any(w =>
                    w is IAssignmentOperation assignmentOperation && assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation && propertyReferenceOperation.Member.Name == requiredMember.Name) ?? false))
                {
                    sb ??= new();
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(requiredMember.Name);
                }

            if (sb is not null)
                context.ReportDiagnostic(Diagnostic.Create(RequiredPropertyMissingDiagnosticDescriptor, op.Syntax.GetLocation(), sb));
        }, OperationKind.ObjectCreation);
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequiredPropertiesCodeFixProvider))]
public class RequiredPropertiesCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(RequiredPropertiesAnalyzer.RequiredPropertyMissingDiagnosticDescriptor.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        //Debugger.Launch();

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            const string title = "Add missing required properties";
            context.RegisterCodeFix(CodeAction.Create(title, createChangedDocument: async ct =>
            {
                var node = (ObjectCreationExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan);

                return context.Document;
            }, title), diagnostic);
        }
    }
}