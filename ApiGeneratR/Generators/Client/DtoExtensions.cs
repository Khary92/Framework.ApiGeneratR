using System.Collections.Immutable;
using System.Linq;
using ApiGeneratR.Builder;
using ApiGeneratR.Mapper;

namespace ApiGeneratR.Generators.Client;

public static class DtoExtensions
{
    public static void AddTranspiledDtos(this TranspilerBuilder transpilerBuilder, ImmutableArray<DtoData> dtos,
        ImmutableArray<EventData> events, ImmutableArray<RequestData> requests)
    {
        foreach (var dto in dtos)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerStatics.TranspilerNamespace);
            var parameters = string.Join(", ", dto.Properties.Select(p => $"{p.Type} {p.Name}"));
            builder.AddLine($"public record {dto.TypeName}({parameters});");
            transpilerBuilder.AddFile($"{dto.TypeName}.g.cs", builder.ToString());
        }

        foreach (var @event in events)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerStatics.TranspilerNamespace);
            builder.AddLine($"public record {@event.TypeName}({string.Join(", ", @event.Properties.Select(p => $"{p.Type} {p.Name}"))});");
            transpilerBuilder.AddFile($"{@event.TypeName}.g.cs", builder.ToString());
        }

        foreach (var request in requests)
        {
            var builder = new SourceCodeBuilder();
            builder.SetNamespace(TranspilerStatics.TranspilerNamespace);
            builder.AddLine($"public record {request.RequestShortName}({string.Join(", ", request.Properties.Select(p => $"{p.Type} {p.Name}"))});");
            transpilerBuilder.AddFile($"{request.RequestShortName}.g.cs", builder.ToString());
        }
    }
}