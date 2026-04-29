using RepositoryIntelligence.Application.Interfaces;

namespace RepositoryIntelligence.Infrastructure.FileSystem;

public sealed class GitIgnoreStylePathFilter : IPathFilter
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", "dist", "build",
        ".git", ".vs", ".idea", "packages", "coverage", "TestResults"
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".json", ".md", ".ts", ".tsx", ".js", ".jsx",
        ".sql", ".yml", ".yaml"
    };

    private const long MaxFileSizeBytes = 1_000_000; // 1 MB

    public bool ShouldIgnoreDirectory(string directoryName) =>
        IgnoredDirectories.Contains(directoryName);

    public bool ShouldIgnoreFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        // Lock files
        if (fileName.EndsWith(".lock", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("package-lock.json", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("yarn.lock", StringComparison.OrdinalIgnoreCase))
            return true;

        // Minified JS
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (nameWithoutExt.EndsWith(".min", StringComparison.OrdinalIgnoreCase))
            return true;

        // Very large files
        try
        {
            var info = new FileInfo(filePath);
            if (info.Exists && info.Length > MaxFileSizeBytes)
                return true;
        }
        catch
        {
            // ignore stat errors
        }

        return false;
    }

    public bool IsSupportedFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return SupportedExtensions.Contains(ext);
    }
}
