using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class RentPayment
{
    public int Id { get; set; }

    public int LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    public decimal OutstandingBalance { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public int RecordedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
