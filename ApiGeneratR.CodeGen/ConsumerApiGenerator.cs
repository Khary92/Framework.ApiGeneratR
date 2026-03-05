using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Helpers;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen;

[Generator(LanguageNames.CSharp)]
public class ConsumerApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var apiSourceData = context.GetRequestSourceData();
        var eventSourceData = context.GetEventSourceData();

        var combined = apiSourceData.Combine(assemblyName).Combine(eventSourceData).Combine(context.GetGlobalOptions());

        context.RegisterSourceOutput(combined,
            static (spc, source) =>
            {
                try
                {
                    Execute(spc, source.Left.Left.Left, source.Left.Left.Right, source.Left.Right, source.Right);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("GEN001", "ServerApiGenerator Error",
                            "Error generating consumer api code: {0}", "Generator", DiagnosticSeverity.Error, true),
                        Location.None, ex.Message));
                }
            });
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<RequestData> requestData,
        string? projectNamespace, ImmutableArray<EventSourceData> eventData, GlobalOptions options)
    {
        if (requestData.IsDefaultOrEmpty || eventData.IsDefaultOrEmpty ||
            projectNamespace != options.DefinitionsProject) return;

        ExecuteHttpClientGeneration(context, projectNamespace);
        ExecuteWebsocketInterfaceGeneration(context, projectNamespace);

        ExecuteRequestSenderGeneration(context, requestData, projectNamespace);
        ExecuteRequestSenderFacadesGeneration(context, requestData, projectNamespace, "Command");
        ExecuteRequestSenderFacadesGeneration(context, requestData, projectNamespace, "Query");
        ExecuteWebsocketReceiverGeneration(context, eventData, projectNamespace);
        ExecuteDocumentationGeneration(context, eventData, requestData, projectNamespace);

        ExecuteInterfaceGeneration(context, eventData, requestData, projectNamespace);
        ExecuteExtensionMethodGeneration(context, eventData, requestData, projectNamespace);
    }

    private static void ExecuteInterfaceGeneration(SourceProductionContext context,
        ImmutableArray<EventSourceData> events, ImmutableArray<RequestData> requests, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Threading"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IApiFacade");
        scb.AddLine("void SetToken(string token);");
        scb.AddLine("IEventSubscriber EventSubscriber { get; }");
        scb.AddLine("IEventPublisher EventPublisher { get; }");
        scb.AddLine("IWebSocketService WebSocket { get; }");
        scb.AddLine("ICommandSender Commands { get; }");
        scb.AddLine("IQuerySender Queries { get; }");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope(
            "public class ConsumerApi(IWebSocketService webSocket, ICommandSender commands, IQuerySender queries, IEventPublisher eventPublisher, IEventSubscriber eventSubscriber) : IApiFacade");
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("commands.InjectToken(token);");
        scb.AddLine("queries.InjectToken(token);");
        scb.AddLine("webSocket.SetToken(token);");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("public IWebSocketService WebSocket => webSocket;");
        scb.AddLine("public ICommandSender Commands => commands;");
        scb.AddLine("public IQuerySender Queries => queries;");
        scb.AddLine("public IEventPublisher EventPublisher => eventPublisher;");
        scb.AddLine("public IEventSubscriber EventSubscriber => eventSubscriber;");
        scb.EndScope();

        context.AddSource("ApiFacade.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));

        scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Threading"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");
    }

    private static void ExecuteExtensionMethodGeneration(SourceProductionContext context,
        ImmutableArray<EventSourceData> events, ImmutableArray<RequestData> requests, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Threading"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public static class ApiFacadeExtensions");
        scb.StartScope("public static void AddApiServices(this IServiceCollection services)");
        scb.AddLine("services.AddSingleton<IApiFacade, ConsumerApi>();");
        scb.AddLine("services.AddSingleton<IWebSocketService, WebSocketService>();");
        scb.AddLine("services.AddSingleton<ICommandSender, GeneratedCommandSender>();");
        scb.AddLine("services.AddSingleton<IQuerySender, GeneratedQuerySender>();");
        scb.AddLine("services.AddSingleton<EventService>();");
        scb.AddLine("services.AddSingleton<IEventSubscriber>(sp => sp.GetRequiredService<EventService>());");
        scb.AddLine("services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<EventService>());");

        foreach (var request in requests)
        {
            if (request == null) continue;

            scb.AddLine(
                $"services.AddSingleton<I{request.RequestShortName}Sender, Generated{request.RequestShortName}Sender>();");
        }

        scb.EndScope();
        scb.EndScope();

        context.AddSource("ApiFacadeExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteDocumentationGeneration(SourceProductionContext context,
        ImmutableArray<EventSourceData> events, ImmutableArray<RequestData> requests, string projectNamespace)
    {
        var mdb = new MarkdownBuilder();
        mdb.AddHeader("API Documentation");
        mdb.AddParagraph(
            $"Auto-generated documentation for the available endpoints. Total endpoints: {requests.Length}");

        if (requests.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No endpoints defined._");
        }
        else
        {
            mdb.AddHeader("Endpoints Overview", 2);

            var rows = new List<List<string>>();
            foreach (var handler in requests)
                rows.Add([
                    $"`{handler.HttpMethod}`", $"{handler.RequiresAuth}", $"`{handler.Route}`",
                    handler.RequestShortName,
                    handler.DataStructureType
                ]);

            mdb.AddTable(new List<string> { "Method", "Requires Auth", "Route", "Command/Record", "Type" }, rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Request Definitions", 2);

            foreach (var request in requests)
            {
                if (request == null) continue;

                mdb.AddHeader(request.RequestShortName, 3);
                mdb.AddParagraph($"Full Type: `{request.RequestFullName}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {request.RequestShortName}");

                foreach (var member in request.Members) mdb.AddLine(member);

                mdb.EndCodeBlock();
            }
        }

        mdb.AddHorizontalRule();

        mdb.AddHeader("Event Documentation");
        mdb.AddParagraph(
            $"Auto-generated documentation for the distributed events. Total events: {events.Length}");

        if (events.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No endpoints defined._");
        }
        else
        {
            foreach (var @event in events)
            {
                if (@event == null) continue;

                mdb.AddHeader(@event.TypeName, 3);
                mdb.AddParagraph($"Full Type: `{@event.FullTypeName}` ");
                mdb.AddParagraph($"Deserialization reference: `{@event.EventType}` ");

                mdb.StartCodeBlock();
                mdb.AddLine($"// Structure of {@event.TypeName}");

                foreach (var member in @event.Properties)
                {
                    mdb.AddLine($"public {member.Type} {member.Name} " + "{ get; }");
                }

                mdb.EndCodeBlock();
            }
        }

        SourceCodeBuilder scb = new();
        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public static class ApiDocumentation");
        scb.AddLine();

        scb.AddLine("private static string Markdown => \"\"\"");

        var lines = mdb.ToString().Split(["\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines) scb.AddLine(line);

        scb.AddLine("\"\"\";");

        scb.AddLine();
        scb.StartScope("public static void PrintToPath(string path)");
        scb.AddLine("File.WriteAllText(path, Markdown);");
        scb.EndScope();
        scb.EndScope();

        context.AddSource("ApiDocumentation.g.cs",
            SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteWebsocketReceiverGeneration(SourceProductionContext context,
        ImmutableArray<EventSourceData> events, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Net.WebSockets",
            "System.Text",
            "System.Text.Json",
            "Microsoft.Extensions.Logging"
        ]);

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope(
            "public class WebSocketService(global::Microsoft.Extensions.Logging.ILogger<WebSocketService> logger, IEventPublisher eventPublisher) : IWebSocketService ");

        scb.AddLine("private string _token;");
        scb.AddLine();
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("_token = token;");
        scb.EndScope();

        scb.AddLine("private ClientWebSocket _ws = new ClientWebSocket();");
        scb.AddLine();

        scb.StartScope("public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)");
        scb.AddLine(
            "if (string.IsNullOrEmpty(_token)) throw new InvalidOperationException(\"Token is null or empty.\");");
        scb.AddLine();
        scb.StartScope("if (_ws.State == WebSocketState.Open)");
        scb.AddLine("logger.LogWarning(\"WebSocket is already open\");");
        scb.AddLine("return;");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("_ws = new ClientWebSocket();");
        scb.AddLine("_ws.Options.SetRequestHeader(\"Authorization\", $\"Bearer {_token}\");");
        scb.AddLine("await _ws.ConnectAsync(uri, CancellationToken.None);");
        scb.AddLine("_ = ReceiveLoop();");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("public async Task DisposeAsync()");
        scb.AddLine("if (_ws.State == WebSocketState.Open)");
        scb.AddIndentedLine(
            "await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, \"Closing\", CancellationToken.None);");
        scb.AddLine("_ws.Dispose();");
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("private async Task ReceiveLoop()");
        scb.AddLine("var buffer = new byte[1024 * 4];");
        scb.StartScope("while (_ws.State == WebSocketState.Open)");
        scb.AddLine("using var ms = new MemoryStream();");
        scb.AddLine("WebSocketReceiveResult result;");
        scb.StartScope("do");
        scb.AddLine("result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);");
        scb.AddLine("ms.Write(buffer, 0, result.Count);");
        scb.EndScope(" while (!result.EndOfMessage);");
        scb.AddLine();
        scb.AddLine("if (result.MessageType == WebSocketMessageType.Close) break;");
        scb.AddLine("if (result.MessageType != WebSocketMessageType.Text) continue;");
        scb.AddLine("ms.Seek(0, SeekOrigin.Begin);");
        scb.AddLine("using var reader = new StreamReader(ms, Encoding.UTF8);");
        scb.AddLine("var message = await reader.ReadToEndAsync();");
        scb.AddLine();
        scb.AddLine("var eventEnvelope = JsonSerializer.Deserialize<EventEnvelope>(message);");
        scb.AddLine("if (eventEnvelope != null) await PublishEvent(eventEnvelope);");
        scb.EndScope();
        scb.EndScope();
        scb.AddLine();

        scb.StartScope("private async Task PublishEvent(EventEnvelope envelope)");
        scb.StartScope("switch (envelope.Type)");

        foreach (var eventType in events)
        {
            if (eventType == null) continue;

            scb.AddLine($"case \"{eventType.EventType}\":");
            scb.AddIndentedLine(
                $"await eventPublisher.PublishAsync(JsonSerializer.Deserialize<{eventType.FullTypeName}>(envelope.Payload)!);");
            scb.AddLine("break;");
        }

        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        context.AddSource("WebSocketService.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteWebsocketInterfaceGeneration(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["System.Net.Http"]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IWebSocketService");
        scb.AddLine("void SetToken(string _token);");
        scb.AddLine("Task ConnectAsync(Uri webSocketUri, CancellationToken ctsToken);");
        scb.AddLine("Task DisposeAsync();");
        scb.EndScope();

        context.AddSource("IWebSocketService.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteHttpClientGeneration(SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings(["System.Net.Http"]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IApiClient");
        scb.AddLine("HttpClient Client { get; }");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public class ApiHttpClient(HttpClient client) : IApiClient");
        scb.AddLine("public HttpClient Client => client;");
        scb.EndScope();

        context.AddSource("ApiClient.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteRequestSenderGeneration(SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public class TokenInjection");
        scb.AddLine("private string _token = string.Empty;");
        scb.AddLine("protected string Token => _token;");
        scb.AddLine();
        scb.StartScope("public void SetToken(string token)");
        scb.AddLine("_token = token;");
        scb.EndScope();
        scb.EndScope();

        context.AddSource("TokenInjection.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        
        foreach (var request in requests)
        {
            scb = new SourceCodeBuilder();
            scb.SetUsings([
                "System.Net.Http.Headers",
                "System.Net.Http.Json"
            ]);
            scb.SetNamespace($"{projectNamespace}.Generated");

            scb.StartScope($"public interface I{request.RequestShortName}Sender");
            scb.AddLine(
                $"Task<{request.ReturnValueFullName}> SendAsync({request.RequestFullName} {request.CqsType.ToLower()}, CancellationToken ct = default);");
            scb.EndScope();
            scb.AddLine();

            scb.StartScope(
                $"public class Generated{request.RequestShortName}Sender(IApiClient http) : TokenInjection, I{request.RequestShortName}Sender");
            
            scb.StartScope(
                $"public async Task<{request.ReturnValueFullName}> SendAsync({request.RequestFullName} command, CancellationToken ct = default)");

            if (request.RequiresAuth)
            {
                scb.AddLine("if (string.IsNullOrEmpty(Token))");
                scb.AddIndentedLine(
                    "throw new InvalidOperationException(\"Token is null or empty. Make sure you are logged in.\");");
                scb.AddLine();
                scb.StartScope(
                    $"var httpRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, \"{request.Route}\");");
                scb.AddLine("httpRequest.Content = JsonContent.Create(command);");
                scb.EndScope();
                scb.AddLine();
                scb.AddLine("httpRequest.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", Token);");
                scb.AddLine();
                scb.AddLine("var response = await http.Client.SendAsync(httpRequest, ct);");
                scb.AddLine();
                scb.AddLine("response.EnsureSuccessStatusCode();");
                scb.AddLine(
                    $"return (await response.Content.ReadFromJsonAsync<{request.ReturnValueFullName}>(ct))!;");
                scb.EndScope();
            }
            else
            {
                scb.AddLine($"var response = await http.Client.PostAsJsonAsync(\"{request.Route}\", command);");
                scb.AddLine();
                scb.AddLine("response.EnsureSuccessStatusCode();");
                scb.AddLine();
                scb.AddLine($"var result = await response.Content.ReadFromJsonAsync<{request.ReturnValueFullName}>();");
                scb.AddLine("return result!;");
                scb.EndScope();
            }

            scb.EndScope();
            context.AddSource($"Generated{request.RequestShortName}Sender.g.cs",
                SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }

    private static void ExecuteRequestSenderFacadesGeneration(SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace, string type)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Net.Http.Headers",
            "System.Net.Http.Json"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        var typedRequests = requests.Where(r => r.CqsType == type).ToImmutableList();

        var interfaceSignature = $"public interface I{type}Sender : ";

        foreach (var request in typedRequests)
        {
            if (request == typedRequests.Last())
            {
                interfaceSignature += $" I{request.RequestShortName}Sender";
                continue;
            }

            interfaceSignature += $" I{request.RequestShortName}Sender, ";
        }

        scb.StartScope(interfaceSignature);
        scb.AddLine("void InjectToken(string token);");
        scb.EndScope();
        scb.AddLine();

        var classSignature = $"public class Generated{type}Sender(";

        foreach (var request in typedRequests)
        {
            if (request == typedRequests.Last())
            {
                classSignature +=
                    $" I{request.RequestShortName}Sender {request.RequestShortName.ToLower()}sender) : I{type}Sender";
                continue;
            }

            classSignature += $" I{request.RequestShortName}Sender {request.RequestShortName.ToLower()}sender,";
        }

        scb.StartScope(classSignature);


        var tokenInjections = new List<string>();
        foreach (var request in requests.Where(r => r.CqsType == type))
        {
            scb.AddLine(
                $"public async Task<{request.ReturnValueFullName}> SendAsync({request.RequestFullName} query, CancellationToken ct = default) => await {request.RequestShortName.ToLower()}sender.SendAsync(query, ct);");
            tokenInjections.Add($"({request.RequestShortName.ToLower()}sender as TokenInjection)?.SetToken(token);");
        }

        scb.AddLine();
        scb.StartScope("public void InjectToken(string token)");
        foreach (var tokenInjection in tokenInjections) scb.AddLine(tokenInjection);
        scb.EndScope();
        scb.AddLine();

        scb.EndScope();

        context.AddSource($"{type}Sender.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}