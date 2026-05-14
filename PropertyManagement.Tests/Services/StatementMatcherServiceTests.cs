using PropertyManagement.Models;
using PropertyManagement.Services;

namespace PropertyManagement.Tests.Services;

public class StatementMatcherServiceTests
{
    readonly StatementMatcherService _svc = new();

    static Lease MakeLease(int id, string firstName, string lastName, string unit, string propName, decimal rent) =>
        new()
        {
            Id          = id,
            MonthlyRent = rent,
            Tenant      = new Tenant { Id = id, FirstName = firstName, LastName = lastName, Email = "t@x.com" },
            Unit        = new Unit   { Id = id, UnitNumber = unit, Property = new Property { Id = id, Name = propName, Address = "X", City = "C" } }
        };

    static BankTransaction MakeTx(decimal amount, DateTime date, string desc = "", string reference = "") =>
        new(date, amount, desc, reference);

    // ── Exact amount match ────────────────────────────────────────────────────

    [Fact]
    public void Match_ExactAmount_ScoresHigh()
    {
        var lease = MakeLease(1, "Tom", "Lee", "A1", "Park House", 1000);
        var tx    = MakeTx(1000, new DateTime(2026, 5, 1), "Tom Lee rent");

        var results = _svc.Match([tx], [lease]);

        Assert.Single(results);
        Assert.True(results[0].Score >= StatementMatcherService.ConfidenceThreshold);
        Assert.True(results[0].Confirmed);
    }

    [Fact]
    public void Match_CloseAmount_WithinFivePercent_StillMatches()
    {
        var lease = MakeLease(1, "Alice", "Brown", "B2", "Oak View", 1000);
        var tx    = MakeTx(1040, new DateTime(2026, 5, 2), "Alice Brown");

        var results = _svc.Match([tx], [lease]);

        Assert.True(results[0].Score > 0);
    }

    // ── Date proximity ────────────────────────────────────────────────────────

    [Fact]
    public void Match_PaymentWithinFiveDays_AddsCloseDatePoints()
    {
        var lease   = MakeLease(1, "Tom", "Lee", "A1", "Prop", 1000);
        var earlyTx = MakeTx(1000, new DateTime(2026, 5, 3));  // 3 days after 1st
        var lateTx  = MakeTx(1000, new DateTime(2026, 5, 15)); // 14 days after 1st

        var earlyResult = _svc.Match([earlyTx], [lease])[0];
        var lateResult  = _svc.Match([lateTx],  [lease])[0];

        Assert.True(earlyResult.Score > lateResult.Score);
    }

    [Fact]
    public void Match_PaymentWithinTenDays_AddsNearDatePoints()
    {
        var lease  = MakeLease(1, "Tom", "Lee", "A1", "Prop", 1000);
        var tx     = MakeTx(1000, new DateTime(2026, 5, 8)); // 7 days after 1st

        var result = _svc.Match([tx], [lease])[0];
        Assert.True(result.Score > 0);
        Assert.Contains(result.Reasons, r => r.Contains("10 days"));
    }

    // ── Tenant name matching ──────────────────────────────────────────────────

    [Fact]
    public void Match_FullNameInDescription_AddsFullNamePoints()
    {
        var lease  = MakeLease(1, "Alice", "Murphy", "C3", "Prop", 900);
        var tx     = MakeTx(900, new DateTime(2026, 5, 1), "Alice Murphy monthly rent");

        var result = _svc.Match([tx], [lease])[0];
        Assert.Contains(result.Reasons, r => r.Contains("Full tenant name"));
    }

    [Fact]
    public void Match_LastNameOnly_AddsLastNamePoints()
    {
        var lease  = MakeLease(1, "Bob", "Kelly", "D4", "Prop", 850);
        var tx     = MakeTx(850, new DateTime(2026, 5, 1), "Kelly rent payment");

        var result = _svc.Match([tx], [lease])[0];
        Assert.Contains(result.Reasons, r => r.Contains("surname"));
    }

