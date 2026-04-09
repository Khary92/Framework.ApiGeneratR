using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;
using Microsoft.CodeAnalysis.Text;

namespace ApiGeneratR.Code.Server;

public static class MediatorInterfaceCodeGen
{
    public static List<SourceCodeFile> Create(ImmutableArray<RequestData> requests, string? projectNamespace)
    {
        var result = new List<SourceCodeFile>();
        var mediatorInterfaces = string.Empty;
        foreach (var request in requests)
        {
            if (request == null) continue;

            var scb = new SourceCodeBuilder();

            scb.SetNamespace($"{projectNamespace}.Generated");

            scb.StartScope($"public interface I{request.RequestShortName}Handler");
            scb.AddLine(
                $"Task<{request.ReturnValueFullName}> HandleAsync({request.RequestFullName} request, CancellationToken ct = default);");
            scb.EndScope();

            result.Add(new SourceCodeFile($"I{request.RequestShortName}Handler.g.cs", scb.ToString()));

            mediatorInterfaces += requests.Last() == request
                ? $"I{request.RequestShortName}Handler;"
                : $"I{request.RequestShortName}Handler, ";
        }

        var mscb = new SourceCodeBuilder();

        mscb.SetNamespace($"{projectNamespace}.Generated");

        mscb.AddLine("public interface IMediator : " + mediatorInterfaces);

        result.Add(new SourceCodeFile("IMediator.g.cs", mscb.ToString()));
        return result;
    }
}