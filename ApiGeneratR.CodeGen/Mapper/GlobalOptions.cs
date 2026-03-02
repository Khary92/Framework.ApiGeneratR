namespace ApiGeneratR.CodeGen.Mapper;

public class GlobalOptions(string definitionsProject, string handlerProject)
{
    public string DefinitionsProject { get; } = definitionsProject;
    public string HandlerProject { get; } = handlerProject;
    //Do not change. This needs to be static!
    public string AttributeNameSpace => "ApiGeneratR.Attributes";
}