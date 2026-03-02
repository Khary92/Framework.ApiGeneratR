using System.Text;
using ApiGeneratR.CodeGen.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.CodeGen;

[Generator(LanguageNames.CSharp)]
public class AttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ExecuteEventAttributeGeneration(ctx, "ApiGeneratR.Definitions");
            ExecuteRequestAttributeGeneration(ctx, "ApiGeneratR.Definitions");
        });
    }
    
    private static void ExecuteRequestAttributeGeneration(IncrementalGeneratorPostInitializationContext spc,
        string? projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.AddLine("[AttributeUsage(AttributeTargets.Class)]");
        scb.StartScope(
            "internal class RequestAttribute(string route, bool requiresAuth, RequestType requestType, HttpMethod method = HttpMethod.Post) : Attribute");
        scb.AddLine("public string Route { get; } = route;");
        scb.AddLine("public bool RequiresAuth { get; set; } = requiresAuth;");
        scb.AddLine("public HttpMethod Method { get; } = method;");
        scb.AddLine("public RequestType RequestType { get; } = requestType;");
        scb.EndScope();

        spc.AddSource("RequestAttribute.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));

        scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("internal enum HttpMethod");
        scb.AddLine("Get,");
        scb.AddLine("Post,");
        scb.AddLine("Put,");
        scb.AddLine("Delete,");
        scb.AddLine("Patch");
        scb.EndScope();
        spc.AddSource("HttpMethod.g.cs", scb.ToString());

        scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");
        scb.StartScope("internal enum RequestType");
        scb.AddLine("Query,");
        scb.AddLine("Command");
        scb.EndScope();

        spc.AddSource("RequestType.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteEventAttributeGeneration(IncrementalGeneratorPostInitializationContext spc,
        string? projectNamespace)
    {
        var scb = new SourceCodeBuilder();

        scb.SetNamespace($"{projectNamespace}.Generated");

        scb.AddLine("[AttributeUsage(AttributeTargets.Class)]");
        scb.StartScope("internal class EventAttribute(string eventType) : Attribute");
        scb.AddLine("public string EventType { get; } = eventType;");
        scb.EndScope();

        spc.AddSource("EventAttribute.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}