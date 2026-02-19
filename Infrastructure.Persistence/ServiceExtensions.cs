using Core.Application.Ports;
using Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class ServiceExtensions
{
    public static void AddDatabaseServices(this IServiceCollection services)
    {
        services.AddSingleton<IUnitOfWork, DatabaseContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
    }
}