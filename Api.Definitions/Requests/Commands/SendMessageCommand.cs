using Api.Definitions.Dto;
using Api.Definitions.Generated;

namespace Api.Definitions.Requests.Commands;

[Request("send-message", true, RequestType.Command)]
public record SendMessageCommand(string Message, Guid TargetUserId, Guid IdentityId = default)
    : IRequest<CommandResponse>;