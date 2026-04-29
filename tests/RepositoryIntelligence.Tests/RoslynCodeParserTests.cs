using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Infrastructure.Parsing;
using Xunit;

namespace RepositoryIntelligence.Tests;

public sealed class RoslynCodeParserTests
{
    private readonly RoslynCodeParser _parser;

    public RoslynCodeParserTests()
    {
        var chunker = new TextChunkingService();
        _parser = new RoslynCodeParser(chunker);
    }

    [Fact]
    public void CanParse_ReturnsTrue_ForCsFiles()
    {
        Assert.True(_parser.CanParse("MyClass.cs"));
        Assert.True(_parser.CanParse("/src/Foo/Bar.cs"));
    }

    [Fact]
    public void CanParse_ReturnsFalse_ForOtherFiles()
    {
        Assert.False(_parser.CanParse("README.md"));
        Assert.False(_parser.CanParse("app.ts"));
    }

    [Fact]
    public async Task ParseAsync_ExtractsClassSymbol()
    {
        var code = """
            namespace MyApp;

            public class OrderService
            {
                public void Process() { }
            }
            """;

        var result = await _parser.ParseAsync("OrderService.cs", code, "repo", "main", Guid.NewGuid());

        Assert.Contains(result.Symbols, s => s.SymbolName == "OrderService" && s.SymbolType == SymbolType.Class);
    }

    [Fact]
    public async Task ParseAsync_ExtractsInterfaceSymbol()
    {
        var code = """
            namespace MyApp;
            public interface IOrderRepository { }
            """;

        var result = await _parser.ParseAsync("IOrderRepository.cs", code, "repo", "main", Guid.NewGuid());

        Assert.Contains(result.Symbols, s => s.SymbolName == "IOrderRepository" && s.SymbolType == SymbolType.Interface);
    }

    [Fact]
    public async Task ParseAsync_ExtractsMethodSymbol()
    {
        var code = """
            namespace MyApp;
            public class PaymentService
            {
                public void ProcessPayment() { }
                public Task SendConfirmationAsync() => Task.CompletedTask;
            }
            """;

        var result = await _parser.ParseAsync("PaymentService.cs", code, "repo", "main", Guid.NewGuid());

        Assert.Contains(result.Symbols, s => s.SymbolName.Contains("ProcessPayment") && s.SymbolType == SymbolType.Method);
    }

    [Fact]
    public async Task ParseAsync_ExtractsRecordSymbol()
    {
        var code = """
            namespace MyApp;
            public record OrderCreatedEvent(Guid OrderId, decimal Amount);
            """;

        var result = await _parser.ParseAsync("OrderCreatedEvent.cs", code, "repo", "main", Guid.NewGuid());

        Assert.Contains(result.Symbols, s => s.SymbolName == "OrderCreatedEvent" && s.SymbolType == SymbolType.Record);
    }

    [Fact]
    public async Task ParseAsync_ProducesChunks()
    {
        var code = """
            namespace MyApp;
            public class Foo
            {
                public void Bar() { }
            }
            """;

        var result = await _parser.ParseAsync("Foo.cs", code, "repo", "main", Guid.NewGuid());

        Assert.NotEmpty(result.Chunks);
    }

    [Fact]
    public async Task ParseAsync_ExtractsDependencies_FromBaseList()
    {
        var code = """
            namespace MyApp;
            public class ConcreteService : IService { }
            public interface IService { }
            """;

        var result = await _parser.ParseAsync("ConcreteService.cs", code, "repo", "main", Guid.NewGuid());

        Assert.Contains(result.Dependencies, d => d.TargetSymbol.Contains("IService"));
    }
}
