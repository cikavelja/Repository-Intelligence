namespace RepositoryIntelligence.Application.Interfaces;

public interface IFileScanner
{
    /// <summary>
    /// Scans the given root directory and returns paths of all supported files
    /// that pass the path filter, relative to <paramref name="rootPath"/>.
    /// </summary>
    IAsyncEnumerable<string> ScanAsync(
        string rootPath,
        CancellationToken cancellationToken = default);
}
