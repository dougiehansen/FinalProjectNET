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
}
