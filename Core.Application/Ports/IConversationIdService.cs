using Core.Domain.Entities;

namespace Core.Application.Ports;

public interface IConversationIdService
{
    string GetConversationId(User firstUser, User secondUser);
}