using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class DocumentService : IDocumentService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "image/jpeg",
        "image/png"
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMalwareScanner _malwareScanner;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IMalwareScanner malwareScanner,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _malwareScanner = malwareScanner;
        _logger = logger;
    }

    public async Task<DocumentUploadResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string title,
        string? description,
        string category,
        int? projectId,
        string? tags,
        int uploadedByUserId)
    {
        // FR-002: reject before any file is persisted, with a clear, specific message.
        if (string.IsNullOrWhiteSpace(title))
        {
            return DocumentUploadResult.Fail("Title is required.");
        }

        if (!DocumentCategories.All.Contains(category))
        {
            return DocumentUploadResult.Fail($"Category must be one of: {string.Join(", ", DocumentCategories.All)}.");
        }

        if (fileSizeBytes <= 0)
        {
            return DocumentUploadResult.Fail("The selected file is empty.");
        }

        if (fileSizeBytes > DocumentUploadLimits.MaxFileSizeBytes)
        {
            return DocumentUploadResult.Fail("The selected file exceeds the 25 MB size limit.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            return DocumentUploadResult.Fail($"Unsupported file type: {contentType}.");
        }

        // Scan happens against a temp file, independent of the storage backend,
        // before the file is handed to IFileStorageService (FR-006).
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}");
        try
        {
            using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(tempFileStream);
            }

            var scanResult = await _malwareScanner.ScanAsync(tempFilePath);
            if (scanResult != ScanResult.Clean)
            {
                _logger.LogWarning(
                    "Upload rejected for user {UserId}: malware scan result {ScanResult} for {FileName}.",
                    uploadedByUserId, scanResult, originalFileName);
                return DocumentUploadResult.Fail(
                    scanResult == ScanResult.Flagged
                        ? "The file was flagged by the malware scan and was not uploaded."
                        : "The file could not be scanned for malware and was not uploaded.");
            }

            // FR-007: GUID-based relative path, generated server-side, never derived from the user's filename.
            var extension = Path.GetExtension(originalFileName);
            var relativePath = Path.Combine(
                uploadedByUserId.ToString(),
                projectId?.ToString() ?? "personal",
                $"{Guid.NewGuid()}{extension}").Replace('\\', '/');

            using (var uploadStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                await _fileStorageService.UploadAsync(uploadStream, relativePath);
            }

            var document = new Document
            {
                Title = title,
                Description = description,
                Category = category,
                ProjectId = projectId,
                Tags = tags,
                UploadedByUserId = uploadedByUserId,
                UploadDate = DateTime.UtcNow,
                FileSizeBytes = fileSizeBytes,
                FileType = contentType,
                FilePath = relativePath,
                OriginalFileName = originalFileName
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            _context.DocumentActivityLogEntries.Add(new DocumentActivityLogEntry
            {
                DocumentId = document.DocumentId,
                DocumentTitleSnapshot = document.Title,
                ActorUserId = uploadedByUserId,
                ActionType = DocumentActionType.Upload,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return DocumentUploadResult.Ok(document);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    public async Task<List<Document>> GetMyDocumentsAsync(int userId)
    {
        return await _context.Documents
            .Include(d => d.Project)
            .Where(d => d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }
}
