using Microsoft.Extensions.DependencyInjection;
using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Domain.Models;
using RepositoryIntelligence.Infrastructure;

if (args.Length == 0)
{
    PrintHelp();
    return 0;
}

var dbPath = Path.Combine(AppContext.BaseDirectory, "repository-intelligence.db");
var services = new ServiceCollection();
services.AddRepositoryIntelligence(dbPath);
var provider = services.BuildServiceProvider();

var command = args[0];

try
{
    return command switch
    {
        "index"           => await RunIndexAsync(args, provider),
        "analyze"         => await RunAnalyzeAsync(args, provider),
        "search"          => await RunSearchAsync(args, provider),
        "summary"         => await RunSummaryAsync(args, provider),
        "generate-prompt" => await RunGeneratePromptAsync(args, provider),
        "--help" or "-h"  => ShowHelp(),
        _ => ShowUnknown(command)
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static string? GetArg(string[] args, string flag)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    return null;
}

static string RequireArg(string[] args, string flag)
    => GetArg(args, flag) ?? throw new InvalidOperationException($"Missing required argument: {flag}");

// ---- index ----
static async Task<int> RunIndexAsync(string[] args, IServiceProvider provider)
{
    var path   = RequireArg(args, "--path");
    var name   = RequireArg(args, "--name");
    var branch = GetArg(args, "--branch") ?? "main";
    var modeStr = GetArg(args, "--mode") ?? "incremental";

    var mode = modeStr.Equals("full", StringComparison.OrdinalIgnoreCase)
        ? IndexingMode.Full : IndexingMode.Incremental;

    var indexer = provider.GetRequiredService<IRepositoryIndexer>();
    Console.WriteLine($"Indexing '{name}' ({branch}) [{mode}] from: {path}");

    var summary = await indexer.IndexRepositoryAsync(new IndexRepositoryCommand
    {
        RepositoryPath = Path.GetFullPath(path),
        RepositoryName = name,
        BranchName = branch,
        Mode = mode
    });

    Console.WriteLine();
    Console.WriteLine("Repository indexed successfully.");
    Console.WriteLine();
    Console.WriteLine($"Added files:     {summary.AddedFiles}");
    Console.WriteLine($"Updated files:   {summary.UpdatedFiles}");
    Console.WriteLine($"Deleted files:   {summary.DeletedFiles}");
    Console.WriteLine($"Unchanged files: {summary.UnchangedFiles}");
    Console.WriteLine($"Added chunks:    {summary.AddedChunks}");
    Console.WriteLine($"Deleted chunks:  {summary.DeletedChunks}");
    Console.WriteLine($"Duration:        {summary.Duration:hh\\:mm\\:ss\\.fff}");
    return 0;
}

// ---- analyze ----
static async Task<int> RunAnalyzeAsync(string[] args, IServiceProvider provider)
{
    var name   = RequireArg(args, "--name");
    var branch = GetArg(args, "--branch") ?? "main";
    var issue  = RequireArg(args, "--issue");

    var analyzer = provider.GetRequiredService<IIssueAnalyzer>();
    var result = await analyzer.AnalyzeAsync(new AnalyzeIssueCommand
    {
        RepositoryName = name,
        BranchName = branch,
        Issue = issue
    });

    Console.WriteLine("Issue:");
    Console.WriteLine(result.Issue);
    Console.WriteLine();
    Console.WriteLine("Recommended files:");
    Console.WriteLine();

    for (int i = 0; i < result.RecommendedFiles.Count; i++)
    {
        var rec = result.RecommendedFiles[i];
        Console.WriteLine($"{i + 1}. {rec.FilePath}");
        Console.WriteLine($"   Confidence: {rec.Confidence:F2}");
        Console.WriteLine($"   Reason: {rec.Reason}");
        if (rec.RelatedSymbols.Count > 0)
        {
            Console.WriteLine("   Related symbols:");
            foreach (var s in rec.RelatedSymbols.Take(5))
                Console.WriteLine($"   - {s}");
        }
        Console.WriteLine($"   Suggested check: {rec.SuggestedCheck}");
        Console.WriteLine();
    }
    return 0;
}

// ---- search ----
static async Task<int> RunSearchAsync(string[] args, IServiceProvider provider)
{
    var name   = RequireArg(args, "--name");
    var branch = GetArg(args, "--branch") ?? "main";
    var query  = RequireArg(args, "--query");

    var searcher = provider.GetRequiredService<ISearchService>();
    var results = await searcher.SearchAsync(new SearchCodeCommand
    {
        RepositoryName = name,
        BranchName = branch,
        Query = query,
        MaxResults = 10
    });

    Console.WriteLine($"Search results for: {query}");
    Console.WriteLine();
    foreach (var r in results)
    {
        Console.WriteLine($"[{r.Score:F2}] {r.FilePath}:{r.StartLine}-{r.EndLine} ({r.SymbolName})");
        Console.WriteLine($"  {r.MatchReason}");
        Console.WriteLine($"  {r.Text.Split('\n')[0].Trim()}...");
        Console.WriteLine();
    }

    if (results.Count == 0)
        Console.WriteLine("No results found.");
    return 0;
}

// ---- summary ----
static async Task<int> RunSummaryAsync(string[] args, IServiceProvider provider)
{
    var name   = RequireArg(args, "--name");
    var branch = GetArg(args, "--branch") ?? "main";

    var store = provider.GetRequiredService<IMetadataStore>();
    await store.InitializeAsync();
    var summary = await store.GetIndexingSummaryAsync(name, branch);
    Console.WriteLine($"Summary for '{name}' ({branch}):");
    Console.WriteLine($"  Indexed files:  {summary.AddedFiles}");
    Console.WriteLine($"  Deleted files:  {summary.DeletedFiles}");
    Console.WriteLine($"  Indexed chunks: {summary.AddedChunks}");
    return 0;
}

// ---- generate-prompt ----
static async Task<int> RunGeneratePromptAsync(string[] args, IServiceProvider provider)
{
    var name   = RequireArg(args, "--name");
    var branch = GetArg(args, "--branch") ?? "main";
    var issue  = RequireArg(args, "--issue");

    var analyzer  = provider.GetRequiredService<IIssueAnalyzer>();
    var generator = provider.GetRequiredService<ICopilotPromptGenerator>();

    var analysisResult = await analyzer.AnalyzeAsync(new AnalyzeIssueCommand
    {
        RepositoryName = name,
        BranchName = branch,
        Issue = issue
    });

    var prompt = await generator.GeneratePromptAsync(analysisResult);
    Console.WriteLine(prompt);
    return 0;
}

static void PrintHelp()
{
    Console.WriteLine("Repository Intelligence — index and analyze a source code repository.");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  index           --path <path> --name <name> [--branch <branch>] [--mode full|incremental]");
    Console.WriteLine("  analyze         --name <name> [--branch <branch>] --issue \"<issue>\"");
    Console.WriteLine("  search          --name <name> [--branch <branch>] --query \"<query>\"");
    Console.WriteLine("  summary         --name <name> [--branch <branch>]");
    Console.WriteLine("  generate-prompt --name <name> [--branch <branch>] --issue \"<issue>\"");
}

static int ShowHelp() { PrintHelp(); return 0; }
static int ShowUnknown(string cmd) { Console.Error.WriteLine($"Unknown command: {cmd}"); return 1; }
