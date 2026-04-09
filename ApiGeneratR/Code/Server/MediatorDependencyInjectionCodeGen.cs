using System.Collections.Immutable;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Server;

public class MediatorDependencyInjectionCodeGen
{
    public static SourceCodeFile Create(ImmutableArray<RequestHandlerData> handlers, string? projectNamespace,
        GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System.Collections.Generic", "System.Threading", "System.Threading.Tasks",
            "Microsoft.Extensions.DependencyInjection"
        ]);

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public static class MediatorExtensions");
        scb.StartScope("extension(IServiceCollection services)");
        scb.StartScope("public void AddGeneratedHandlerServices(ServiceLifetime lifetime = ServiceLifetime.Scoped)");
        scb.AddLine(
            $"services.Add(new ServiceDescriptor(typeof(global::{options.DefinitionsProject}.Generated.IMediator), typeof(global::{projectNamespace}.Generated.SourceMediator), lifetime));");

        foreach (var handler in handlers)
        {
            if (handler == null) continue;
            scb.AddLine(
                $"services.Add(new ServiceDescriptor(typeof(global::{options.DefinitionsProject}.Generated.I{handler.RequestShortName}Handler), typeof({handler.HandlerFullName}), lifetime));");
        }

        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        return new SourceCodeFile("MediatorExtensions.g.cs", scb.ToString());
    }
}