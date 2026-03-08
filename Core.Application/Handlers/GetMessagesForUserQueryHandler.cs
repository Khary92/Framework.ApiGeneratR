using Api.Definitions.Dto;
using Api.Definitions.Generated;
using Api.Definitions.Requests.Queries;
using ApiGeneratR.Attributes;
using Core.Application.Mapper;
using Core.Application.Ports;

namespace Core.Application.Handlers;

[RequestHandler(typeof(GetMessagesForUserQuery))]
public class GetMessagesForUserQueryHandler(
    IUnitOfWork db,
    IConversationIdService conversationIdService,
    MessageMapper mapper) : IGetMessagesForUserQueryHandler
{
    public Task<MessagesWrapper> HandleAsync(GetMessagesForUserQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var originUser = db.Users.FirstOrDefault(u => u.IdentityId == query.IdentityId) ??
                             throw new KeyNotFoundException("User not found");
            var targetUser = db.Users.FirstOrDefault(u => u.Id == query.UserId) ??
                             throw new KeyNotFoundException("User not found");

            var conversationId = conversationIdService.GetConversationId(targetUser, originUser);

            var messages = db.Messages
                .Where(m => m.ConversationId == conversationId)
                .Select(mapper.ToDto)
                .ToList();

            return Task.FromResult(new MessagesWrapper(conversationId, messages));
        }
        catch (Exception)
        {
            return Task.FromResult(new MessagesWrapper(string.Empty, new List<MessageDto>()));
        }
    }
}