using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Builder;
using ApiGeneratR.Code.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Code.Api;

public static class ApiClassCodeGen
{
    public static List<SourceCodeFile> Create(ImmutableArray<DtoData> dtos,
        ImmutableArray<EventData> events, ImmutableArray<RequestData> requests, ImmutableArray<ApiEnumData> enums)
    {
        return AddCSharpTranspiledDtos(dtos, events, requests, enums);
    }

    private static List<SourceCodeFile> AddCSharpTranspiledDtos(ImmutableArray<DtoData> dtos,
        ImmutableArray<EventData> events, ImmutableArray<RequestData> requests, ImmutableArray<ApiEnumData> enums)
    {
        var result = new List<SourceCodeFile>();
        foreach (var dto in dtos)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            var parameters = string.Join(", ", dto.Properties.Select(p => $"{p.Type.Split('.').Last()} {p.Name}"));
            builder.AddLine($"public record {dto.TypeName}({parameters});");
            result.Add(new SourceCodeFile($"{dto.TypeName}.g.cs", builder.ToString()));
        }

        foreach (var @event in events)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            builder.AddLine(
                $"public record {@event.TypeName}({string.Join(", ", @event.Properties.Select(p => $"{p.Type.Split('.').Last()} {p.Name}"))});");
            result.Add(new SourceCodeFile($"{@event.TypeName}.g.cs", builder.ToString()));
        }

        foreach (var request in requests)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            builder.AddLine(
                $"public record {request.RequestShortName}({string.Join(", ", request.Properties.Select(p => $"{p.Type.Split('.').Last()} {p.Name}"))});");
            result.Add(new SourceCodeFile($"{request.RequestShortName}.g.cs", builder.ToString()));
        }

        foreach (var @enum in enums)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            builder.StartScope($"public enum {@enum.Name}");
            builder.AddLine(string.Join(", ", @enum.Fields));
            builder.EndScope();
            result.Add(new SourceCodeFile($"{@enum.Name}.g.cs", builder.ToString()));
        }
        
        return result;
    }
}