using Api.Definitions.Dto;
using Api.Definitions.Requests.Queries;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class GetOwnUserIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMyUserIdQuery, UserIdDto>
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