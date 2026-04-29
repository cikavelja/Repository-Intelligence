using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Domain.Models;
using System.Security.Cryptography;
using System.Text;

namespace RepositoryIntelligence.Infrastructure.Parsing;

public sealed class RoslynCodeParser : ICodeParser
{
    private readonly IChunkingService _textChunker;

    public RoslynCodeParser(IChunkingService textChunker)
    {
        _textChunker = textChunker;
    }

    public bool CanParse(string filePath) =>
        filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    public Task<ParsedFile> ParseAsync(
        string filePath,
        string content,
        string repositoryName,
        string branchName,
        Guid repositoryDocumentId,
        CancellationToken cancellationToken = default)
    {
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = tree.GetCompilationUnitRoot();
        var text = tree.GetText();

        var chunks = new List<CodeChunk>();
        var symbols = new List<CodeSymbol>();
        var dependencies = new List<CodeDependency>();

        // Extract namespace
        var namespaceDecls = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().ToList();
        string namespaceName = namespaceDecls.FirstOrDefault()?.Name.ToString() ?? "";

        // Process type declarations (classes, interfaces, records)
        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var typeName = type.Identifier.Text;
            var (startLine, endLine) = GetLineRange(text, type.Span);

            var symbolType = type switch
            {
                ClassDeclarationSyntax => SymbolType.Class,
                InterfaceDeclarationSyntax => SymbolType.Interface,
                RecordDeclarationSyntax => SymbolType.Record,
                _ => SymbolType.Class
            };

            symbols.Add(new CodeSymbol
            {
                Id = Guid.NewGuid(),
                RepositoryDocumentId = repositoryDocumentId,
                RepositoryName = repositoryName,
                BranchName = branchName,
                FilePath = filePath,
                SymbolName = typeName,
                SymbolType = symbolType,
                Namespace = namespaceName,
                StartLine = startLine,
                EndLine = endLine
            });

            // Chunk the type declaration
            var typeText = type.ToString();
            chunks.Add(MakeChunk(typeText, filePath, "csharp", repositoryName, branchName, repositoryDocumentId,
                startLine, endLine, symbolType == SymbolType.Interface ? ChunkType.Interface : ChunkType.Class, typeName));

            // Base types / interfaces → dependencies
            if (type.BaseList != null)
            {
                foreach (var baseType in type.BaseList.Types)
                {
                    var targetSymbol = baseType.Type.ToString();
                    dependencies.Add(new CodeDependency
                    {
                        Id = Guid.NewGuid(),
                        RepositoryName = repositoryName,
                        BranchName = branchName,
                        SourceFilePath = filePath,
                        SourceSymbol = typeName,
                        TargetFilePath = "",
                        TargetSymbol = targetSymbol,
                        DependencyType = DependencyType.Inheritance
                    });
                }
            }

            // Methods and constructors
            foreach (var method in type.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var (ms, me) = GetLineRange(text, method.Span);
                var methodName = method.Identifier.Text;
                symbols.Add(new CodeSymbol
                {
                    Id = Guid.NewGuid(),
                    RepositoryDocumentId = repositoryDocumentId,
                    RepositoryName = repositoryName,
                    BranchName = branchName,
                    FilePath = filePath,
                    SymbolName = $"{typeName}.{methodName}",
                    SymbolType = SymbolType.Method,
                    Namespace = namespaceName,
                    StartLine = ms,
                    EndLine = me
                });
                chunks.Add(MakeChunk(method.ToString(), filePath, "csharp", repositoryName, branchName, repositoryDocumentId,
                    ms, me, ChunkType.Method, $"{typeName}.{methodName}"));

                // Record method invocations
                foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var targetSymbol = invocation.Expression.ToString();
                    if (targetSymbol.Length > 0)
                    {
                        dependencies.Add(new CodeDependency
                        {
                            Id = Guid.NewGuid(),
                            RepositoryName = repositoryName,
                            BranchName = branchName,
                            SourceFilePath = filePath,
                            SourceSymbol = $"{typeName}.{methodName}",
                            TargetFilePath = "",
                            TargetSymbol = targetSymbol,
                            DependencyType = DependencyType.MethodInvocation
                        });
                    }
                }
            }

            foreach (var ctor in type.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                var (cs, ce) = GetLineRange(text, ctor.Span);
                symbols.Add(new CodeSymbol
                {
                    Id = Guid.NewGuid(),
                    RepositoryDocumentId = repositoryDocumentId,
                    RepositoryName = repositoryName,
                    BranchName = branchName,
                    FilePath = filePath,
                    SymbolName = $"{typeName}..ctor",
                    SymbolType = SymbolType.Constructor,
                    Namespace = namespaceName,
                    StartLine = cs,
                    EndLine = ce
                });
                chunks.Add(MakeChunk(ctor.ToString(), filePath, "csharp", repositoryName, branchName, repositoryDocumentId,
                    cs, ce, ChunkType.Constructor, $"{typeName}..ctor"));
            }
        }

        // If no typed chunks produced, fall back to text chunking for the whole file
        if (chunks.Count == 0)
        {
            var textChunks = _textChunker.ChunkText(content, filePath, "csharp", repositoryName, branchName, repositoryDocumentId);
            chunks.AddRange(textChunks);
        }

        var result = new ParsedFile
        {
            FilePath = filePath,
            Language = "csharp",
            Chunks = chunks,
            Symbols = symbols,
            Dependencies = dependencies
        };

        return Task.FromResult(result);
    }

    private static (int start, int end) GetLineRange(SourceText text, TextSpan span)
    {
        var startLine = text.Lines.GetLineFromPosition(span.Start).LineNumber + 1;
        var endLine = text.Lines.GetLineFromPosition(span.End).LineNumber + 1;
        return (startLine, endLine);
    }

    private static CodeChunk MakeChunk(string chunkText, string filePath, string language,
        string repositoryName, string branchName, Guid repositoryDocumentId,
        int startLine, int endLine, ChunkType chunkType, string symbolName)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chunkText)));
        var id = Guid.NewGuid();
        return new CodeChunk
        {
            Id = id,
            RepositoryDocumentId = repositoryDocumentId,
            RepositoryName = repositoryName,
            BranchName = branchName,
            FilePath = filePath,
            Language = language,
            ChunkType = chunkType,
            SymbolName = symbolName,
            StartLine = startLine,
            EndLine = endLine,
            Text = chunkText,
            ContentHash = hash,
            EmbeddingId = id.ToString()
        };
    }
}
