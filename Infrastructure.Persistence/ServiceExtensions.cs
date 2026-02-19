using Core.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class ServiceExtensions
{
    public static void AddPersistenceServices(this IServiceCollection services)
    {
        services.AddSingleton<IUnitOfWork, DatabaseContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IConversationIdService, ConversationIdService>();
    }
}