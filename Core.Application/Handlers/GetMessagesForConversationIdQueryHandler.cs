using Api.Definitions.Dto;
using Api.Definitions.Requests.Queries;
using Core.Application.Mapper;
using Core.Application.Ports;
using Shared.Contracts.Mediator;

namespace Core.Application.Handlers;

public class GetMessagesForConversationIdQueryHandler(IUnitOfWork db, MessageMapper mapper)
    : IRequestHandler<GetMessagesForConversationIdQuery, List<MessageDto>>
{
    public Task<List<MessageDto>> HandleAsync(GetMessagesForConversationIdQuery query,
        CancellationToken ct = default)
    {
        return Task.FromResult(db.Messages
            .Where(m => m.ConversationId == query.ConversationId)
            .Select(mapper.ToAdminDto)
            .ToList());
    }
}