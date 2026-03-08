using Api.Definitions.Generated;
using Core.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class ServiceExtensions
{
    public static void AddPersistenceServices(this IServiceCollection services)
    {
        services.AddSingleton<DatabaseContext>();
        services.AddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<DatabaseContext>());
        services.AddSingleton<IIdentityIdMapper>(sp => sp.GetRequiredService<DatabaseContext>());

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IConversationIdService, ConversationIdService>();
    }
}