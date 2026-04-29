# Copilot Agent Instructions — Repository Intelligence Index PoC

Use this file as the main instruction source for GitHub Copilot Agent Mode.

The goal is to generate a complete proof-of-concept project that demonstrates how to index a large software repository, detect file changes, perform semantic-style code search, and recommend which files should be changed for a given issue.

---

## 1. Project Goal

Create a complete proof-of-concept project named **RepositoryIntelligence**.

The purpose of this project is to demonstrate a **Repository Intelligence Index** for large software projects.

### Problem

AI coding agents can struggle on large repositories because they cannot reliably understand the entire codebase at once. They may:

- Modify the wrong files
- Miss architectural boundaries
- Ignore existing conventions
- Solve only symptoms
- Make unrelated changes
- Lose context in large solutions

### Solution

Build a local indexing and analysis system that:

1. Scans a repository
2. Indexes files and code chunks
3. Stores metadata
4. Performs semantic-style search
5. Detects added, updated, deleted, and unchanged files
6. Recommends which files are most relevant for a given issue
7. Explains why each file is relevant
8. Produces focused context that can be used by GitHub Copilot Agent

The project should show how developers can give AI agents a focused working set of relevant files before asking the agent to make code changes.

---

## 2. Technology Stack

Use:

- .NET 8 or .NET 9
- C#
- Clean Architecture
- SQLite
- Entity Framework Core or Dapper
- Roslyn for C# parsing
- xUnit or NUnit for tests
- Dependency Injection
- Local deterministic mock embedding service
- In-memory or SQLite-backed vector search for the first PoC

Do not require:

- OpenAI API keys
- Azure subscription
- Qdrant installation
- Docker
- Paid cloud services

The system must be designed so that the following can be added later:

- OpenAI embeddings
- Azure AI Search
- Qdrant
- pgvector
- Pinecone

---

## 3. Expected Solution Structure

Create this structure:

```text
RepositoryIntelligence.sln

src/
  RepositoryIntelligence.Domain/
  RepositoryIntelligence.Application/
  RepositoryIntelligence.Infrastructure/
  RepositoryIntelligence.ConsoleApp/

tests/
  RepositoryIntelligence.Tests/

samples/
  SampleECommerce/
```

Use Clean Architecture.

### Layering Rules

- `RepositoryIntelligence.Domain` must not depend on Application, Infrastructure, or ConsoleApp.
- `RepositoryIntelligence.Application` may depend on Domain.
- `RepositoryIntelligence.Infrastructure` may depend on Application and Domain.
- `RepositoryIntelligence.ConsoleApp` may depend on Application, Domain, and Infrastructure.
- Tests may depend on all layers.
- Do not put infrastructure implementation details in Application.
- Do not put business workflows in Infrastructure.
- Use interfaces in Application and implementations in Infrastructure.

---

## 4. Domain Layer Requirements

The Domain project should contain core models and enums.

### Required Domain Models

Create these models:

- `RepositoryDocument`
- `CodeChunk`
- `CodeSymbol`
- `CodeDependency`
- `SearchResult`
- `FileChangeRecommendation`
- `IssueAnalysisResult`
- `IndexingSummary`
- `IndexRepositoryCommand`
- `AnalyzeIssueCommand`
- `SearchCodeCommand`

### Required Enums

Create these enums:

- `IndexingMode`
- `ChunkType`
- `SymbolType`
- `DependencyType`

### RepositoryDocument

Should include:

```csharp
public sealed class RepositoryDocument
{
    public Guid Id { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string FilePath { get; set; } = "";
    public string AbsolutePath { get; set; } = "";
    public string Language { get; set; } = "";

    public string ContentHash { get; set; } = "";
    public long FileSizeBytes { get; set; }

    public DateTime LastModifiedUtc { get; set; }
    public DateTime IndexedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
```

### CodeChunk

Should include:

