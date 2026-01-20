using System.Collections.Immutable;
using System.Text;
using Framework.Generators.Builder;
using Framework.Generators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Framework.Generators.Generators;

[Generator(LanguageNames.CSharp)]
public class RepositoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.GetAttributeAnnotatedClassSymbols("DomainEntity"),
            static (spc, handlers) => Execute(spc, handlers));
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> handlers)
    {
        CreateRepositories(context, handlers);
        CreateExtensionMethod(context, handlers);
    }

    private static void CreateExtensionMethod(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> handlers)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System", "System.Collections.Generic", "System.Threading",
            "System.Threading.Tasks", "Microsoft.Extensions.DependencyInjection"
        ]);
        scb.SetNamespace("Repository.Generated");

        scb.StartScope("public static class RepositoryExtensions");
        scb.StartScope("public static void AddSingletonRepositoryServices(this IServiceCollection services)");

        if (!handlers.IsDefaultOrEmpty)
        {
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var interfaceSymbol = handler.AllInterfaces.FirstOrDefault(i =>
                    i.Name == "IRepository" &&
                    i.ContainingNamespace.ToDisplayString().Contains("Framework.Contract"));

                if (interfaceSymbol == null || interfaceSymbol.TypeArguments.Length != 1) continue;

                var entityType = interfaceSymbol.TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var repositoryType = handler.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                scb.AddLine(
                    $"services.AddSingleton<global::Framework.Contract.Repository.IRepository<{entityType}>, {repositoryType}>();");
            }
        }

        scb.EndScope();
        scb.EndScope();

        context.AddSource("RepositoryExtensions.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }

    private static void CreateRepositories(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> entities)
    {
        if (entities.IsDefaultOrEmpty) return;

        foreach (var entity in entities)
        {
            if (entity == null) continue;

            var entityType = entity.Name;
            var fullyQualifiedEntity = entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var scb = new SourceCodeBuilder();
            scb.SetUsings([
                "System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks",
                "Framework.Contract.Repository"
            ]);
            scb.SetNamespace("ApiGeneratR.Generated");

            scb.StartScope(
                $"public class {entityType}MockRepository : global::Framework.Contract.Repository.IRepository<{fullyQualifiedEntity}>");

            scb.AddLine($"private readonly List<{fullyQualifiedEntity}> {entityType}List = [];");
            scb.AddLine("");

            scb.StartScope($"public Task AddAsync({fullyQualifiedEntity} entity)");
            scb.AddLine($"{entityType}List.Add(entity);");
            scb.AddLine("return Task.CompletedTask;");
            scb.EndScope();
            scb.AddLine("");

            scb.StartScope($"public Task<{fullyQualifiedEntity}> GetByIdAsync(Guid id)");
            scb.AddLine($"return Task.FromResult({entityType}List.FirstOrDefault(e => e.Id == id));");
            scb.EndScope();
            scb.AddLine("");

            scb.AddLine(
                $"public Task<List<{fullyQualifiedEntity}>> GetAllAsync() => Task.FromResult({entityType}List);");
            scb.AddLine("");

            scb.StartScope($"public Task UpdateAsync({fullyQualifiedEntity} entity)");
            scb.AddLine($"var index = {entityType}List.FindIndex(e => e.Id == entity.Id);");
            scb.AddLine($"if(index != -1) {entityType}List[index] = entity;");
            scb.AddLine("return Task.CompletedTask;");
            scb.EndScope();
            scb.AddLine("");

            scb.StartScope("public Task DeleteAsync(Guid id)");
            scb.AddLine($"var {entityType}Item = {entityType}List.FirstOrDefault(e => e.Id == id);");
            scb.AddLine($"if({entityType}Item == null) return Task.CompletedTask;");
            scb.AddLine($"{entityType}List.Remove({entityType}Item);");
            scb.AddLine("return Task.CompletedTask;");
            scb.EndScope();
            scb.AddLine("");

            scb.AddLine(
                $"public Task<bool> ExistsAsync(Guid id) => Task.FromResult({entityType}List.Any(e => e.Id == id));");

            scb.EndScope();

            context.AddSource($"{entityType}MockRepository.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }
}