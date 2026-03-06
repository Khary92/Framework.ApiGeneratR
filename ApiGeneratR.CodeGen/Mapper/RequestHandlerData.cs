namespace ApiGeneratR.CodeGen.Mapper;

public record RequestHandlerData(
    string HandlerShortName,
    string HandlerFullName,
    string RequestShortName,
    string RequestFullName,
    string ResponseShortName,
    string ResponseFullName,
    string Namespace);