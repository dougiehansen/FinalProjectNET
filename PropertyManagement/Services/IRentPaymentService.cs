using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// Defines data operations for rent payment recording and retrieval.
/// Payments are always linked to an active lease; the outstanding balance
/// is recalculated and stored on each payment at the time of recording.
/// </summary>
public interface IRentPaymentService
{
    /// <summary>
    /// Returns all rent payments with Lease, Lease.Tenant, Lease.Unit,
    /// and Lease.Unit.Property navigation properties loaded — used by the
    /// Rent Payments management page.
    /// </summary>
    Task<List<RentPayment>> GetAllAsync();

    /// <summary>
    /// Persists a new rent payment and updates the stored OutstandingBalance
    /// field based on total expected rent minus all payments received to date
    /// for that lease.
    /// </summary>
    Task RecordPaymentAsync(RentPayment payment);
}
