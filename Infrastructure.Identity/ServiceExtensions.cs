using Core.Application.Ports;
using Infrastructure.Identity.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

public static class ServiceExtensions
{
    public static void AddIdentityServices(this IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
    }
}