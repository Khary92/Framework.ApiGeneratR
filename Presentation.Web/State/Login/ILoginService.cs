namespace Presentation.Web.State.Login;

public interface ILoginService
{
    bool IsUserLoggedIn { get; }
    bool IsCurrentUser(Guid userId);
    Task Login(string token);
    Task Logout();
}