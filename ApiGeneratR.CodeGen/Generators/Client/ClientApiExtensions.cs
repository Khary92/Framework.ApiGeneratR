using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ApiGeneratR.CodeGen.Builder;
using ApiGeneratR.CodeGen.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen.Generators.Client;

public static class ClientApiExtensions
{
    public static void CreateApiContainer(this SourceProductionContext ctx, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Threading"
        ]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IApiContainer");
        scb.AddLine("void SetToken(string token);");
        scb.AddLine("IEventSubscriber EventSubscriber { get; }");
        scb.AddLine("IEventPublisher EventPublisher { get; }");
        scb.AddLine("IWebSocketService WebSocket { get; }");
        scb.AddLine("ICommandSender Commands { get; }");
        scb.AddLine("IQuerySender Queries { get; }");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope(
            "public class ConsumerApi(IWebSocketService webSocket, ICommandSender commands, IQuerySender queries, IEventPublisher eventPublisher, IEventSubscriber eventSubscriber) : IApiContainer");
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

        AddSource(ctx, "ApiContainer.g.cs", scb.ToString());
    }

    public static void CreateClientApiExtensions(this SourceProductionContext ctx,
        ImmutableArray<RequestData> requests,
        string projectNamespace)
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
        scb.AddLine("services.AddSingleton<IApiContainer, ConsumerApi>();");
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

        AddSource(ctx, "ApiContainerExtensions.g.cs", scb.ToString());
    }


    public static void GenerateWebsocketReceiver(this SourceProductionContext ctx,
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

        AddSource(ctx, "WebSocketService.g.cs", scb.ToString());
    }

    public static void CreateTokenInjectorBaseClass(this SourceProductionContext context, string projectNamespace)
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

        AddSource(context, "TokenInjection.g.cs", scb.ToString());
    }

    public static void CreateAtomicRequestSenderWithInterfaces(this SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace)
    {
        foreach (var request in requests)
        {
            var scb = new SourceCodeBuilder();
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

            AddSource(context, $"Generated{request.RequestShortName}Sender.g.cs", scb.ToString());
        }
    }

    public static void CreateCommandSenderWithInterface(this SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace)
    {
        context.InternalCreateRequestSenderFacades(requests, projectNamespace, "Command");
    }

    public static void CreateQuerySenderWithInterface(this SourceProductionContext context,
        ImmutableArray<RequestData> requests, string projectNamespace)
    {
        context.InternalCreateRequestSenderFacades(requests, projectNamespace, "Query");
    }

    private static void InternalCreateRequestSenderFacades(this SourceProductionContext context,
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

        AddSource(context, $"{type}Sender.g.cs", scb.ToString());
    }

    public static void CreateWebsocketInterface(this SourceProductionContext context, string projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings(["System.Net.Http"]);
        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.StartScope("public interface IWebSocketService");
        scb.AddLine("void SetToken(string _token);");
        scb.AddLine("Task ConnectAsync(Uri webSocketUri, CancellationToken ctsToken);");
        scb.AddLine("Task DisposeAsync();");
        scb.EndScope();


        AddSource(context, "IWebSocketService.g.cs", scb.ToString());
    }

    public static void CreateApiClientWithInterface(this SourceProductionContext context, string projectNamespace)
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

        AddSource(context, "ApiClient.g.cs", scb.ToString());
    }

    public static void CreateEventBusWithInterfaces(this SourceProductionContext context, string? projectNamespace,
        GlobalOptions options)
    {
        if (projectNamespace != options.DefinitionsProject) return;

        var scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("public interface IEventPublisher");
        scb.AddLine("Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public interface IEventSubscriber");
        scb.AddLine("Task Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;");
        scb.AddLine("void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public class EventService : IEventSubscriber, IEventPublisher");
        scb.AddLine("private readonly Dictionary<Type, List<Delegate>> _handlers = new();");
        scb.AddLine("private readonly Lock _lock = new();");
        scb.StartScope("public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class");
        scb.AddLine("List<Delegate> handlersCopy;");
        scb.AddLine();
        scb.StartScope("lock (_lock)");
        scb.AddLine("if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;");
        scb.AddLine("handlersCopy = new List<Delegate>(handlers);");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("var tasks = handlersCopy.Cast<Func<TEvent, Task>>().Select(h => h(@event));");
        scb.AddLine("await Task.WhenAll(tasks);");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public Task Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class");
        scb.StartScope("lock (_lock)");
        scb.StartScope("if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))");
        scb.AddLine("handlers = [];");
        scb.AddLine("_handlers[typeof(TEvent)] = handlers;");
        scb.EndScope();
        scb.AddLine();
        scb.AddLine("handlers.Add(handler);");
        scb.EndScope();
        scb.AddLine("return Task.CompletedTask;");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class");
        scb.StartScope("lock (_lock)");
        scb.StartScope("if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;");
        scb.AddLine();
        scb.AddLine("handlers.Remove(handler);");
        scb.AddLine();
        scb.AddLine("if (handlers.Count == 0) _handlers.Remove(typeof(TEvent));");
        scb.EndScope();
        scb.EndScope();
        scb.EndScope();
        scb.EndScope();

        AddSource(context, "EventBus.g.cs", scb.ToString());
    }

    private static void AddSource(SourceProductionContext context, string fileName, string sourceCode)
    {
        context.AddSource(fileName, SourceText.From(sourceCode, Encoding.UTF8));
    }
}