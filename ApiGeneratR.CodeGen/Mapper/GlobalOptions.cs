using System.Linq;

namespace ApiGeneratR.CodeGen.Mapper;

public record GlobalOptions(
    string[] ClientProjects,
    string DefinitionsProject,
    string HandlerProject,
    bool IsLogMediator,
    bool IsLogWebsockets)
{
    public string GetLoggerForType(string className) => $"Microsoft.Extensions.Logging.ILogger<{className}>";

    public bool IsClientProject(string projectName) => ClientProjects.Contains(projectName);
}