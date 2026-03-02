namespace ApiGeneratR.Definitions.Mediator;

public interface IRequestHandler<TRequest, TResponse> : IRequest<TResponse> where TRequest : IRequest<TResponse>;