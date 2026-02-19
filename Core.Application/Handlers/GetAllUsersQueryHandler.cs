using Api.Definitions.Dto;
using Api.Definitions.Requests.Queries;
using Core.Application.Mapper;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class GetAllUsersQueryHandler(IUnitOfWork db, UserMapper mapper)
    : IRequestHandler<GetAllUsersQuery, List<UserDto>>
{
    public Task<List<UserDto>> HandleAsync(GetAllUsersQuery query, CancellationToken ct = default)
    {
        return Task.FromResult(db.Users
            .Select(mapper.ToDto)
            .ToList());
    }
}