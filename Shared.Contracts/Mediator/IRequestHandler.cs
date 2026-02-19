namespace Shared.Contracts.Mediator;

public interface IRequestHandler<in TRequestIn, TResponseOut> where TRequestIn : IRequest<TResponseOut>
{
    Task<TResponseOut> HandleAsync(TRequestIn request, CancellationToken cancellationToken = default);
}