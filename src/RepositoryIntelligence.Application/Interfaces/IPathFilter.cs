namespace RepositoryIntelligence.Application.Interfaces;

public interface IPathFilter
{
    bool ShouldIgnoreDirectory(string directoryName);
    bool ShouldIgnoreFile(string filePath);
    bool IsSupportedFile(string filePath);
}
