using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis;

namespace ApiGeneratR.Helpers;

public static class OptionsExtensions
{
    public static IncrementalValueProvider<GlobalOptions> GetGlobalOptions(
        this IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("apigeneratr_project_clients", out var clientProjects);

                options.GlobalOptions.TryGetValue("apigeneratr_auth_profiles", out var authProfiles);

                options.GlobalOptions.TryGetValue("apigeneratr_project_definitions",
                    out var definitionsProject);

                options.GlobalOptions.TryGetValue("apigeneratr_project_handler", out var handlerProject);

                options.GlobalOptions.TryGetValue("apigeneratr_log_mediator", out var isLogMediator);

                options.GlobalOptions.TryGetValue("apigeneratr_log_websocket", out var isLogWebsocket);

                return new GlobalOptions(
                    clientProjects == null
                        ? ["Missing global config entry for client projects!"]
                        : clientProjects.Split(','),
                    authProfiles == null
                        ? ["Missing global config entry for auth profiles!"]
                        : authProfiles.Split(','),
                    definitionsProject ?? "Missing global config entry for definitions project!",
                    handlerProject ?? "Missing global config entry for request handler project!",
                    isLogMediator == "true",
                    isLogWebsocket == "true");
            });
    }
}