```csharp
public sealed class CodeChunk
{
    public Guid Id { get; set; }
    public Guid RepositoryDocumentId { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string FilePath { get; set; } = "";
    public string Language { get; set; } = "";

    public ChunkType ChunkType { get; set; }
    public string SymbolName { get; set; } = "";

    public int StartLine { get; set; }
    public int EndLine { get; set; }

    public string Text { get; set; } = "";
    public string ContentHash { get; set; } = "";

    public string EmbeddingId { get; set; } = "";
}
```

### CodeSymbol

Should include:

```csharp
public sealed class CodeSymbol
{
    public Guid Id { get; set; }
    public Guid RepositoryDocumentId { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string FilePath { get; set; } = "";
    public string SymbolName { get; set; } = "";

    public SymbolType SymbolType { get; set; }

    public string Namespace { get; set; } = "";

    public int StartLine { get; set; }
    public int EndLine { get; set; }
}
```

### CodeDependency

Should include:

```csharp
public sealed class CodeDependency
{
    public Guid Id { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string SourceFilePath { get; set; } = "";
    public string SourceSymbol { get; set; } = "";

    public string TargetFilePath { get; set; } = "";
    public string TargetSymbol { get; set; } = "";

    public DependencyType DependencyType { get; set; }
}
```

### IndexingSummary

Should include:

```csharp
public sealed class IndexingSummary
{
    public int AddedFiles { get; set; }
    public int UpdatedFiles { get; set; }
    public int DeletedFiles { get; set; }
    public int UnchangedFiles { get; set; }

    public int AddedChunks { get; set; }
    public int DeletedChunks { get; set; }

    public TimeSpan Duration { get; set; }
}
```

### IndexRepositoryCommand

Should include:

```csharp
public sealed class IndexRepositoryCommand
{
    public string RepositoryPath { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "main";
    public IndexingMode Mode { get; set; } = IndexingMode.Incremental;
}
```

---

## 5. Application Layer Requirements

The Application project should contain interfaces and use cases.

### Required Interfaces

Create these interfaces:

- `IRepositoryIndexer`
- `IFileScanner`
- `IPathFilter`
- `ICodeParser`
- `IChunkingService`
- `IEmbeddingService`
- `IVectorSearchService`
- `IMetadataStore`
- `IIssueAnalyzer`
- `ISearchService`

### IRepositoryIndexer

```csharp
public interface IRepositoryIndexer
{
    Task<IndexingSummary> IndexRepositoryAsync(
        IndexRepositoryCommand command,
        CancellationToken cancellationToken = default);
}
```

### IFileScanner

Scans supported files from a repository path.

### IPathFilter

Decides whether a file or directory should be ignored.

### ICodeParser

Parses source files and extracts:

- Chunks
- Symbols
- Dependencies

### IChunkingService

Splits text files into searchable chunks.

### IEmbeddingService

Generates deterministic local embeddings.

### IVectorSearchService

Stores, deletes, and searches vectors.

### IMetadataStore

Stores:

- Repository documents
- Chunks
- Symbols
- Dependencies
- Indexing state

### IIssueAnalyzer

Analyzes an issue and recommends files.

### ISearchService

Performs code search.

---

## 6. Infrastructure Layer Requirements

The Infrastructure project should contain implementations.

### Required Implementations

Create:

- `LocalFileScanner`
- `GitIgnoreStylePathFilter`
- `RoslynCodeParser`
- `TextChunkingService`
- `LocalHashEmbeddingService`
- `InMemoryVectorSearchService` or `SQLiteVectorSearchService`
- `SqliteMetadataStore`
- `RepositoryIndexer`
- `SearchService`
- `IssueAnalyzer`

---

## 7. File Scanning Rules

Supported file types:

- `.cs`
- `.csproj`
- `.json`
- `.md`
- `.ts`
- `.tsx`
- `.js`
- `.jsx`
- `.sql`
- `.yml`
- `.yaml`

Ignore folders:

