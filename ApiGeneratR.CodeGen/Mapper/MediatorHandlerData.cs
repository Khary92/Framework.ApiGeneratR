namespace ApiGeneratR.CodeGen.Mapper;

public record MediatorHandlerData(
    string HandlerShortName,
    string RequestType,
    string RequestShortName,
    string ResponseShortName,
    string ResponseType,
    string HandlerType);