using Presentation.Web.Models;

namespace Presentation.Web.State.User;

public interface IUserService : IAsyncInitializeModel
{
    List<UserModel> Users { get; }
    event Func<Task>? OnCollectionChanged;
}