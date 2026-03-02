using ApiGeneratR.Attributes;
using ApiGeneratR.Definitions.Dto;
using ApiGeneratR.Definitions.Mediator;

namespace ApiGeneratR.Definitions.Requests.Commands;

[Request("create-user", true, RequestType.Command)]
public record CreateUserCommand(
    string LoginName,
    string InitialPassword,
    string FirstName,
    string LastName) : IRequest<CommandResponse>;