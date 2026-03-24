using System.Collections.Immutable;
using System.Linq;

namespace ApiGeneratR.Mapper;

public record GlobalOptions(
    ImmutableArray<ChannelData> CommunicationChannels,
    string[] ClientProjects,
    string[] AuthProfiles,
    string DefinitionsProject,
    string HandlerProject,
    bool IsLogMediator,
    bool IsLogWebsockets)
{
    public string GetLoggerForType(string className) => $"Microsoft.Extensions.Logging.ILogger<{className}>";

    public bool IsClientProject(string projectName) => ClientProjects.Contains(projectName);
}