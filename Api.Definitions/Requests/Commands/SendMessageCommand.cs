using Api.Definitions.Dto;
using Shared.Contracts.Attributes;
using Shared.Contracts.Attributes.Enums;
using Shared.Contracts.Mediator;

namespace Api.Definitions.Requests.Commands;

[Request("send-message", true, RequestType.Command)]
public record SendMessageCommand(string Message, Guid TargetUserId, Guid IdentityId = default)
    : IRequest<CommandResponse>;