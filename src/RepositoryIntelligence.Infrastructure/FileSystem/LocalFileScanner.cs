using RepositoryIntelligence.Application.Interfaces;

namespace RepositoryIntelligence.Infrastructure.FileSystem;

public sealed class LocalFileScanner : IFileScanner
{
    private readonly IPathFilter _pathFilter;

    public LocalFileScanner(IPathFilter pathFilter)
    {
        _pathFilter = pathFilter;
    }

    public async IAsyncEnumerable<string> ScanAsync(
        string rootPath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dir = stack.Pop();

            string[] subdirs;
            string[] files;
            try
            {
                subdirs = Directory.GetDirectories(dir);
                files = Directory.GetFiles(dir);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var subdir in subdirs)
            {
                var dirName = Path.GetFileName(subdir);
                if (!_pathFilter.ShouldIgnoreDirectory(dirName))
                    stack.Push(subdir);
            }

            foreach (var file in files)
            {
                if (_pathFilter.IsSupportedFile(file) && !_pathFilter.ShouldIgnoreFile(file))
                {
                    yield return file;
                    await Task.Yield();
                }
            }
        }
    }
}
