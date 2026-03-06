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
                if (!options.GlobalOptions.TryGetValue("apigeneratr_clientprojects", out var clientProjects))
                {
                    options.GlobalOptions.TryGetValue("apigeneratr_clientprojects", out clientProjects);
                }

                if (!options.GlobalOptions.TryGetValue("build_property.apigeneratr_definitionsproject",
                        out var definitionsProject))
                {
                    options.GlobalOptions.TryGetValue("apigeneratr_definitionsproject", out definitionsProject);
                }

                if (!options.GlobalOptions.TryGetValue("build_property.apigeneratr_handlerproject",
                        out var handlerProject))
                {
                    options.GlobalOptions.TryGetValue("apigeneratr_handlerproject", out handlerProject);
                }

                if (!options.GlobalOptions.TryGetValue("apigeneratr_log_mediator", out var isLogMediator))
                {
                    options.GlobalOptions.TryGetValue("apigeneratr_log_mediator", out isLogMediator);
                }

                if (!options.GlobalOptions.TryGetValue("apigeneratr_log_websocket", out var isLogWebsocket))
                {
                    options.GlobalOptions.TryGetValue("apigeneratr_log_websocket", out isLogWebsocket);
                }

                return new GlobalOptions(
                    clientProjects == null
                        ? ["Missing global config entry for client projects!"]
                        : clientProjects.Split(','),
                    definitionsProject ?? "Missing global config entry for definitions project!",
                    handlerProject ?? "Missing global config entry for request handler project!",
                    isLogMediator == "true",
                    isLogWebsocket == "true");
            });
    }
}