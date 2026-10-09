using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

// Exactly one of SharedWithUserId / SharedWithDepartment must be set (enforced
// in DocumentService, not at the database level — see data-model.md).
public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int SharedByUserId { get; set; }

    public int? SharedWithUserId { get; set; }

    [MaxLength(100)]
    public string? SharedWithDepartment { get; set; }

    public DateTime SharedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("SharedByUserId")]
    public virtual User SharedByUser { get; set; } = null!;

    [ForeignKey("SharedWithUserId")]
    public virtual User? SharedWithUser { get; set; }
}
