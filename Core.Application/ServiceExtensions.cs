using Api.Definitions.Generated;
using Core.Application.Mapper;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Application;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public void AddApplicationServices()
        {
            services.AddMapperServices();
        }

        private void AddMapperServices()
        {
            services.AddSingleton<MessageMapper>();
            services.AddSingleton<UserMapper>();
        }
    }
}