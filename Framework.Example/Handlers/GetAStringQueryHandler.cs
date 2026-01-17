using Framework.Contract.Mediator;
using Framework.Example.Queries;
using Framework.Example.Services;

namespace Framework.Example.Handlers;

public class GetAStringQueryHandler(IStringService service) : IRequestHandler<GetAStringQuery, string>
{
    public Task<string> HandleAsync(GetAStringQuery request)
    {
        return Task.FromResult(service.Get());
    }
}