using Microsoft.IdentityModel.Tokens;

namespace Core.Host;

public static class ServiceExtensions
{
    public static void AddHostServices(this IServiceCollection services,
        TokenValidationParameters tokenValidationParameters)
    {
        services.AddSingleton(tokenValidationParameters);
    }

    public static void AddSecurityConfig(this WebApplication app)
    {
        app.UseWebSockets();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
    }
}