namespace Shared.Contracts.Mediator;

public interface IMediator
{
    Task<TResponse> HandleAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}