using Azure.Communication.Email;
using Azure.Storage.Blobs;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class DocumentService : IDocumentService
{
    private readonly IConfiguration _config;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IConfiguration config, ILogger<DocumentService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendSigningEmailAsync(Lease lease)
    {
        var connStr = _config["Azure:AcsConnectionString"];
        var sender  = _config["Azure:SenderEmail"];
        var appUrl  = _config["Azure:AppUrl"] ?? "http://localhost:5163";

        if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(sender))
        {
            _logger.LogWarning("Azure ACS not configured — skipping signing email for lease {Id}.", lease.Id);
            return;
        }

        var signingLink = $"{appUrl}/sign/{lease.SigningToken}";
        var client      = new EmailClient(connStr);

        var message = new EmailMessage(
            senderAddress: sender,
            recipientAddress: lease.Tenant.Email,
            content: new EmailContent($"Lease Agreement Ready to Sign — {lease.Unit.Property.Name}")
            {
                Html = GenerateEmailHtml(lease, signingLink)
            });

        await client.SendAsync(Azure.WaitUntil.Started, message);
        _logger.LogInformation("Signing email sent to {Email} for lease {Id}.", lease.Tenant.Email, lease.Id);
    }

    public async Task UploadSignedDocumentAsync(Lease lease)
    {
        var connStr   = _config["Azure:StorageConnectionString"];
        var container = _config["Azure:BlobContainer"] ?? "finalprojectblob";

        if (string.IsNullOrEmpty(connStr))
        {
            _logger.LogWarning("Azure Storage not configured — skipping document upload for lease {Id}.", lease.Id);
            return;
        }

        var blobClient = new BlobServiceClient(connStr)
            .GetBlobContainerClient(container)
            .GetBlobClient($"signed-leases/lease_{lease.Id}_{lease.SignedAt:yyyyMMdd}.html");

        var html  = GenerateLeaseDocument(lease, signed: true);
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(html));
        await blobClient.UploadAsync(ms, overwrite: true);
        _logger.LogInformation("Signed document uploaded to blob for lease {Id}.", lease.Id);
    }

    static string GenerateEmailHtml(Lease lease, string signingLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#1c1c1e;">
            <div style="background:#2563eb;padding:24px;border-radius:12px 12px 0 0;">
                <h1 style="color:#fff;margin:0;font-size:22px;">Lease Agreement Ready to Sign</h1>
            </div>
            <div style="background:#f9fafb;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e5e7eb;">
                <p>Dear <strong>{lease.Tenant.FirstName}</strong>,</p>
                <p>Your lease agreement for <strong>{lease.Unit.Property.Name} — Unit {lease.Unit.UnitNumber}</strong> is ready for your review and signature.</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                    <tr><td style="padding:8px;color:#6b7280;width:140px;">Monthly Rent</td><td style="padding:8px;font-weight:600;">€{lease.MonthlyRent:N2}</td></tr>
                    <tr style="background:#fff;"><td style="padding:8px;color:#6b7280;">Lease Start</td><td style="padding:8px;font-weight:600;">{lease.StartDate:dd MMM yyyy}</td></tr>
                    <tr><td style="padding:8px;color:#6b7280;">Lease End</td><td style="padding:8px;font-weight:600;">{lease.EndDate:dd MMM yyyy}</td></tr>
                    <tr style="background:#fff;"><td style="padding:8px;color:#6b7280;">Security Deposit</td><td style="padding:8px;font-weight:600;">€{lease.SecurityDeposit:N2}</td></tr>
                </table>
                <div style="text-align:center;margin:28px 0;">
                    <a href="{signingLink}" style="background:#2563eb;color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:700;font-size:16px;">Review &amp; Sign Lease</a>
                </div>
                <p style="color:#6b7280;font-size:13px;">This link is unique to you. If you did not expect this email, please contact your property manager.</p>
            </div>
        </body>
        </html>
        """;

    public static string GenerateLeaseDocument(Lease lease, bool signed = false) => $$"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"/><style>
            body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 40px; color: #1c1c1e; }
            h1   { font-size: 26px; text-align: center; margin-bottom: 4px; }
            h2   { font-size: 16px; color: #6b7280; text-align: center; font-weight: normal; margin-top: 0; }
            hr   { border: none; border-top: 2px solid #e5e7eb; margin: 24px 0; }
            table { width: 100%; border-collapse: collapse; margin: 16px 0; }
            td   { padding: 10px 12px; border-bottom: 1px solid #e5e7eb; }
            td:first-child { color: #6b7280; width: 200px; }
            td:last-child  { font-weight: 600; }
            .clause { margin: 16px 0; line-height: 1.7; }
            .sig-block { margin-top: 40px; padding: 20px; background: #f0fdf4; border-radius: 8px; border: 1px solid #16a34a; }
        </style></head>
        <body>
            <h1>Residential Lease Agreement</h1>
            <h2>PropertyMS — Official Document</h2>
            <hr/>
            <h3>Parties</h3>
            <table>
                <tr><td>Tenant</td><td>{{lease.Tenant?.FullName ?? "—"}}</td></tr>
                <tr><td>Tenant Email</td><td>{{lease.Tenant?.Email ?? "—"}}</td></tr>
                <tr><td>Property</td><td>{{lease.Unit?.Property?.Name ?? "—"}}</td></tr>
                <tr><td>Address</td><td>{{lease.Unit?.Property?.Address ?? "—"}}, {{lease.Unit?.Property?.City ?? "—"}}</td></tr>
                <tr><td>Unit</td><td>{{lease.Unit?.UnitNumber ?? "—"}}</td></tr>
            </table>
            <hr/>
            <h3>Lease Terms</h3>
            <table>
                <tr><td>Start Date</td><td>{{lease.StartDate:dd MMMM yyyy}}</td></tr>
                <tr><td>End Date</td><td>{{lease.EndDate:dd MMMM yyyy}}</td></tr>
                <tr><td>Monthly Rent</td><td>€{{lease.MonthlyRent:N2}}</td></tr>
                <tr><td>Security Deposit</td><td>€{{lease.SecurityDeposit:N2}}</td></tr>
                <tr><td>Total Term</td><td>{{(int)(lease.EndDate - lease.StartDate).TotalDays / 30}} months</td></tr>
            </table>
            <hr/>
            <h3>Terms and Conditions</h3>
            <div class="clause"><strong>1. Rent Payment.</strong> The tenant agrees to pay the monthly rent of €{{lease.MonthlyRent:N2}} on or before the 1st day of each calendar month.</div>
            <div class="clause"><strong>2. Security Deposit.</strong> A security deposit of €{{lease.SecurityDeposit:N2}} is held against damage beyond normal wear and tear and will be returned within 30 days of lease end.</div>
            <div class="clause"><strong>3. Property Use.</strong> The tenant shall use the property solely as a private residence and shall not sublet without written consent from the landlord.</div>
            <div class="clause"><strong>4. Maintenance.</strong> The tenant shall keep the premises in a clean and sanitary condition and report any maintenance issues promptly.</div>
            <div class="clause"><strong>5. Termination.</strong> Either party may terminate this agreement with 60 days written notice prior to the end date.</div>
            {{(signed && lease.SignedByName != null ? $"""
            <hr/>
            <div class="sig-block">
                <strong>✓ Digitally Signed</strong><br/>
                Signed by: <strong>{lease.SignedByName}</strong><br/>
                Date &amp; Time: <strong>{lease.SignedAt:dd MMM yyyy HH:mm} UTC</strong>
            </div>
            """ : "")}}
        </body>
        </html>
        """;
}
