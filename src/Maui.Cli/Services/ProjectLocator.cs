namespace Maui.Cli.Services;

internal sealed class ProjectLocator : IProjectLocator
{
    public Task<FileInfo?> FindProjectFileAsync(string directory, CancellationToken cancellationToken = default)
    {
        var dir = new DirectoryInfo(directory);
        
        if (!dir.Exists)
        {
            return Task.FromResult<FileInfo?>(null);
        }

        // Find .csproj files in the directory
        var projectFiles = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);

        if (projectFiles.Length == 0)
        {
            return Task.FromResult<FileInfo?>(null);
        }

        // If there's only one, return it
        if (projectFiles.Length == 1)
        {
            return Task.FromResult<FileInfo?>(projectFiles[0]);
        }

        // If there are multiple, prefer ones with "MAUI" or "Maui" in the name
        var mauiProject = projectFiles.FirstOrDefault(f => 
            f.Name.Contains("MAUI", StringComparison.OrdinalIgnoreCase) ||
            f.Name.Contains("Maui", StringComparison.OrdinalIgnoreCase));

        if (mauiProject != null)
        {
            return Task.FromResult<FileInfo?>(mauiProject);
        }

        // Otherwise return the first one
        return Task.FromResult<FileInfo?>(projectFiles[0]);
    }
}
