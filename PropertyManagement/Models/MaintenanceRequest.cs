using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class MaintenanceRequest
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public int SubmittedByUserId { get; set; }
    public ApplicationUser SubmittedBy { get; set; } = null!;

    public int? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Medium;

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    [MaxLength(100)]
    public string Priority { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string AssignmentNotes { get; set; } = string.Empty;

    public DateTime? CompletionDate { get; set; }

    [MaxLength(500)]
    public string MaterialsUsed { get; set; } = string.Empty;

    public decimal? EstimatedCost { get; set; }

    [MaxLength(2000)]
    public string CompletionNotes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
