using PropertyManagement.Models;

namespace PropertyManagement.Services;

public interface IDocumentService
{
    Task SendSigningEmailAsync(Lease lease);
    Task UploadSignedDocumentAsync(Lease lease);
}
