using Core.Application.Mapper;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.EventBus;

namespace Core.Application;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public void AddApplicationServices()
        {
            services.AddAuthSettingsService();
            services.AddMapperServices();
        }

        private void AddMapperServices()
        {
            services.AddSingleton<MessageMapper>();
            services.AddSingleton<UserMapper>();
        }

        private void AddAuthSettingsService()
        {
            services.AddSingleton<EventService>();
            services.AddSingleton<IEventSubscriber>(sp => sp.GetRequiredService<EventService>());
            services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<EventService>());
        }
    }
}