using Api.Definitions.Dto;
using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;
using Shared.Contracts.Mediator;

namespace Api.Definitions.Requests.Commands;

[Request("create-user", true, RequestType.Command)]
public record CreateUserCommand(
    string LoginName,
    string InitialPassword,
    string FirstName,
    string LastName) : IRequest<CommandResponse>;