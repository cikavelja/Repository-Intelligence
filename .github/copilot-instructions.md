# Copilot Instructions — Repository Intelligence PoC

This repository contains a .NET proof-of-concept named **RepositoryIntelligence**.

The goal is to demonstrate how to index large software repositories, detect file changes, perform semantic-style code search, and recommend which files are relevant for a given issue before asking an AI coding agent to make changes.

---

## Core Goal

Build a local-first system that can:

- Scan a source-code repository
- Ignore irrelevant folders and files
- Detect added, updated, deleted, and unchanged files
- Parse source files into searchable chunks
- Extract C# metadata with Roslyn
- Generate deterministic local mock embeddings
- Store metadata in SQLite
- Search code using hybrid search
- Recommend files relevant to a software issue
- Explain why each file is relevant

The project must run locally without API keys or cloud services.

---

## Technology

Use:

- .NET 8 or .NET 9
- C#
- Clean Architecture
- SQLite
- Roslyn
- Dependency Injection
- xUnit or NUnit
- Local deterministic mock embeddings

Do not require:

- OpenAI API keys
- Azure subscription
- Docker
- Qdrant
- Paid services

---

## Solution Structure

Create:

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

---

## Architecture Rules

Follow Clean Architecture:

- Domain must not depend on Application, Infrastructure, or ConsoleApp.
- Application may depend on Domain.
- Infrastructure may depend on Application and Domain.
- ConsoleApp may depend on Application, Domain, and Infrastructure.
- Keep storage, embeddings, vector search, parsing, and file scanning replaceable.
- Do not put all logic in `Program.cs`.
- Do not create one huge class.

---

## Required Domain Models

Create models:

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

Create enums:

- `IndexingMode`
- `ChunkType`
- `SymbolType`
- `DependencyType`

---

## Required Application Interfaces

Create interfaces:

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

---

## Infrastructure Implementations

Implement:

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

## Supported Files

Index:

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

Ignore binary files, generated files, minified files, and very large files.

---

## Indexing Requirements

Support two modes:

```text
full
incremental
```

### Full Indexing

- Clear existing index for selected repository and branch
- Re-index all supported files

### Incremental Indexing

Detect:

- Added files
- Updated files
- Deleted files
- Unchanged files

Use SHA256 content hash to detect updates.

Rules:

- Added files must be indexed.
- Updated files must have old chunks, symbols, dependencies, and vectors removed before re-indexing.
- Deleted files must be removed or marked as deleted.
- Deleted files must never appear in search results or issue recommendations.
- Unchanged files must not be re-indexed.

---

## C# Parsing

Use Roslyn for `.cs` files.

Extract:

- namespaces
- classes
- records
- interfaces
- methods
- constructors
- properties
- base classes
- implemented interfaces
- method invocations where practical

For non-C# files, use text chunking with line ranges.

---

## Search Requirements

Use hybrid search:

- semantic-style vector similarity
- keyword overlap
- symbol name matching
- file path relevance
- chunk type importance

Pure vector search is not enough for code.

The mock embedding service must be deterministic and local.

---

## Issue Analyzer

Given an issue description, the analyzer should:

1. Normalize the issue text
2. Extract important terms
3. Search indexed chunks
4. Group results by file
5. Score files
6. Recommend relevant files
7. Explain why each file is relevant
8. List related symbols
9. Suggest what to check or change
10. Suggest related tests if found

Example issue:

```text
Payment is not triggered after order creation.
```

Expected recommended files:

- `CreateOrderCommandHandler.cs`
- `OrderCreatedEvent.cs`
- `ServiceBusPublisher.cs`
- `OrderCreatedEventListenerService.cs`
- related tests

---

## Console Commands

Support:

```bash
dotnet run -- index --path "./samples/SampleECommerce" --name "SampleECommerce" --branch "main" --mode full

dotnet run -- index --path "./samples/SampleECommerce" --name "SampleECommerce" --branch "main" --mode incremental

dotnet run -- analyze --name "SampleECommerce" --branch "main" --issue "Payment is not triggered after order creation"

dotnet run -- search --name "SampleECommerce" --branch "main" --query "order created event service bus payment listener"

dotnet run -- summary --name "SampleECommerce" --branch "main"
```

Index output should include:

- Added files
- Updated files
- Deleted files
- Unchanged files
- Added chunks
- Deleted chunks
- Duration

---

## Sample Repository

Create:

```text
samples/SampleECommerce/
```

Include fake but realistic files for:

- Orders API
- Orders Application
- Orders Infrastructure
- Payments API
- Shared Events
- Tests
- Some unrelated files to prove filtering works

The sample issue must be:

```text
Payment is not triggered after order creation.
```

---

## Tests

Add tests for:

- ignored folders
- supported file scanning
- chunking
- Roslyn class/method extraction
- deterministic embeddings
- vector similarity ranking
- full indexing
- incremental indexing
- added files
- updated files
- deleted files
- unchanged files
- deleted files excluded from search
- issue analyzer recommends expected files

---

## README

Create `README.md` explaining:

- problem
- solution
- architecture
- indexing flow
- incremental indexing
- semantic-style search
- issue analysis
- how to run
- how to test
- example output
- how this helps Copilot Agent on large repositories
- future improvements

Future improvements:

- OpenAI embeddings
- Azure AI Search
- Qdrant
- pgvector
- Git history ranking
- dependency graph
- test impact analysis
- Copilot prompt generation
- patch generation
- GitHub Actions integration

---

## Implementation Rules

- Implement working code, not only skeletons.
- Keep it simple but functional.
- Use dependency injection.
- Use async I/O.
- Use cancellation tokens.
- Do not require secrets or cloud services.
- Do not modify files outside this project.
- Do not automatically change indexed repository files.
- The PoC should only read, index, search, and recommend.

---

## Validation

Run:

```bash
dotnet build
dotnet test
```

Fix all build and test errors before finishing.
