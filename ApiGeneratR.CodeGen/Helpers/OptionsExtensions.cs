using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.CodeGen.Helpers;

public static class OptionsExtensions
{
    public static IncrementalValueProvider<GlobalOptions> GetGlobalOptions(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                // 1. Definiere die Keys (Roslyn-konform in Kleinschreibung)
                // Wenn es aus MSBuild kommt: "build_property.apigeneratr_definitionsproject"
                // Wenn es aus .globalconfig kommt: "apigeneratr_definitionsproject"
            
                // Versuche beide Varianten (mit und ohne build_property)
                if (!options.GlobalOptions.TryGetValue("build_property.apigeneratr_definitionsproject", out var definitionsProject))
                {
                    options.GlobalOptions.TryGetValue("apigeneratr_definitionsproject", out definitionsProject);
                }

                if (!options.GlobalOptions.TryGetValue("build_property.apigeneratr_handlerproject", out var handlerProject))
                {
                    options.GlobalOptions.TryGetValue("apigeneratr_handlerproject", out handlerProject);
                }

                return new GlobalOptions(
                    definitionsProject ?? "ApiGeneratR.Definitions.Default",
                    handlerProject ?? "ApiGeneratR.Handler.Default");
            });
    }

}