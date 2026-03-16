namespace Core.Host;

public static class ServiceExtensions
{
    public static void AddSecurityConfig(this WebApplication app)
    {
        app.UseWebSockets();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
    }
}