- `bin`
- `obj`
- `node_modules`
- `dist`
- `build`
- `.git`
- `.vs`
- `.idea`
- `packages`
- `coverage`
- `TestResults`

Ignore files:

- Binary files
- Very large files above a configurable size limit
- Minified JavaScript files
- Generated files where obvious
- Lock files unless explicitly needed

---

## 8. Incremental Indexing Requirements

The indexer must support both full and incremental indexing.

### Full Indexing

Full mode should:

- Clear existing index data for the selected repository and branch
- Re-index all supported files from scratch

### Incremental Indexing

Incremental mode should:

- Scan current repository state
- Load existing indexed files for selected repository and branch
- Compare current files with indexed files by normalized file path
- Detect added files
- Detect updated files using SHA256 hash
- Detect deleted files
- Leave unchanged files untouched

### Added Files

When a new supported file appears:

- Create a `RepositoryDocument` record
- Parse content
- Create `CodeChunk` records
- Extract `CodeSymbol` records
- Extract `CodeDependency` records where possible
- Generate embeddings
- Add vector records
- Include the file in search results

### Updated Files

When a file exists but its content hash changed:

- Delete old chunks for that file
- Delete old vector records for that file
- Delete old symbols for that file
- Delete old dependencies for that file
- Parse updated content
- Create new chunks
- Generate new embeddings
- Update `RepositoryDocument` metadata
- Include the updated file in search results

### Deleted Files

When a file exists in the index but no longer exists on disk:

- Mark it as deleted or remove it completely
- Delete related chunks
- Delete related vectors
- Delete related symbols
- Delete related dependencies
- Ensure deleted files never appear in search results
- Ensure deleted files never appear in issue recommendations

### Unchanged Files

When a file exists and the SHA256 hash is unchanged:

- Do not re-parse
- Do not regenerate embeddings
- Do not modify existing chunks
- Count the file in the indexing summary

### File Change Detection Formula

```text
Current disk files - indexed files = added files

Indexed files - current disk files = deleted files

Files existing in both:
    if current hash != indexed hash:
        updated files
    else:
        unchanged files
```

---

## 9. C# Parsing with Roslyn

For `.cs` files, use Roslyn to extract:

- Namespace
- Classes
- Records
- Interfaces
- Methods
- Constructors
- Properties
- Base classes
- Implemented interfaces
- Method invocations where practical

Create chunks for:

- Whole file summary if useful
- Class declarations
- Interface declarations
- Method declarations
- Constructor declarations
- Important configuration files

For non-C# files:

- Use `TextChunkingService`
- Chunk by size and line ranges
- Preserve file path and language metadata

---

## 10. Embedding Service Requirements

Implement a deterministic mock embedding service.

The mock embedding service should:

- Accept text
- Normalize tokens
- Generate a fixed-size float vector, for example 128 dimensions
- Be deterministic for the same input
- Be good enough for demo ranking
- Not call external APIs

Example behavior:

```text
Input:
"Payment is not triggered after order creation"

Output:
float[128]
```

The same input must always return the same vector.

---

## 11. Vector Search Requirements

The vector search service should:

- Store vectors by chunk ID
- Delete vectors by file path
- Delete vectors by repository document ID
- Search using cosine similarity
- Return top matching chunks
- Exclude deleted files
- Combine vector score with keyword overlap where practical

Search should combine:

- Semantic-style similarity
- Keyword overlap
- Symbol name matching
- File path relevance
- Chunk type importance
- Dependency proximity where available

Pure vector search is not enough for source code. Use hybrid scoring.

---

## 12. Issue Analyzer Requirements

Given an issue description, the analyzer should:

1. Normalize issue text
2. Extract important terms
3. Perform search
4. Group matching chunks by file
5. Score each file
6. Recommend the most relevant files
7. Explain why each file is relevant
8. List related symbols
9. Suggest checks or possible change areas
10. Suggest related tests if found

### File Scoring Factors

