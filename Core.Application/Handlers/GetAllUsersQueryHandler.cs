using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Generated;
using ApiGeneratR.Definitions.Requests.Queries;
using Core.Application.Mapper;
using Core.Application.Ports;

namespace Core.Application.Handlers;

[RequestHandler(typeof(GetAllUsersQuery))]
public class GetAllUsersQueryHandler(IUnitOfWork db, UserMapper mapper)
    : IGetAllUsersQueryHandler
{
    public Task<List<UserDto>> HandleAsync(GetAllUsersQuery query, CancellationToken ct = default)
    {
        return Task.FromResult(db.Users
            .Select(mapper.ToDto)
            .ToList());
    }
}