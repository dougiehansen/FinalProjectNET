using PropertyManagement.Services;

namespace PropertyManagement.Tests.Services;

public class BankStatementParserTests
{
    readonly BankStatementParser _parser = new();

    // ── Basic parsing ─────────────────────────────────────────────────────────

    [Fact]
    public void ParseCsv_StandardFormat_ReturnsTransactions()
    {
        var csv = "Date,Amount,Description\n01/05/2026,1000.00,Rent payment\n15/05/2026,500.00,Deposit\n";
        var result = _parser.ParseCsv(csv);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseCsv_ReturnsEmpty_WhenOnlyHeader()
    {
        var csv = "Date,Amount,Description\n";
        var result = _parser.ParseCsv(csv);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCsv_ReturnsEmpty_WhenEmptyString()
    {
        var result = _parser.ParseCsv(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCsv_ReturnsEmpty_WhenNoDateOrAmountColumns()
    {
        var csv = "Name,Notes\nAlice,Something\n";
        var result = _parser.ParseCsv(csv);
        Assert.Empty(result);
    }

    // ── Amount parsing ────────────────────────────────────────────────────────

    [Fact]
    public void ParseCsv_ParsesAmountCorrectly()
    {
        var csv = "Date,Amount,Description\n01/05/2026,1234.56,Test\n";
        var result = _parser.ParseCsv(csv);
        Assert.Equal(1234.56m, result[0].Amount);
    }

    [Fact]
    public void ParseCsv_StripsCurrencySymbols()
    {
        var csv = "Date,Amount,Description\n01/05/2026,€950.00,Rent\n";
        var result = _parser.ParseCsv(csv);
        Assert.Equal(950m, result[0].Amount);
    }

    [Fact]
    public void ParseCsv_SkipsRows_WithZeroOrNegativeAmount()
    {
        var csv = "Date,Amount,Description\n01/05/2026,0,Zero\n02/05/2026,-100,Refund\n03/05/2026,500,Good\n";
        var result = _parser.ParseCsv(csv);
        Assert.Single(result);
        Assert.Equal(500m, result[0].Amount);
    }

    // ── Credit/Debit column layout (AIB/BOI format) ───────────────────────────

    [Fact]
    public void ParseCsv_CreditDebitColumns_UsesCreditColumn()
    {
        var csv = "Date,Debit,Credit,Description\n01/05/2026,,1200.00,Rent in\n";
        var result = _parser.ParseCsv(csv);
        Assert.Single(result);
        Assert.Equal(1200m, result[0].Amount);
    }

    [Fact]
    public void ParseCsv_CreditDebitColumns_FallsBackToDebit_WhenCreditEmpty()
    {
        var csv = "Date,Debit,Credit,Description\n01/05/2026,800.00,,Payment out\n";
        var result = _parser.ParseCsv(csv);
        Assert.Single(result);
        Assert.Equal(800m, result[0].Amount);
    }

    // ── Date parsing ──────────────────────────────────────────────────────────

    [Fact]
    public void ParseCsv_ParsesDdMmYyyy()
    {
        var csv = "Date,Amount,Description\n15/03/2026,700,Rent\n";
        var result = _parser.ParseCsv(csv);
        Assert.Equal(new DateTime(2026, 3, 15), result[0].Date);
    }

    [Fact]
    public void ParseCsv_ParsesIsoDate()
    {
        var csv = "Date,Amount,Description\n2026-04-10,750,April rent\n";
        var result = _parser.ParseCsv(csv);
        Assert.Equal(new DateTime(2026, 4, 10), result[0].Date);
    }

    [Fact]
    public void ParseCsv_SkipsRows_WithUnparseableDate()
    {
        var csv = "Date,Amount,Description\nNOTADATE,900,Rent\n01/05/2026,1000,Good\n";
        var result = _parser.ParseCsv(csv);
        Assert.Single(result);
    }

    // ── TSV support ───────────────────────────────────────────────────────────

    [Fact]
    public void ParseCsv_HandlesTabSeparated()
    {
        var tsv = "Date\tAmount\tDescription\n01/05/2026\t1100\tRent TSV\n";
        var result = _parser.ParseCsv(tsv);
        Assert.Single(result);
        Assert.Equal(1100m, result[0].Amount);
    }

    // ── Description and Reference ─────────────────────────────────────────────

    [Fact]
    public void ParseCsv_PopulatesDescription()
    {
        var csv = "Date,Amount,Description\n01/05/2026,1000,Tom Smith Rent May\n";
        var result = _parser.ParseCsv(csv);
        Assert.Equal("Tom Smith Rent May", result[0].Description);
    }

    [Fact]
    public void ParseCsv_PopulatesReference_WhenColumnPresent()
    {
        var csv = "Date,Amount,Description,Reference\n01/05/2026,1000,Rent,REF001\n";
        var result = _parser.ParseCsv(csv);
        Assert.Equal("REF001", result[0].Reference);
    }

    // ── Quoted fields ─────────────────────────────────────────────────────────

    [Fact]
    public void ParseCsv_HandlesQuotedFieldsWithCommas()
    {
        var csv = "Date,Amount,Description\n01/05/2026,1000,\"Smith, John - Rent\"\n";
        var result = _parser.ParseCsv(csv);
        Assert.Single(result);
        Assert.Equal("Smith, John - Rent", result[0].Description);
    }

    // ── SplitLine ─────────────────────────────────────────────────────────────

    [Fact]
    public void SplitLine_SplitsOnComma()
    {
        var parts = BankStatementParser.SplitLine("a,b,c");
        Assert.Equal(["a", "b", "c"], parts);
    }

    [Fact]
    public void SplitLine_SplitsOnTab()
    {
        var parts = BankStatementParser.SplitLine("a\tb\tc", '\t');
        Assert.Equal(["a", "b", "c"], parts);
    }

    [Fact]
    public void SplitLine_RespectsQuotedCommas()
    {
        var parts = BankStatementParser.SplitLine("\"hello, world\",next");
        Assert.Equal(2, parts.Length);
        Assert.Equal("hello, world", parts[0]);
    }
}
