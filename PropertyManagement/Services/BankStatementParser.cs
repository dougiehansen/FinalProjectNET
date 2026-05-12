using System.Globalization;
using System.Text;

namespace PropertyManagement.Services;

/// <summary>
/// Parses a bank statement (CSV or TSV) into a list of transactions.
/// Handles column names and date formats from AIB, BOI, Revolut, and
/// generic exports. Supports both single-Amount layouts and separate
/// Debit/Credit column layouts — amounts are taken from whichever
/// column is non-empty per row, so both landlord statements (rent as
/// Credit) and tenant statements (rent as Debit) are handled correctly.
/// </summary>
public class BankStatementParser
{
    private static readonly string[] DateFormats =
    [
        "dd/MM/yyyy", "d/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd",
        "MM/dd/yyyy", "dd.MM.yyyy", "d MMM yyyy", "dd MMM yyyy",
        "dd/MM/yy", "MM/dd/yy"
    ];

    public List<BankTransaction> ParseCsv(string csvContent)
    {
        var lines = csvContent
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
            return [];

        var headerLine = lines[0].TrimEnd('\r');
        var separator  = DetectSeparator(headerLine);

        var headers = SplitLine(headerLine, separator)
            .Select(h => h.Trim('"').Trim().ToLowerInvariant())
            .ToArray();

        int dateCol   = FindColumn(headers, "date", "value date", "transaction date", "posted date");

        // Separate credit and debit columns (e.g. AIB, BOI format)
        int creditCol = FindColumn(headers, "credit");
        int debitCol  = FindColumn(headers, "debit");

        // Single amount column (e.g. Revolut, generic CSV)
        int amountCol = creditCol >= 0
            ? creditCol
            : FindColumn(headers, "amount", "value");

        int descCol   = FindColumn(headers, "description", "narrative", "details", "memo", "transaction details");
        int refCol    = FindColumn(headers, "reference", "ref", "transaction id", "transaction ref");

        // Need at least a date and one amount source
        if (dateCol < 0 || (amountCol < 0 && debitCol < 0))
            return [];

        var results = new List<BankTransaction>();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitLine(line, separator);

            if (!TryGetDate(cols, dateCol, out var date)) continue;

            // Try credit/amount column first; fall back to debit column.
            // This means both landlord statements (rent arrives as Credit)
            // and tenant statements (rent leaves as Debit) are captured.
            decimal amount = 0;
            if (amountCol >= 0) TryParseAmount(SafeGet(cols, amountCol), out amount);
            if (amount <= 0 && debitCol >= 0) TryParseAmount(SafeGet(cols, debitCol), out amount);

            if (amount <= 0) continue;

            results.Add(new BankTransaction(
                Date:        date,
                Amount:      amount,
                Description: SafeGet(cols, descCol),
                Reference:   SafeGet(cols, refCol)
            ));
        }

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Detects whether the file uses tabs or commas as the delimiter by
    /// counting occurrences of each in the header row.
    /// </summary>
    private static char DetectSeparator(string headerLine)
    {
        int tabs   = headerLine.Count(c => c == '\t');
        int commas = headerLine.Count(c => c == ',');
        return tabs > commas ? '\t' : ',';
    }

    private static int FindColumn(string[] headers, params string[] candidates)
    {
        foreach (var candidate in candidates)
            for (int i = 0; i < headers.Length; i++)
                if (headers[i].Contains(candidate, StringComparison.OrdinalIgnoreCase))
                    return i;
        return -1;
    }

    private static bool TryGetDate(string[] cols, int index, out DateTime result)
    {
        result = default;
        if (index < 0 || index >= cols.Length) return false;
        return TryParseDate(cols[index], out result);
    }

    private static bool TryParseDate(string raw, out DateTime result)
    {
        raw = raw.Trim('"').Trim();
        return DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static bool TryParseAmount(string raw, out decimal result)
    {
        raw = raw.Trim('"').Trim()
                 .Replace("€", "").Replace("$", "").Replace("£", "")
                 .Replace(" ", "").Replace(",", "");
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Splits a line on the given separator. For commas, respects
    /// double-quoted fields that may contain commas. For tabs, a simple
    /// split is used since TSV files rarely quote fields.
    /// </summary>
    public static string[] SplitLine(string line, char separator = ',')
    {
        if (separator == '\t')
            return line.Split('\t');

        // Comma-separated: handle quoted fields
        var fields  = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return [.. fields];
    }

    private static string SafeGet(string[] cols, int index)
    {
        if (index < 0 || index >= cols.Length) return string.Empty;
        return cols[index].Trim('"').Trim();
    }
}
