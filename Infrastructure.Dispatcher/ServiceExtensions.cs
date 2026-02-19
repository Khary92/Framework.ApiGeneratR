using Core.Application.Ports;
using Infrastructure.Dispatcher.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dispatcher;

public static class ServiceExtensions
{
    public static void AddDispatcherServices(this IServiceCollection services)
    {
        services.AddSingleton<ISocketConnectionService, SocketConnectionService>();
    }
}