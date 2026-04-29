using Microsoft.Extensions.DependencyInjection;
using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Infrastructure.Analysis;
using RepositoryIntelligence.Infrastructure.Embeddings;
using RepositoryIntelligence.Infrastructure.FileSystem;
using RepositoryIntelligence.Infrastructure.Indexing;
using RepositoryIntelligence.Infrastructure.Parsing;
using RepositoryIntelligence.Infrastructure.Search;
using RepositoryIntelligence.Infrastructure.Storage;

namespace RepositoryIntelligence.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoryIntelligence(
        this IServiceCollection services,
        string databasePath)
    {
        services.AddSingleton<IPathFilter, GitIgnoreStylePathFilter>();
        services.AddSingleton<IFileScanner, LocalFileScanner>();
        services.AddSingleton<IChunkingService, TextChunkingService>();
        services.AddSingleton<IEmbeddingService, LocalHashEmbeddingService>();
        services.AddSingleton<IVectorSearchService, InMemoryVectorSearchService>();
        services.AddSingleton<IMetadataStore>(new SqliteMetadataStore(databasePath));
        services.AddSingleton<ICodeParser, RoslynCodeParser>();
        services.AddSingleton<IRepositoryIndexer, RepositoryIndexer>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IIssueAnalyzer, IssueAnalyzer>();
        services.AddSingleton<ICopilotPromptGenerator, CopilotPromptGenerator>();
        return services;
    }
}
