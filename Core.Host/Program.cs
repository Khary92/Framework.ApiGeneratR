using System.Security.Cryptography;
using Api.Definitions.Generated;
using ApiDefinitions.Generated;
using Core.Application;
using Core.Application.Ports;
using CoreApplication.Generated;
using Infrastructure.Dispatcher;
using Infrastructure.Dispatcher.Communication;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

namespace Core.Host;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var publicApp = BuildServerApp(args);
        await publicApp.RunAsync("http://*:8080");
    }

    private static WebApplication BuildServerApp(string[] args)
    {
        ApiDocumentation.PrintToPath("/home/jannic/Documents/Documentation.md");
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Directory.GetCurrentDirectory()
        });
        
        builder.Logging.AddSimpleConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/Certs/dataprotection-keys"))
            .SetApplicationName("BlazorApp")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(14));

        builder.Services.AddAntiforgery(options => { options.Cookie.Name = "AntiforgeryCookie"; });

        builder.Services.AddSingletonMediatorServices();
        builder.Services.AddPersistenceServices();

        builder.Services.AddApplicationServices();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddIdentityServices();
        builder.Services.AddDispatcherServices();

        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        var verificationKey = EnsureAndLoadPublicKey();

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = verificationKey,

            ValidateIssuer = true,
            ValidIssuer = AuthStatics.Issuer,

            ValidateAudience = true,
            ValidAudience = AuthStatics.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        builder.Services.AddHostServices(tokenValidationParameters);

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => { options.TokenValidationParameters = tokenValidationParameters; });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            using (app.Logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = context.TraceIdentifier }))
            {
                await next();
            }
        });

        app.AddSecurityConfig();
        app.AddApiEndpoints();

        // Websockets
        app.AddWebSocketEndpoints();

        return app;
    }

    private static RsaSecurityKey EnsureAndLoadPublicKey()
    {
        if (!File.Exists(AuthStatics.PrivateKeyPath) || !File.Exists(AuthStatics.PublicKeyPath))
        {
            var rsa = RSA.Create(2048);
            Directory.CreateDirectory(Path.GetDirectoryName(AuthStatics.PrivateKeyPath)!);
            File.WriteAllText(AuthStatics.PrivateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
            File.WriteAllText(AuthStatics.PublicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        }

        var verificationRsa = RSA.Create();
        verificationRsa.ImportFromPem(File.ReadAllText(AuthStatics.PublicKeyPath));

        return new RsaSecurityKey(verificationRsa)
        {
            KeyId = "auth-server-signing-key"
        };
    }
}