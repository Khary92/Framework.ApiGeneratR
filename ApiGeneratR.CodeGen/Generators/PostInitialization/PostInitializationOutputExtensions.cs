using System.Text;
using ApiGeneratR.CodeGen.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen.Generators.PostInitialization;

public static class PostInitializationOutputExtensions
{
    public static void GenerateRequestAttribute(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();

            scb.SetNamespace("ApiGeneratR.Attributes");

            scb.AddLine("[AttributeUsage(AttributeTargets.Class)]");
            scb.StartScope(
                "internal class RequestAttribute(string route, bool requiresAuth, RequestType requestType, HttpMethod method = HttpMethod.Post) : Attribute");
            scb.AddLine("public string Route { get; } = route;");
            scb.AddLine("public bool RequiresAuth { get; set; } = requiresAuth;");
            scb.AddLine("public HttpMethod Method { get; } = method;");
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
            scb.StartScope("public ApiConsumerAttribute(params Type[] eventSubscriptionTypes)");
            scb.AddLine("EventSubscriptionTypes = eventSubscriptionTypes;");
            scb.EndScope();
            scb.StartScope("public ApiConsumerAttribute()");
            scb.AddLine("EventSubscriptionTypes = Array.Empty<Type>();");
            scb.EndScope();
            scb.EndScope();

            ctx.AddSource("ApiConsumerAttribute.g.cs", scb.ToString());
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
    
    public static void GenerateHttpMethodEnum(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var scb = new SourceCodeBuilder();
            
            scb.SetNamespace("ApiGeneratR.Attributes");
            scb.StartScope("internal enum HttpMethod");
            scb.AddLine("Get,");
            scb.AddLine("Post,");
            scb.AddLine("Put,");
            scb.AddLine("Delete,");
            scb.AddLine("Patch");
            scb.EndScope();

            ctx.AddSource("HttpMethod.g.cs", scb.ToString());
        });
    }
}