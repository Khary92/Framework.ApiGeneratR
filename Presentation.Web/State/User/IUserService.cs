using Presentation.Web.Models;

namespace Presentation.Web.State.User;

public interface IUserService : IAsyncInitializeModel, IAsyncDisposable
{
    List<UserModel> Users { get; }
    event Func<Task>? OnCollectionChanged;
}