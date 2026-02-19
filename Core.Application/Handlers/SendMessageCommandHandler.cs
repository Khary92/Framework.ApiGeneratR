using Api.Definitions.Dto;
using Api.Definitions.Events.Message.Generated;
using Api.Definitions.Requests.Commands;
using Core.Application.Mapper;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class SendMessageCommandHandler(
    IUnitOfWork db,
    ISocketConnectionService socket,
    IConversationIdService conversationIdService,
    MessageMapper messageMapper)
    : IRequestHandler<SendMessageCommand, CommandResponse>
{
    public async Task<CommandResponse> HandleAsync(SendMessageCommand command,
        CancellationToken ct = default)
    {
        try
        {
            return await db.ExecuteAsync(async () =>
            {
                var targetUser = db.Users.FirstOrDefault(u => u.Id == command.TargetUserId)
                                 ?? throw new KeyNotFoundException("User not found");

                var originUser = db.Users.FirstOrDefault(u => u.IdentityId == command.IdentityId)
                                 ?? throw new KeyNotFoundException("User not found");

                var conversationId = conversationIdService.GetConversationId(targetUser, originUser);

                var message = messageMapper.ToDomain(command, conversationId, originUser.Id);
                db.Messages.Add(message);

                await socket.SendMessageToUser(messageMapper.ToMessageReceivedEvent(message).ToWebsocketMessage(),
                    targetUser.Id, ct);
                await socket.SendMessageToUser(messageMapper.ToMessageReceivedEvent(message).ToWebsocketMessage(),
                    originUser.Id, ct);

                return new CommandResponse(true, "Message sent");
            }, ct);
        }
        catch (Exception ex)
        {
            return new CommandResponse(false, ex.Message);
        }
    }
}