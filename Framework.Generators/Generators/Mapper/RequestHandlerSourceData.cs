namespace Framework.Generators.Generators.Mapper;

public class RequestHandlerSourceData(
    string handlerShortName,
    string requestType,
    string requestShortName,
    string responseShortName,
    string responseType,
    string handlerType)
{
    public string RequestShortName { get; } = requestShortName;
    public string RequestType { get; } = requestType;

    public string ResponseShortName { get; } = responseShortName;
    public string ResponseType { get; } = responseType;

    public string HandlerShortName => handlerShortName;
    public string HandlerType { get; } = handlerType;
}