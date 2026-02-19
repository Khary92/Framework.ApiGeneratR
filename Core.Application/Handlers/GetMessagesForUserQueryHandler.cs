using Api.Definitions.Dto;
using Api.Definitions.Requests.Queries;
using Core.Application.Mapper;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class GetMessagesForUserQueryHandler(
    IUnitOfWork db,
    IConversationIdService conversationIdService,
    MessageMapper mapper)
    : IRequestHandler<GetMessagesForUserQuery, List<MessageDto>>
{
    public Task<List<MessageDto>> HandleAsync(GetMessagesForUserQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var originUser = db.Users.FirstOrDefault(u => u.IdentityId == query.IdentityId) ??
                             throw new KeyNotFoundException("User not found");
            var targetUser = db.Users.FirstOrDefault(u => u.Id == query.UserId) ??
                             throw new KeyNotFoundException("User not found");

            var conversationId = conversationIdService.GetConversationId(targetUser, originUser);

            return Task.FromResult(db.Messages
                .Where(m => m.ConversationId == conversationId)
                .Select(mapper.ToDto)
                .ToList());
        }
        catch (Exception)
        {
            return Task.FromResult(new List<MessageDto>());
        }
    }
}