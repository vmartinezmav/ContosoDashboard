namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredRoot = configuration["DocumentStorage:RootPath"] ?? "App_Data/uploads";
        _rootPath = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot);
    }

    public async Task<string> UploadAsync(Stream fileStream, string relativePath)
    {
        var fullPath = ResolveFullPath(relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        using var destination = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(destination);

        return relativePath;
    }

    public Task<Stream> DownloadAsync(string relativePath)
    {
        var fullPath = ResolveFullPath(relativePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath)
    {
        var fullPath = ResolveFullPath(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public bool Exists(string relativePath) => File.Exists(ResolveFullPath(relativePath));

    // Resolve and verify the path stays within the storage root, to prevent
    // path traversal even though FilePath is always server-generated (defense in depth).
    private string ResolveFullPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var normalizedRoot = Path.GetFullPath(_rootPath);
        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Resolved path is outside the document storage root.");
        }
        return fullPath;
    }
}
