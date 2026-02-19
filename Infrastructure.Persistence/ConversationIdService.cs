using Core.Application.Ports;
using Core.Domain.Entities;

namespace Infrastructure.Persistence;

public class ConversationIdService : IConversationIdService
{
    public string GetConversationId(User firstUser, User secondUser)
    {
        List<Guid> guids = [firstUser.Id, secondUser.Id];
        guids.Sort();

        return guids[0] + ":" + guids[1];
    }
}