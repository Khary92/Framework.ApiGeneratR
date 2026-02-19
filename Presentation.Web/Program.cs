using Microsoft.AspNetCore.DataProtection;

namespace Presentation.Web;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var publicApp = BuildAdminWebApp(args);
        await publicApp.RunAsync("http://*:8080");
    }

    private static WebApplication BuildAdminWebApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Directory.GetCurrentDirectory()
        });

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/certs/dataprotection-keys"))
            .SetApplicationName("PublicBlazorApp")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(14));
        builder.Services.AddAntiforgery(options => { options.Cookie.Name = "AntiforgeryCookie"; });

        builder.Services.AddAdminBlazorServices();

        var app = builder.Build();

        app.ConfigureWebApp();

        return app;
    }
}