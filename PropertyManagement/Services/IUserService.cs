using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IUserService
{
    Task<User?> ValidateCredentialsAsync(string email, string password);
    Task UpdateLastLoginAsync(int userId);
}
