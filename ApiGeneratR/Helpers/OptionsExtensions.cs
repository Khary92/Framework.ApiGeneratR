using System;
using System.Collections.Immutable;
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

                options.GlobalOptions.TryGetValue("apigeneratr_log_clientapi", out var isLogClientApi);

                options.GlobalOptions.TryGetValue("apigeneratr_com_channels", out var comChannels);

                if (comChannels == null)
                    throw new InvalidOperationException("Missing global config entry for communication channels!");

                var channels = comChannels.Split(',');

                var arrayBuilder = ImmutableArray.CreateBuilder<ChannelData>();
                foreach (var channel in channels)
                {
                    var data = channel.Split(':');
                    arrayBuilder.Add(new ChannelData(data[0], data[1]));
                }

                return new GlobalOptions(
                    arrayBuilder.ToImmutableArray(),
                    clientProjects == null
                        ? ["Missing global config entry for client projects!"]
                        : clientProjects.Split(','),
                    authProfiles == null
                        ? ["Missing global config entry for auth profiles!"]
                        : authProfiles.Split(','),
                    definitionsProject ?? "Missing global config entry for definitions project!",
                    handlerProject ?? "Missing global config entry for request handler project!",
                    isLogMediator == "true",
                    isLogWebsocket == "true",
                    isLogClientApi == "true");
            });
    }
}