using Api.Definitions.Dto;
using ApiGeneratR.Attributes;
using ApiGeneratR.Tags;

namespace Api.Definitions.Requests.Commands;

[Request("create-user", "User", RequestType.Command)]
public record CreateUserCommand(
    string LoginName,
    string InitialPassword,
    string FirstName,
    string LastName) : RequestResponseTag<CommandResponse>;