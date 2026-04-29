using RepositoryIntelligence.Infrastructure.FileSystem;
using Xunit;

namespace RepositoryIntelligence.Tests;

public sealed class LocalFileScannerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public LocalFileScannerTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task ScanAsync_ReturnsOnlySupportedFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "MyClass.cs"), "// code");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# title");
        File.WriteAllText(Path.Combine(_tempDir, "image.png"), "binary");

        var filter = new GitIgnoreStylePathFilter();
        var scanner = new LocalFileScanner(filter);

        var results = new List<string>();
        await foreach (var f in scanner.ScanAsync(_tempDir))
            results.Add(f);

        Assert.Contains(results, r => r.EndsWith("MyClass.cs"));
        Assert.Contains(results, r => r.EndsWith("README.md"));
        Assert.DoesNotContain(results, r => r.EndsWith("image.png"));
    }

    [Fact]
    public async Task ScanAsync_IgnoresIgnoredDirectories()
    {
        var binDir = Path.Combine(_tempDir, "bin");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "Assembly.dll"), "binary");
        File.WriteAllText(Path.Combine(binDir, "GeneratedFile.cs"), "// generated");

        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "Program.cs"), "// code");

        var filter = new GitIgnoreStylePathFilter();
        var scanner = new LocalFileScanner(filter);

        var results = new List<string>();
        await foreach (var f in scanner.ScanAsync(_tempDir))
            results.Add(f);

        Assert.Contains(results, r => r.EndsWith("Program.cs") && r.Contains("src"));
        Assert.DoesNotContain(results, r => r.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
    }

    [Fact]
    public async Task ScanAsync_IgnoresMinifiedFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "app.min.js"), "minified");
        File.WriteAllText(Path.Combine(_tempDir, "Component.tsx"), "// react");

        var filter = new GitIgnoreStylePathFilter();
        var scanner = new LocalFileScanner(filter);

        var results = new List<string>();
        await foreach (var f in scanner.ScanAsync(_tempDir))
            results.Add(f);

        Assert.DoesNotContain(results, r => r.EndsWith("app.min.js"));
        Assert.Contains(results, r => r.EndsWith("Component.tsx"));
    }
}