    [Fact]
    public void Match_FirstNameOnly_AddsFirstNamePoints()
    {
        var lease  = MakeLease(1, "Maria", "Gonzalez", "E5", "Prop", 750);
        var tx     = MakeTx(750, new DateTime(2026, 5, 1), "maria transfer");

        var result = _svc.Match([tx], [lease])[0];
        Assert.Contains(result.Reasons, r => r.Contains("first name"));
    }

    // ── Unit number and property name ─────────────────────────────────────────

    [Fact]
    public void Match_UnitNumberInDescription_AddsUnitPoints()
    {
        var lease  = MakeLease(1, "Sam", "Clarke", "12B", "Prop", 1100);
        var tx     = MakeTx(1100, new DateTime(2026, 5, 1), "Unit 12B payment");

        var result = _svc.Match([tx], [lease])[0];
        Assert.Contains(result.Reasons, r => r.Contains("Unit number"));
    }

    [Fact]
    public void Match_PropertyNameInDescription_AddsPropertyPoints()
    {
        var lease  = MakeLease(1, "Sam", "Clarke", "A1", "Riverside", 1100);
        var tx     = MakeTx(1100, new DateTime(2026, 5, 1), "riverside rent may");

        var result = _svc.Match([tx], [lease])[0];
        Assert.Contains(result.Reasons, r => r.Contains("Property name"));
    }

    // ── Confidence threshold and auto-confirm ─────────────────────────────────

    [Fact]
    public void Match_LowScore_IsNotConfirmed()
    {
        var lease  = MakeLease(1, "Unknown", "Person", "Z9", "FarAway", 999);
        var tx     = MakeTx(500, new DateTime(2026, 5, 20), "unrelated transaction");

        var result = _svc.Match([tx], [lease])[0];

        Assert.False(result.Confirmed);
        Assert.Null(result.MatchedLease);
    }

    [Fact]
    public void Match_HighScore_AutoConfirmsAndSetsSelectedLeaseId()
    {
        var lease  = MakeLease(7, "John", "Walsh", "F6", "Prop", 1200);
        var tx     = MakeTx(1200, new DateTime(2026, 5, 1), "John Walsh Rent F6");

        var result = _svc.Match([tx], [lease])[0];

        Assert.True(result.Confirmed);
        Assert.Equal(7, result.SelectedLeaseId);
        Assert.NotNull(result.MatchedLease);
    }

    // ── Multiple leases — best match wins ─────────────────────────────────────

    [Fact]
    public void Match_SelectsBestMatchFromMultipleLeases()
    {
        var leases = new List<Lease>
        {
            MakeLease(1, "Alice", "Smith",  "A1", "Prop", 1000),
            MakeLease(2, "Brian", "Murphy", "B2", "Prop", 800),
        };
        var tx = MakeTx(1000, new DateTime(2026, 5, 1), "Alice Smith rent A1");

        var result = _svc.Match([tx], leases)[0];

        Assert.Equal(1, result.MatchedLease?.Id);
    }

    // ── Empty inputs ──────────────────────────────────────────────────────────

    [Fact]
    public void Match_NoTransactions_ReturnsEmptyList()
    {
        var result = _svc.Match([], [MakeLease(1, "A", "B", "1", "P", 1000)]);
        Assert.Empty(result);
    }

    [Fact]
    public void Match_NoLeases_ReturnsUnconfirmedResults()
    {
        var tx     = MakeTx(1000, new DateTime(2026, 5, 1), "Some rent");
        var result = _svc.Match([tx], []);
        Assert.Single(result);
        Assert.False(result[0].Confirmed);
    }

    // ── ConfidenceLabel ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(80, "High")]
    [InlineData(60, "Medium")]
    [InlineData(30, "Low")]
    public void ConfidenceLabel_ReturnsCorrectLabel(int score, string expected)
    {
        var result = new MatchResult
        {
            Transaction = MakeTx(1000, DateTime.Today),
            Score       = score
        };
        Assert.Equal(expected, result.ConfidenceLabel);
    }
}