Use:

- Semantic similarity
- Keyword overlap
- Symbol name matches
- File path matches
- Chunk type importance
- Dependency proximity if available

### Example Issue

```text
Payment is not triggered after order creation.
```

Expected recommended files should include:

- `CreateOrderCommandHandler.cs`
- `OrderCreatedEvent.cs`
- `ServiceBusPublisher.cs`
- `OrderCreatedEventListenerService.cs`
- Related tests if available

### Example Output

```text
Issue:
Payment is not triggered after order creation.

Recommended files:

1. Orders.Application/Commands/CreateOrder/CreateOrderCommandHandler.cs
   Confidence: 0.91
   Reason:
   This handler appears to create and save orders. It contains symbols related to order creation and may be responsible for publishing the OrderCreatedEvent.

   Related symbols:
   - CreateOrderCommandHandler
   - Handle
   - OrderCreatedEvent

   Suggested check:
   Verify that OrderCreatedEvent is published after the order is saved.

2. Orders.Infrastructure/Messaging/ServiceBusPublisher.cs
   Confidence: 0.84
   Reason:
   This file appears responsible for publishing integration events to the message broker.

   Suggested check:
   Verify queue/topic name, serialization contract, and error handling.

3. Payments.API/Consumers/OrderCreatedEventListenerService.cs
   Confidence: 0.79
   Reason:
   This service appears to consume OrderCreatedEvent and create payment records.

   Suggested check:
   Verify listener registration, queue/topic subscription, and error handling.
```

---

## 13. Console App Requirements

Implement a command-line interface.

### Required Commands

#### Index Repository

```bash
dotnet run -- index --path "./samples/SampleECommerce" --name "SampleECommerce" --branch "main" --mode full
```

```bash
dotnet run -- index --path "./samples/SampleECommerce" --name "SampleECommerce" --branch "main" --mode incremental
```

#### Analyze Issue

```bash
dotnet run -- analyze --name "SampleECommerce" --branch "main" --issue "Payment is not triggered after order creation"
```

#### Search Code

```bash
dotnet run -- search --name "SampleECommerce" --branch "main" --query "order created event service bus payment listener"
```

#### Print Summary

```bash
dotnet run -- summary --name "SampleECommerce" --branch "main"
```

### Index Command Output

The index command should print:

```text
Repository indexed successfully.

Added files: X
Updated files: X
Deleted files: X
Unchanged files: X
Added chunks: X
Deleted chunks: X
Duration: 00:00:00.000
```

---

## 14. Sample Repository Requirements

Create a sample repository under:

```text
samples/SampleECommerce/
```

It should contain a small fake e-commerce solution with:

- Orders API
- Orders Application
- Orders Infrastructure
- Payments API
- Shared Events
- Tests

Sample files should include:

- `Orders.Application/Commands/CreateOrder/CreateOrderCommandHandler.cs`
- `Orders.Domain/Entities/Order.cs`
- `Orders.Infrastructure/Messaging/ServiceBusPublisher.cs`
- `Shared/Events/OrderCreatedEvent.cs`
- `Payments.API/Consumers/OrderCreatedEventListenerService.cs`
- `Payments.Infrastructure/PaymentRepository.cs`
- `Tests/CreateOrderCommandHandlerTests.cs`

Also include unrelated files to prove that search filters noise:

- `ProductController.cs`
- `CustomerProfileService.cs`
- `InventoryReportGenerator.cs`

The sample repository should be realistic enough to demonstrate that the analyzer can filter irrelevant files.

The sample issue should be:

```text
Payment is not triggered after order creation.
```

Expected analysis should recommend:

- Order creation handler
- Event publishing service
- Shared event contract
- Payment event listener
- Related tests

---

## 15. Unit Test Requirements

Create tests for:

