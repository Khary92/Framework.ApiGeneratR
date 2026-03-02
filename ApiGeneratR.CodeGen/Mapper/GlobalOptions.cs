namespace ApiGeneratR.CodeGen.Mapper;

public record GlobalOptions(string DefinitionsProject, string HandlerProject, bool IsLogMediator, bool IsLogWebsockets)
{
    public string GetLoggerForType(string className) => $"Microsoft.Extensions.Logging.ILogger<{className}>";
}