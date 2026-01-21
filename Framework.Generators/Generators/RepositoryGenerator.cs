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
        var entities = context.GetAttributeAnnotatedClassSymbols("DomainEntity");

        context.RegisterSourceOutput(entities,
            static (spc, domainEntities) => Execute(spc, domainEntities));

        context.RegisterSourceOutput(entities,
            static (spc, domainEntities) => ExecuteDocuGeneration(spc, domainEntities));
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> domainEntities)
    {
        CreateRepositories(context, domainEntities);
        CreateExtensionMethod(context, domainEntities);
    }

    private static void CreateExtensionMethod(SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> domainEntities)
    {
        var scb = new SourceCodeBuilder();

        scb.SetUsings([
            "System", "System.Collections.Generic", "System.Threading",
            "System.Threading.Tasks", "Microsoft.Extensions.DependencyInjection"
        ]);
        scb.SetNamespace("Framework.Generated");

        scb.StartScope("public static class RepositoryExtensions");
        scb.StartScope("public static void AddSingletonRepositoryServices(this IServiceCollection services)");
        scb.AddLine(
            "services.AddSingleton<global::Framework.Contract.Documentation.IDocumentation, RepositoryDocumentation>();");
       
        if (!domainEntities.IsDefaultOrEmpty)
        {
            foreach (var domainEntity in domainEntities)
            {
                if (domainEntity == null) continue;
                
                var globalEntityString = domainEntity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                
                scb.AddLine(
                    $"services.AddSingleton<global::Framework.Contract.Repository.IRepository<{globalEntityString}>, {domainEntity.Name}MockRepository>();");
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
            scb.SetNamespace("Framework.Generated");

            scb.StartScope(
                $"public class {entityType}MockRepository : global::Framework.Contract.Repository.IRepository<{fullyQualifiedEntity}>");

            scb.AddLine($"private readonly List<{fullyQualifiedEntity}> {entityType}List = [];");
            scb.AddLine();

            scb.StartScope($"public Task AddAsync({fullyQualifiedEntity} entity)");
            scb.AddLine($"{entityType}List.Add(entity);");
            scb.AddLine("return Task.CompletedTask;");
            scb.EndScope();
            scb.AddLine();

            scb.StartScope($"public Task<{fullyQualifiedEntity}> GetByIdAsync(Guid id)");
            scb.AddLine($"return Task.FromResult({entityType}List.FirstOrDefault(e => e.Id == id));");
            scb.EndScope();
            scb.AddLine();

            scb.AddLine(
                $"public Task<List<{fullyQualifiedEntity}>> GetAllAsync() => Task.FromResult({entityType}List);");
            scb.AddLine();

            scb.StartScope($"public Task UpdateAsync({fullyQualifiedEntity} entity)");
            scb.AddLine($"var index = {entityType}List.FindIndex(e => e.Id == entity.Id);");
            scb.AddLine($"if(index != -1) {entityType}List[index] = entity;");
            scb.AddLine("return Task.CompletedTask;");
            scb.EndScope();
            scb.AddLine();

            scb.StartScope("public Task DeleteAsync(Guid id)");
            scb.AddLine($"var {entityType}Item = {entityType}List.FirstOrDefault(e => e.Id == id);");
            scb.AddLine($"if({entityType}Item == null) return Task.CompletedTask;");
            scb.AddLine($"{entityType}List.Remove({entityType}Item);");
            scb.AddLine("return Task.CompletedTask;");
            scb.EndScope();
            scb.AddLine();

            scb.AddLine(
                $"public Task<bool> ExistsAsync(Guid id) => Task.FromResult({entityType}List.Any(e => e.Id == id));");

            scb.EndScope();

            context.AddSource($"{entityType}MockRepository.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
        }
    }

    private static void ExecuteDocuGeneration(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> entities)
    {
        var mdb = new MarkdownBuilder();

        mdb.AddHeader("Repository & Entity Documentation");
        mdb.AddParagraph("This document lists all Domain Entities and their generated Mock Repositories.");

        if (entities.IsDefaultOrEmpty)
        {
            mdb.AddParagraph("_No Domain Entities found._");
        }
        else
        {
            mdb.AddHeader("Persistence Overview", 2);
            
            var rows = new List<List<string>>();
            foreach (var entity in entities)
            {
                if (entity == null) continue;

                var entityName = entity.Name;
                var repoName = $"{entityName}MockRepository";
                var @namespace = entity.ContainingNamespace.ToDisplayString();

                rows.Add([entityName, repoName, @namespace]);
            }

            mdb.AddTable(["Entity", "Generated Repository", "Original Namespace"], rows);

            mdb.AddHorizontalRule();
            mdb.AddHeader("Entity Details", 2);

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                mdb.AddHeader(entity.Name, 3);
                mdb.AddParagraph($"The entity `{entity.Name}` has an automatically generated In-Memory Repository for testing and local development.");
                
                mdb.AddListItem($"**Repository Class:** `{entity.Name}MockRepository`", 0);
                mdb.AddListItem($"**Interface:** `IRepository<{entity.Name}>`", 0);
                mdb.AddListItem($"**Full Entity Path:** `{entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}`", 0);
                
                mdb.AddLine();
            }
        }
        
        SourceCodeBuilder scb = new();
        scb.SetNamespace("Framework.Generated");
        scb.StartScope("public class RepositoryDocumentation : global::Framework.Contract.Documentation.IDocumentation");
        scb.AddLine();

        scb.AddLine("public string FileName => \"RepositoryDocumentation.md\";");
        scb.AddLine("public string Markdown => \"\"\"");

        var lines = mdb.ToString().Split(["\n", "\r"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            scb.AddLine(line);
        }

        scb.AddLine("\"\"\";");
        scb.EndScope();

        context.AddSource("RepositoryDocumentation.g.cs", SourceText.From(scb.ToString(), Encoding.UTF8));
    }
}