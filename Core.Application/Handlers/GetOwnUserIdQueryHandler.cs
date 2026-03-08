using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Api.Definitions.Requests.Queries;
using ApiGeneratR.Attributes;
using Core.Application.Ports;

namespace Core.Application.Handlers;

[RequestHandler(typeof(GetMyUserIdQuery))]
public class GetOwnUserIdQueryHandler(IUnitOfWork unitOfWork) : IGetMyUserIdQueryHandler
{
    public Task<UserIdDto> HandleAsync(GetMyUserIdQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = unitOfWork.Users.First(u => u.IdentityId == request.IdentityId).Id;
            return Task.FromResult(new UserIdDto(id));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(new UserIdDto(Guid.Empty));
        }
    }
}