using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class Lease
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public decimal MonthlyRent { get; set; }

    public decimal SecurityDeposit { get; set; }

    public LeaseStatus Status { get; set; } = LeaseStatus.Active;

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RentPayment> RentPayments { get; set; } = new List<RentPayment>();

    public bool IsExpiringSoon => Status == LeaseStatus.Active && EndDate <= DateTime.Today.AddDays(60);
}
