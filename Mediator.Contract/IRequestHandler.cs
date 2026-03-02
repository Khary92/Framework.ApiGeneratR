namespace Mediator.Contract;

public interface IRequestHandler<TRequest, TResponse> : IRequest<TResponse> where TRequest : IRequest<TResponse>;