- `LocalFileScanner` ignores `bin`, `obj`, `node_modules`, `dist`, `.git`
- `LocalFileScanner` returns supported files
- `GitIgnoreStylePathFilter` rejects ignored folders
- `TextChunkingService` creates chunks with correct line ranges
- `RoslynCodeParser` extracts classes
- `RoslynCodeParser` extracts methods
- `LocalHashEmbeddingService` returns deterministic vectors
- Cosine similarity ranks similar text higher
- `SearchService` returns relevant chunks
- `RepositoryIndexer` indexes new files
- `RepositoryIndexer` updates changed files
- `RepositoryIndexer` removes deleted files
- Deleted files do not appear in search results
- Unchanged files are not re-indexed
- Full indexing clears and rebuilds repository index
- Incremental indexing preserves unchanged records
- `IssueAnalyzer` recommends expected files for the sample issue

---

## 16. README Requirements

Create a detailed `README.md` with:

- Project title
- Problem statement
- Solution overview
- Architecture diagram in text
- Solution structure
- How indexing works
- How incremental indexing works
- How semantic-style search works
- How issue analysis works
- How to run the sample
- How to run tests
- Example output
- How this helps GitHub Copilot Agent on large repositories
- Future improvements

### Future Improvements

Mention:

- OpenAI embeddings
- Azure AI Search
- Qdrant
- pgvector
- Pinecone
- Git history ranking
- Full dependency graph
- Call graph analysis
- Test impact analysis
- Copilot prompt generation
- Patch generation
- Pull request creation
- GitHub Actions integration
- Support for Python, Java, and TypeScript AST parsing

---

## 17. Optional Feature: Copilot Prompt Generation

After the base project works, add a feature called **Copilot Prompt Generation**.

### Purpose

After analyzing an issue and recommending relevant files, generate a focused prompt that a developer can paste into GitHub Copilot Agent Mode.

### Add Interface

```csharp
public interface ICopilotPromptGenerator
{
    Task<string> GeneratePromptAsync(
        IssueAnalysisResult analysisResult,
        CancellationToken cancellationToken = default);
}
```

### Generated Prompt Should Include

- Issue description
- Recommended files
- Reasons why files are relevant
- Related symbols
- Suggested checks
- Constraints

### Example Generated Prompt

```text
Issue:
Payment is not triggered after order creation.

Relevant files:
1. Orders.Application/Commands/CreateOrder/CreateOrderCommandHandler.cs
2. Orders.Infrastructure/Messaging/ServiceBusPublisher.cs
3. Shared/Events/OrderCreatedEvent.cs
4. Payments.API/Consumers/OrderCreatedEventListenerService.cs

Task:
Investigate why payment is not triggered after order creation.

Constraints:
- Do not modify unrelated files.
- Respect Clean Architecture boundaries.
- Do not put business logic in controllers.
- Check whether OrderCreatedEvent is published after the order is saved.
- Check whether the payment listener consumes the correct event contract.
- Add or update tests where needed.
- Run dotnet build and dotnet test.
```

### Add Console Command

```bash
dotnet run -- generate-prompt --name "SampleECommerce" --branch "main" --issue "Payment is not triggered after order creation"
```

Add tests for prompt generation.

---

## 18. Required Skills for Copilot Agent

Apply these skills while implementing the project.

### 1. Software Architecture

- Apply Clean Architecture.
- Keep domain independent.
- Keep infrastructure replaceable.
- Separate indexing, parsing, storage, search, and analysis responsibilities.

### 2. Code Analysis

- Use Roslyn for C# source-code parsing.
- Extract useful symbols from C# files.
- Preserve file path, symbol name, and line number metadata.

### 3. Search Engineering

- Use hybrid search.
- Combine semantic-style vector score with keyword overlap.
- Group search results by file.
- Avoid returning deleted files.

### 4. Incremental Indexing

- Use SHA256 hashes to detect changed files.
- Detect added, updated, deleted, and unchanged files.
- Avoid re-indexing unchanged files.

### 5. Data Modeling

