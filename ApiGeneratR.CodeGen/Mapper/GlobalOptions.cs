namespace ApiGeneratR.CodeGen.Mapper;

public class GlobalOptions(string definitionsProject, string handlerProject, bool isLogMediator, bool isLogWebsockets)
{
    public string DefinitionsProject { get; } = definitionsProject;
    public string HandlerProject { get; } = handlerProject;
    public bool IsLogMediator { get; } = isLogMediator;
    public bool IsLogWebsockets { get; } = isLogWebsockets;

    public string GetLoggerForType(string className) => $"Microsoft.Extensions.Logging.ILogger<{className}>";
}