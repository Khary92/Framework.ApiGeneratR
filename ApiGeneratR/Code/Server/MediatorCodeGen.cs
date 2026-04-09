using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Code.Server;

public static class MediatorCodeGen
{
    public static SourceCodeFile Create(ImmutableArray<RequestHandlerData> handlers, string projectNamespace,
        GlobalOptions options)
    {
        var scb = new SourceCodeBuilder();
        
        scb.SetUsings([
            "System.Collections.Generic", "System.Threading",
            "System.Threading.Tasks", "Microsoft.Extensions.DependencyInjection"
        ]);

        if (options.IsLogMediator) scb.AddUsing("Microsoft.Extensions.Logging");

        scb.SetNamespace($"{projectNamespace}.Generated");

        var parameters = string.Empty;
        foreach (var handler in handlers)
        {
            parameters += handler == handlers.Last()
                ? $"global::{options.DefinitionsProject}.Generated.I{handler.RequestShortName}Handler {handler.HandlerShortName.ToLower()}"
                : $"global::{options.DefinitionsProject}.Generated.I{handler.RequestShortName}Handler {handler.HandlerShortName.ToLower()}, ";
        }

        var optionalLogger = options.IsLogMediator ? $"{options.GetLoggerForType("SourceMediator")} logger, " : "";

        scb.StartScope(
            $"public class SourceMediator({optionalLogger}{parameters}) : global::{options.DefinitionsProject}.Generated.IMediator");

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            scb.StartScope(
                $"public async Task<{handler.ResponseFullName}> HandleAsync({handler.RequestFullName} request, CancellationToken ct = default)");

            scb.AddLine("if (request == null) throw new ArgumentNullException(\"request is null\");");

            if (options.IsLogMediator)
                scb.AddLine("logger.LogDebug($\"Handling request: {request} \");");
            scb.AddLine();
            scb.StartScope("try");
            scb.AddLine($"var result = await {handler.HandlerShortName.ToLower()}.HandleAsync(request, ct);");
            if (options.IsLogMediator)
                scb.AddLine("logger.LogDebug(\"Successfully handled {RequestFullName}\", request.GetType().Name);");
            scb.AddLine("return result;");
            scb.EndScope();

            scb.StartScope(options.IsLogMediator ? "catch (Exception e)" : "catch (Exception)");

            if (options.IsLogMediator)
                scb.AddLine(
                    "logger.LogError(e, \"Error handling request {RequestFullName}\", request.GetType().Name);");
            scb.AddLine("throw;");
            scb.EndScope();
            scb.EndScope();

            if (!Equals(handler, handlers.Last())) scb.AddLine();
        }

        scb.EndScope();

        return new SourceCodeFile("SourceMediator.g.cs", scb.ToString());
    }
}