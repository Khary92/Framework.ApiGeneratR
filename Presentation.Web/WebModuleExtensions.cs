using ApiGeneratR.Definitions.Generated;
using MudBlazor.Services;
using Presentation.Web.Components;
using Presentation.Web.Services;
using Presentation.Web.State;
using Presentation.Web.State.Login;
using Presentation.Web.State.Messaging;
using Presentation.Web.State.User;

namespace Presentation.Web;

public static class WebModuleExtensions
{
    private const string BaseUrl = "http://core-server:8080/";

    public static void ConfigureWebApp(this WebApplication app)
    {
        app.UseDeveloperExceptionPage(); // Enable detailed errors

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }

    extension(IServiceCollection services)
    {
        public void AddBlazorServices()
        {
            services.AddRazorComponents()
                .AddInteractiveServerComponents();

            services.AddMudServices();

            services.AddStateServices();
            services.AddGeneratedServices();
            services.AddValidators();
        }

        private void AddGeneratedServices()
        {
            services.AddHttpClient<IApiClient, ApiHttpClient>(client => { client.BaseAddress = new Uri(BaseUrl); });
            services.AddApiServices();
        }

        private void AddStateServices()
        {
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<IMessageService, MessageService>();

            services.AddScoped<UserService>();
            services.AddScoped<IUserService>(sp => sp.GetRequiredService<UserService>());
            services.AddScoped<IAsyncInitializeModel>(sp => sp.GetRequiredService<UserService>());
        }

        private void AddValidators()
        {
            services.AddTransient<IPasswordGenerator, PasswordGenerator>();
        }
    }
}