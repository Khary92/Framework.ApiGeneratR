namespace Presentation.Web.State.Login;

public interface ILoginService
{
    bool IsUserLoggedIn { get; }
    Task Login(string token);
    Task Logout();
}