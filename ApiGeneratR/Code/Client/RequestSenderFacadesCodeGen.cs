using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Builder;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Client;

public static class RequestSenderFacadesCodeGen
{
    public static List<SourceCodeFile> Create(Language targetLanguage, ImmutableArray<RequestData> requests,
        string projectNamespace)
    {
        var sourceFiles = new List<SourceCodeFile>();
        switch (targetLanguage)
        {
            case Language.CSharpWeb:
                sourceFiles.Add(CreateCSharpCommandSenderFacade(requests, projectNamespace + ".Generated"));
                sourceFiles.Add(CreateCSharpQuerySenderFacade(requests, projectNamespace + ".Generated"));
                return sourceFiles;
            case Language.CSharpTranspiled:
                sourceFiles.Add(CreateCSharpCommandSenderFacade(requests, TranspilerBuilder.TranspilerNamespace + ".Generated"));
                sourceFiles.Add(CreateCSharpQuerySenderFacade(requests, TranspilerBuilder.TranspilerNamespace + ".Generated"));
                return sourceFiles;
            default:
                throw new NotSupportedException(
                    $"Language {targetLanguage} is not supported for ApiContainer generation.");
        }
    }

    private static SourceCodeFile CreateCSharpCommandSenderFacade(ImmutableArray<RequestData> requests,
        string projectNamespace)
    {
        return InternalCreateRequestSenderFacades(requests, projectNamespace, "Command");
    }

    private static SourceCodeFile CreateCSharpQuerySenderFacade(ImmutableArray<RequestData> requests,
        string projectNamespace)
    {
        return InternalCreateRequestSenderFacades(requests, projectNamespace, "Query");
    }

    private static SourceCodeFile InternalCreateRequestSenderFacades(ImmutableArray<RequestData> requests,
        string projectNamespace, string type)
    {
        var scb = new SourceCodeBuilder();
        scb.SetUsings([
            "System.Net.Http.Headers",
            "System.Net.Http.Json"
        ]);
        scb.SetNamespace(projectNamespace);

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

        return new SourceCodeFile($"{type}Sender.g.cs", scb.ToString());
    }
}