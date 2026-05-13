using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class Expense
{
    public int             Id          { get; set; }
    public int             PropertyId  { get; set; }
    public Property        Property    { get; set; } = null!;
    public ExpenseCategory Category    { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public decimal  Amount    { get; set; }
    public DateTime Date      { get; set; }

    [MaxLength(200)]
    public string? Vendor     { get; set; }

    [MaxLength(500)]
    public string? Notes      { get; set; }

    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
    public int      CreatedByUserId  { get; set; }
}
