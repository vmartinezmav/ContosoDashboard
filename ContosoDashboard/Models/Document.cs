using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    // Stored as text, not an enum (stakeholder constraint). Allowed values are
    // enforced at the application layer: see DocumentCategories.All.
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public int? ProjectId { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    [Required]
    public int UploadedByUserId { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    public long FileSizeBytes { get; set; }

    // Max 255 to accommodate long Office Open XML MIME type strings.
    [Required]
    [MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    // GUID-based relative path; never derived from the user-supplied filename (FR-007).
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    public int? TaskId { get; set; }

    // Navigation properties
    [ForeignKey("UploadedByUserId")]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("TaskId")]
    public virtual TaskItem? Task { get; set; }

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
}

// Fixed category list per FR-004 — Category is stored as text, validated here.
public static class DocumentCategories
{
    public const string ProjectDocuments = "Project Documents";
    public const string TeamResources = "Team Resources";
    public const string PersonalFiles = "Personal Files";
    public const string Reports = "Reports";
    public const string Presentations = "Presentations";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ProjectDocuments, TeamResources, PersonalFiles, Reports, Presentations, Other
    };
}
