using Presentation.Web.Models;

namespace Presentation.Web.State.Messaging;

public interface IMessageService : IDisposable
{
    event Action? OnMessageReceived;
    Task<List<MessageModel>> GetMessagesForSelectedUser(UserModel selectedUser);
}