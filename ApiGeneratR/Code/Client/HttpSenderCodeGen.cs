using System;
using ApiGeneratR.Code.Builder;

namespace ApiGeneratR.Code.Client;

public static class HttpSenderCodeGen
{
    public static SourceCodeFile Create(Language targetLanguage, string projectNamespace)
    {
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                return CreateCSharpApiClientWithInterface(projectNamespace + ".Generated");
            case Language.CSharpTranspiled:
                return CreateCSharpApiClientWithInterface(TranspilerBuilder.TranspilerNamespace + ".Generated");
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static SourceCodeFile CreateCSharpApiClientWithInterface(string projectNamespace)
    {
        var scb = new SourceCodeBuilder();
        
        scb.SetUsings(["System.Net.Http"]);
        scb.SetNamespace(projectNamespace);

        scb.StartScope("public interface IApiClient");
        scb.AddLine("HttpClient Client { get; }");
        scb.EndScope();
        scb.AddLine();
        scb.StartScope("public class ApiHttpClient(HttpClient client) : IApiClient");
        scb.AddLine("public HttpClient Client => client;");
        scb.EndScope();

        return new SourceCodeFile("HttpClient.g.cs", scb.ToString());
    }
}