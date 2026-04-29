using RepositoryIntelligence.Infrastructure.FileSystem;
using Xunit;

namespace RepositoryIntelligence.Tests;

public sealed class PathFilterTests
{
    private readonly GitIgnoreStylePathFilter _filter = new();

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("node_modules")]
    [InlineData("dist")]
    [InlineData("build")]
    [InlineData(".git")]
    [InlineData(".vs")]
    [InlineData(".idea")]
    [InlineData("packages")]
    [InlineData("coverage")]
    [InlineData("TestResults")]
    public void ShouldIgnoreDirectory_ReturnsTrue_ForIgnoredFolders(string dirName)
    {
        Assert.True(_filter.ShouldIgnoreDirectory(dirName));
    }

    [Theory]
    [InlineData("src")]
    [InlineData("tests")]
    [InlineData("Orders.Application")]
    [InlineData("Payments.API")]
    public void ShouldIgnoreDirectory_ReturnsFalse_ForAllowedFolders(string dirName)
    {
        Assert.False(_filter.ShouldIgnoreDirectory(dirName));
    }

    [Theory]
    [InlineData("MyClass.cs")]
    [InlineData("appsettings.json")]
    [InlineData("README.md")]
    [InlineData("schema.sql")]
    [InlineData("docker-compose.yml")]
    [InlineData("Component.tsx")]
    public void IsSupportedFile_ReturnsTrue_ForKnownExtensions(string fileName)
    {
        Assert.True(_filter.IsSupportedFile(fileName));
    }

    [Theory]
    [InlineData("image.png")]
    [InlineData("binary.exe")]
    [InlineData("data.zip")]
    public void IsSupportedFile_ReturnsFalse_ForUnsupportedExtensions(string fileName)
    {
        Assert.False(_filter.IsSupportedFile(fileName));
    }

    [Theory]
    [InlineData("bundle.min.js")]
    [InlineData("app.min.css")]
    public void ShouldIgnoreFile_ReturnsTrue_ForMinifiedFiles(string fileName)
    {
        Assert.True(_filter.ShouldIgnoreFile(fileName));
    }

    [Theory]
    [InlineData("package-lock.json")]
    [InlineData("yarn.lock")]
    public void ShouldIgnoreFile_ReturnsTrue_ForLockFiles(string fileName)
    {
        Assert.True(_filter.ShouldIgnoreFile(fileName));
    }
}
