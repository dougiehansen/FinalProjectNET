using PropertyManagement.Models;

namespace PropertyManagement.Services;

/// <summary>
/// A single credit row parsed from a bank statement CSV.
/// </summary>
public record BankTransaction(
    DateTime Date,
    decimal  Amount,
    string   Description,
    string   Reference
);

/// <summary>
/// The result of attempting to match one bank transaction to a lease.
/// Confirmed and SelectedLeaseId are mutable so the user can override
/// them in the review UI before committing.
/// </summary>
public class MatchResult
{
    public required BankTransaction Transaction    { get; init; }
    public Lease?                   MatchedLease   { get; init; }
    public int                      Score          { get; init; }
    public List<string>             Reasons        { get; init; } = [];
    public bool                     Confirmed      { get; set; }
    public int                      SelectedLeaseId { get; set; }

    /// <summary>High (≥70), Medium (50–69), Low (&lt;50).</summary>
    public string ConfidenceLabel => Score switch
    {
        >= 70 => "High",
        >= 50 => "Medium",
        _     => "Low"
    };
}

/// <summary>
/// Matches a list of bank transactions against active leases using a
/// weighted scoring algorithm. Each transaction is scored against every
/// lease on four criteria: amount proximity, date proximity, tenant name,
/// and unit/property reference in the transaction description.
/// </summary>
public class StatementMatcherService
{
    // --- Scoring weights ---
    private const int ExactAmountPts   = 50; // amount == MonthlyRent exactly
    private const int CloseAmountPts   = 25; // amount within 5 %
    private const int CloseDatePts     = 20; // within 5 days of the 1st
    private const int NearDatePts      = 10; // within 10 days of the 1st
    private const int FullNamePts      = 30; // full tenant name in description
    private const int LastNamePts      = 15; // surname only in description
    private const int FirstNamePts     = 8;  // first name only in description
    private const int UnitPts          = 12; // unit number in description
    private const int PropertyPts      = 8;  // property name in description

    /// <summary>
    /// Minimum score for a match to be auto-confirmed.
    /// Transactions below this threshold appear in the review table
    /// as "unmatched" and require manual lease selection.
    /// </summary>
    public const int ConfidenceThreshold = 50;

    /// <summary>
    /// Runs the matching algorithm over every transaction × lease pair
    /// and returns the best match (or no match) for each transaction.
    /// </summary>
    public List<MatchResult> Match(List<BankTransaction> transactions, List<Lease> leases)
        => transactions.Select(t => MatchOne(t, leases)).ToList();

    private static MatchResult MatchOne(BankTransaction t, List<Lease> leases)
    {
        Lease?       bestLease   = null;
        int          bestScore   = 0;
        List<string> bestReasons = [];

        foreach (var lease in leases)
        {
            var reasons = new List<string>();
            int score   = 0;

            // 1 ── Amount proximity
            var diff       = Math.Abs(t.Amount - lease.MonthlyRent);
            var pctDiff    = lease.MonthlyRent > 0 ? diff / lease.MonthlyRent : 1m;

            if (diff == 0)
            {
                score += ExactAmountPts;
                reasons.Add("Exact amount match");
            }
            else if (pctDiff <= 0.05m)
            {
                score += CloseAmountPts;
                reasons.Add($"Amount within 5% (€{diff:N2} off)");
            }

            // 2 ── Date proximity to the 1st of the month
            var dueDate  = new DateTime(t.Date.Year, t.Date.Month, 1);
            var daysDiff = Math.Abs((t.Date - dueDate).TotalDays);

            if (daysDiff <= 5)
            {
                score += CloseDatePts;
                reasons.Add("Paid within 5 days of due date");
            }
            else if (daysDiff <= 10)
            {
                score += NearDatePts;
                reasons.Add("Paid within 10 days of due date");
            }

            // 3 ── Tenant name presence in description or reference
            var haystack  = $"{t.Description} {t.Reference}".ToLowerInvariant();
            var firstName = lease.Tenant.FirstName.ToLowerInvariant();
            var lastName  = lease.Tenant.LastName.ToLowerInvariant();
            var fullName  = lease.Tenant.FullName.ToLowerInvariant();

            if (haystack.Contains(fullName))
            {
                score += FullNamePts;
                reasons.Add("Full tenant name found in description");
            }
            else if (haystack.Contains(lastName) && haystack.Contains(firstName))
            {
                score += FullNamePts;
                reasons.Add("Full tenant name found in description");
            }
            else if (haystack.Contains(lastName))
            {
                score += LastNamePts;
                reasons.Add("Tenant surname found in description");
            }
            else if (haystack.Contains(firstName))
            {
                score += FirstNamePts;
                reasons.Add("Tenant first name found in description");
            }

            // 4 ── Unit number and property name in description
            var unitNumber   = lease.Unit.UnitNumber.ToLowerInvariant();
            var propertyName = lease.Unit.Property.Name.ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(unitNumber) && haystack.Contains(unitNumber))
            {
                score += UnitPts;
                reasons.Add($"Unit number ({lease.Unit.UnitNumber}) found in description");
            }

            if (!string.IsNullOrWhiteSpace(propertyName) && haystack.Contains(propertyName))
            {
                score += PropertyPts;
                reasons.Add($"Property name found in description");
            }

            if (score > bestScore)
            {
                bestScore   = score;
                bestLease   = lease;
                bestReasons = reasons;
            }
        }

        bool autoConfirmed = bestScore >= ConfidenceThreshold;

        return new MatchResult
        {
            Transaction     = t,
            MatchedLease    = autoConfirmed ? bestLease : null,
            Score           = bestScore,
            Reasons         = bestReasons,
            Confirmed       = autoConfirmed,
            SelectedLeaseId = autoConfirmed ? bestLease!.Id : 0,
        };
    }
}