- Store repository documents, chunks, symbols, dependencies, and vectors clearly.
- Keep data model simple enough for a PoC but extensible.

### 6. Testing

- Write focused unit tests.
- Use small temporary test repositories.
- Test full and incremental indexing.
- Test deleted-file behavior.

### 7. Developer Experience

- Provide simple CLI commands.
- Print readable output.
- Include a useful README.
- Make the demo runnable locally without cloud dependencies.

### 8. AI-Agent Enablement

- Design the output so it can be used to create focused Copilot prompts.
- Recommended files should include reasons, symbols, and suggested checks.

---

## 19. Implementation Rules

- Implement real working code, not only skeletons.
- Keep the project simple but functional.
- Do not require external paid services.
- Do not require API keys.
- Use dependency injection.
- Use async I/O.
- Use cancellation tokens.
- Avoid unnecessary complexity.
- Do not create one huge class.
- Do not put all logic into `Program.cs`.
- Do not ignore tests.
- Do not skip README.
- Do not modify files outside this project.
- Do not automatically modify source code in the indexed repository.
- The PoC should only read repository files, index them, search them, and recommend files.

---

## 20. Recommended Implementation Order

Use this order to reduce mistakes.

```text
1. Generate solution and project structure.
2. Generate Domain models and enums.
3. Generate Application interfaces.
4. Generate file scanner and path filter.
5. Generate chunking service.
6. Generate mock embedding service.
7. Generate vector search service.
8. Generate SQLite metadata store.
9. Generate Roslyn parser.
10. Generate repository indexer.
11. Generate search service.
12. Generate issue analyzer.
13. Generate console commands.
14. Generate sample repository.
15. Generate tests.
16. Generate README.
17. Build and fix.
18. Test and fix.
```

---

## 21. First Prompt to Use with Copilot Agent

Start with this prompt:

```text
Create only the solution structure, project references, Domain models, Application interfaces, and README outline for the RepositoryIntelligence PoC.

Do not implement Infrastructure yet.

After creating this first step, run dotnet build and fix project/reference errors.
```

Then continue with:

```text
Now implement Infrastructure services for file scanning, path filtering, text chunking, deterministic mock embeddings, vector search, SQLite metadata storage, and incremental indexing.

Add unit tests for added, updated, deleted, and unchanged files.

Run dotnet build and dotnet test.
```

---

## 22. Full Copilot Agent Prompt

Paste this into GitHub Copilot Agent Mode when you want it to generate the full project:

