using Api.Definitions.Dto;
using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace Api.Definitions.Requests.Commands;

[Request("create-user", true, RequestType.Command)]
public record CreateUserCommand(
    string LoginName,
    string InitialPassword,
    string FirstName,
    string LastName) : RequestResponseTag<CommandResponse>;