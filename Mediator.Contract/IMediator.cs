namespace Mediator.Contract;

public interface IMediator
{
    Task<TResponse> HandleAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}