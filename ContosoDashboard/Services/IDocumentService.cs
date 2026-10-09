using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

// FR-001/FR-002: shared so both DocumentService and upload-UI code validate consistently.
public static class DocumentUploadLimits
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024;
}

public class DocumentUploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Document? Document { get; set; }

    public static DocumentUploadResult Fail(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    public static DocumentUploadResult Ok(Document document) =>
        new() { Success = true, Document = document };
}

// Grows incrementally as later user stories (search/filter/sort, download,
// sharing, etc.) are implemented — only Upload + "My Documents" are needed
// for the current MVP (US1).
public interface IDocumentService
{
    Task<DocumentUploadResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string title,
        string? description,
        string category,
        int? projectId,
        string? tags,
        int uploadedByUserId);

    Task<List<Document>> GetMyDocumentsAsync(int userId);
}
