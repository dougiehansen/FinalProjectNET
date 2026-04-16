using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IUserService
{
    Task<User?> ValidateCredentialsAsync(string email, string password);
    Task<User?> GetByEmailAsync(string email);
    Task UpdateLastLoginAsync(int userId);
    Task<List<User>> GetAllAsync();
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task CreateAsync(User user, string password);
    Task UpdateAsync(User user, string? newPassword = null);
    Task DeactivateAsync(int userId);
    Task ActivateAsync(int userId);

    /// <summary>
    /// Returns all active users with the given role — used to populate
    /// the maintenance staff assignment dropdown.
    /// </summary>
    Task<List<User>> GetByRoleAsync(UserRole role);
}
