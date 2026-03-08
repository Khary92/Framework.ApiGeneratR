using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApiGeneratR.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AuthPolicyAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static readonly DiagnosticDescriptor Rule = new(
        "APIGEN001",
        "Invalid auth profile",
        $"The profile '{0}' is not configured. Allowed profiles are : AllowAnonymous, Default, {string.Join(", ", 1)}",
        "Security",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var globalOptions = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        globalOptions.TryGetValue("apigeneratr_auth_profiles", out var authProfilesRaw);

        var allowedProfiles = authProfilesRaw?.Split(',') ?? Array.Empty<string>();

        var symbol = (INamedTypeSymbol)context.Symbol;
        var attribute = symbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == "ApiGeneratR.Attributes.RequestAttribute");

        if (attribute == null || attribute.ConstructorArguments.Length < 2) return;

        var authPolicyValue = attribute.ConstructorArguments[1].Value as string;

        if (authPolicyValue == null ||
            allowedProfiles.Contains(authPolicyValue) ||
            authPolicyValue == "Default" ||
            authPolicyValue == "AllowAnonymous") return;
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                "APIGEN001",
                "Invalid auth profile",
                $"The profile '{authPolicyValue}' is not configured. Allowed profiles are : AllowAnonymous, Default, {string.Join(", ", allowedProfiles)}",
                "Security",
                DiagnosticSeverity.Error,
                true),
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
        );
        context.ReportDiagnostic(diagnostic);
    }
}