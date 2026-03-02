using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Api.Definitions.Mediator;

namespace Api.Definitions.Requests.Commands;

[Request("create-user", true, RequestType.Command)]
public record CreateUserCommand(
    string LoginName,
    string InitialPassword,
    string FirstName,
    string LastName) : IRequest<CommandResponse>;