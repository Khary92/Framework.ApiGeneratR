using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Generators.Client;

public static class DtoExtensions
{
    public static void AddTranspiledDtos(this TranspilerBuilder transpilerBuilder, ImmutableArray<DtoData> dtos,
        ImmutableArray<EventData> events, ImmutableArray<RequestData> requests, ImmutableArray<ApiEnumData> enums)
    {
        foreach (var dto in dtos)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            var parameters = string.Join(", ", dto.Properties.Select(p => $"{p.Type.Split('.').Last()} {p.Name}"));
            builder.AddLine($"public record {dto.TypeName}({parameters});");
            transpilerBuilder.AddFile($"{dto.TypeName}.g.cs", builder.ToString());
        }

        foreach (var @event in events)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            builder.AddLine(
                $"public record {@event.TypeName}({string.Join(", ", @event.Properties.Select(p => $"{p.Type.Split('.').Last()} {p.Name}"))});");
            transpilerBuilder.AddFile($"{@event.TypeName}.g.cs", builder.ToString());
        }

        foreach (var request in requests)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            builder.AddLine(
                $"public record {request.RequestShortName}({string.Join(", ", request.Properties.Select(p => $"{p.Type.Split('.').Last()} {p.Name}"))});");
            transpilerBuilder.AddFile($"{request.RequestShortName}.g.cs", builder.ToString());
        }

        foreach (var @enum in enums)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerBuilder.TranspilerNamespace + ".Generated");
            builder.StartScope($"public enum {@enum.Name}");
            builder.AddLine(string.Join(", ", @enum.Fields));
            builder.EndScope();
            transpilerBuilder.AddFile($"{@enum.Name}.g.cs", builder.ToString());
        }
    }
}