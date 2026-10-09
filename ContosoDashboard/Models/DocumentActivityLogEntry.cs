using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

// DocumentId is intentionally a plain value, NOT an enforced FK/navigation, so
// the audit trail survives after a Document row is hard-deleted (FR-015, FR-021).
public class DocumentActivityLogEntry
{
    [Key]
    public int LogId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DocumentTitleSnapshot { get; set; } = string.Empty;

    [Required]
    public int ActorUserId { get; set; }

    [Required]
    public DocumentActionType ActionType { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [ForeignKey("ActorUserId")]
    public virtual User ActorUser { get; set; } = null!;
}

public enum DocumentActionType
{
    Upload,
    Download,
    Delete,
    Share
}
