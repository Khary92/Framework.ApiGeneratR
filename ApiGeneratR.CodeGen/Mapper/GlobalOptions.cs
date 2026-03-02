namespace ApiGeneratR.CodeGen.Mapper;

public class GlobalOptions(string definitionsProject, string handlerProject, bool isLogMediator)
{
    public string DefinitionsProject { get; } = definitionsProject;
    public string HandlerProject { get; } = handlerProject;
    public bool IsLogMediator { get; } = isLogMediator;

    public string GetLoggerForType(string className) =>
        IsLogMediator ? $"Microsoft.Extensions.Logging.ILogger<{className}>" : "";

    //Do not change. This needs to be static!
    public string AttributeNameSpace => "ApiGeneratR.Attributes";
}