```text
Act as a senior .NET software architect and trainer.

Create a complete proof-of-concept project named RepositoryIntelligence.

The purpose of this project is to demonstrate a Repository Intelligence Index for large software projects.

The problem:

AI coding agents can struggle on large repositories because they cannot reliably understand the entire codebase at once. They may modify the wrong files, miss architectural boundaries, ignore existing conventions, or solve only symptoms.

The solution:

Build a local indexing and analysis system that scans a repository, indexes files and code chunks, stores metadata, performs semantic-style search, and recommends which files are most likely relevant for a given issue.

The project should demonstrate how developers can give AI agents a focused working set of relevant files before asking the agent to make code changes.

Use .NET 8 or .NET 9 and C#.

Create this solution structure:

RepositoryIntelligence.sln

src/
  RepositoryIntelligence.Domain/
  RepositoryIntelligence.Application/
  RepositoryIntelligence.Infrastructure/
  RepositoryIntelligence.ConsoleApp/

tests/
  RepositoryIntelligence.Tests/

samples/
  SampleECommerce/

Use Clean Architecture.

The Domain project should contain core models and enums.

The Application project should contain interfaces and use cases.

The Infrastructure project should contain implementations for file scanning, path filtering, Roslyn parsing, chunking, deterministic embeddings, vector search, SQLite metadata storage, repository indexing, search, and issue analysis.

The ConsoleApp should provide commands for indexing, searching, analyzing issues, and printing summaries.

The indexer must support both full and incremental indexing.

Incremental indexing must detect added, updated, deleted, and unchanged files.

Deleted files must not appear in search results or issue recommendations.

Use SHA256 content hashes to detect file updates.

Use Roslyn to parse C# files.

Use text chunking for non-C# files.

Use a deterministic local mock embedding service so the PoC works without API keys.

Use hybrid search that combines semantic-style vector similarity with keyword overlap.

Create a sample repository under samples/SampleECommerce.

The sample repository should demonstrate this issue:

Payment is not triggered after order creation.

The analyzer should recommend files related to order creation, event publishing, shared event contract, payment listener, and related tests.

Add unit tests for file scanning, chunking, Roslyn parsing, embedding, vector search, full indexing, incremental indexing, deleted files, search, and issue analysis.

Create a detailed README.

Implementation rules:

- Implement real working code, not only skeletons.
- Keep the project simple but functional.
- Do not require external paid services.
- Do not require API keys.
- Use dependency injection.
- Use async I/O.
- Use cancellation tokens.
- Avoid unnecessary complexity.
- Do not create one huge class.
- Do not put all logic into Program.cs.
- Do not skip tests.
- Do not skip README.
- Do not modify files outside this project.
- Do not write changes to the indexed repository.

Work incrementally:

Step 1:
Create solution and projects.

Step 2:
Create Domain models and enums.

Step 3:
Create Application interfaces and commands.

Step 4:
Implement Infrastructure services.

Step 5:
Implement SQLite metadata store.

Step 6:
Implement vector search and mock embeddings.

Step 7:
Implement repository indexer with full and incremental modes.

Step 8:
Implement search service.

Step 9:
Implement issue analyzer.

Step 10:
Create sample e-commerce repository.

Step 11:
Create console commands.

Step 12:
Create tests.

Step 13:
Create README.

Step 14:
Run dotnet build and dotnet test.

Step 15:
Fix all build and test errors.

After implementation, provide a summary of:

- what was created
- how to run it
- how to test it
- known limitations
- future improvements
```

---

## 23. Recovery Prompt if Copilot Gets Lost

Use this if Copilot Agent starts generating messy or unrelated code:

```text
Stop and review the current implementation.

Do not add new features.

Check the project against these rules:

1. Does the solution build?
2. Are all projects referenced correctly?
3. Are Domain, Application, Infrastructure, and ConsoleApp responsibilities separated?
4. Are there any infrastructure dependencies inside Domain or Application?
5. Does incremental indexing handle added, updated, deleted, and unchanged files?
6. Are deleted files excluded from search?
7. Are tests meaningful and passing?
8. Is README accurate?

Fix only architectural, build, and test issues.

Run:

dotnet build
dotnet test

Do not rewrite the whole project unless absolutely necessary.
```

---

## 24. Warning for Real Repositories

Do not scan or modify an actual production repository first.

First build the PoC using:

```text
samples/SampleECommerce
```

Only after the PoC works, test indexing on a copy of a real repository.

The PoC should only:

- Read repository files
- Index files
- Search files
- Recommend files

It should not automatically modify source code.

---

## 25. Validation Checklist

Before considering the project complete, verify:

- [ ] Solution builds successfully
- [ ] Tests pass successfully
- [ ] Full indexing works
- [ ] Incremental indexing works
- [ ] Added files are detected
- [ ] Updated files are detected
- [ ] Deleted files are detected
- [ ] Unchanged files are not re-indexed
- [ ] Deleted files do not appear in search results
- [ ] Search returns relevant files
- [ ] Issue analysis recommends expected files
- [ ] Sample e-commerce repository exists
- [ ] README explains how to run everything
- [ ] No API keys are required
- [ ] No cloud dependency is required
- [ ] Architecture follows Clean Architecture boundaries

---

## 26. Final Validation Commands

Run:

```bash
dotnet build
dotnet test
```

Fix all errors before finishing.
