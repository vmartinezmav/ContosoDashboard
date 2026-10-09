namespace ContosoDashboard.Services;

// Local/offline default. A production deployment can swap this for an
// AzureBlobStorageService without changing DocumentService, Pages, or the
// Document schema (FilePath works for both local paths and blob names).
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string relativePath);
    Task<Stream> DownloadAsync(string relativePath);
    Task DeleteAsync(string relativePath);
    bool Exists(string relativePath);
}
