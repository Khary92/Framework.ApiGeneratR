using System.Text;
using ApiGeneratR.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Generators.PostInitialization;

public static class PostInitializationOutputExtensions
{
    public static void GenerateIRequestTaggingInterface(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();

            scb.SetNamespace("ApiGeneratR.Tags");

            scb.AddLine("internal interface RequestResponseTag<TResponse>;");

            ctx.AddSource("RequestResponseTag.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        });
    }

    public static void GenerateRequestHandlerAttribute(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();

            scb.SetNamespace("ApiGeneratR.Attributes");

            scb.AddLine("[AttributeUsage(AttributeTargets.Class)]");
            scb.StartScope(
                "internal class RequestHandlerAttribute(Type requestType) : Attribute");
            scb.AddLine("public Type RequestFullName { get; } = requestType;");
            scb.EndScope();

            ctx.AddSource("RequestHandlerAttribute.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        });
    }
    
    public static void GenerateRequestAttribute(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();

            scb.SetNamespace("ApiGeneratR.Attributes");

            scb.AddLine("[AttributeUsage(AttributeTargets.Class)]");
            scb.StartScope(
                "internal class RequestAttribute(string route, string authPolicy, RequestType requestType) : Attribute");
            scb.AddLine("public string Route { get; } = route;");
            scb.AddLine("public string AuthPolicy { get; set; } = authPolicy;");
            scb.AddLine("public RequestType RequestType { get; } = requestType;");
            scb.EndScope();

            ctx.AddSource("RequestAttribute.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        });
    }

    public static void GenerateApiConsumerAttribute(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();

            scb.SetNamespace("ApiGeneratR.Attributes");

            scb.AddLine("[AttributeUsage(AttributeTargets.Class)]");
            scb.StartScope(
                "internal class ApiConsumerAttribute : Attribute");
            scb.AddLine("public Type[] EventSubscriptionTypes { get; }");
            scb.AddLine();
            scb.StartScope("public ApiConsumerAttribute(params Type[] eventSubscriptionTypes)");
            scb.AddLine("EventSubscriptionTypes = eventSubscriptionTypes;");
            scb.EndScope();
            scb.AddLine();
            scb.StartScope("public ApiConsumerAttribute()");
            scb.AddLine("EventSubscriptionTypes = Array.Empty<Type>();");
            scb.EndScope();
            scb.EndScope();

            ctx.AddSource("ApiConsumerAttribute.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        });
    }

    public static void GenerateRequestTypeAttribute(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();

            scb.SetNamespace("ApiGeneratR.Attributes");


            scb.SetNamespace("ApiGeneratR.Attributes");
            scb.StartScope("internal enum RequestType");
            scb.AddLine("Query,");
            scb.AddLine("Command");
            scb.EndScope();

            ctx.AddSource("RequestType.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        });
    }

    public static void GenerateEventAttributeAttribute(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();

            scb.SetNamespace("ApiGeneratR.Attributes");

            scb.AddLine("[AttributeUsage(AttributeTargets.Class)]");
            scb.StartScope("internal class EventAttribute(string eventType) : Attribute");
            scb.AddLine("public string EventType { get; } = eventType;");
            scb.EndScope();

            ctx.AddSource("EventAttribute.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        });
    }
}