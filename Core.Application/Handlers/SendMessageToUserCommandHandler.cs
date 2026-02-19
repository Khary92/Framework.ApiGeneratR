using Api.Definitions.Dto;
using Api.Definitions.Events.Message.Generated;
using Api.Definitions.Requests.Commands;
using Core.Application.Mapper;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class SendMessageToUserCommandHandler(
    IUnitOfWork db,
    ISocketConnectionService socket,
    MessageMapper messageMapper)
    : IRequestHandler<SendMessageToUserCommand, CommandResponse>
{
    public async Task<CommandResponse> HandleAsync(SendMessageToUserCommand command,
        CancellationToken ct = default)
    {
        try
        {
            return await db.ExecuteAsync(async () =>
            {
                var user = db.Users.FirstOrDefault(u => u.Id == command.UserId)
                           ?? throw new KeyNotFoundException("User not found");

                var message = messageMapper.ToDomain(command, user.ConversationId);
                db.Messages.Add(message);

                await Task.WhenAll(
                    socket.BroadcastToAllAdmins(
                        messageMapper.ToPublicMessageReceivedEvent(message).ToWebsocketMessage(), ct)
                );

                return new CommandResponse(true, "Message sent");
            }, ct);
        }
        catch (Exception ex)
        {
            return new CommandResponse(false, ex.Message);
        }
    }
}