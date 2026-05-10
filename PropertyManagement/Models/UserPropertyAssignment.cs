namespace PropertyManagement.Models;

public class UserPropertyAssignment
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
}
