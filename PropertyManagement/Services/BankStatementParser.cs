using System.Globalization;
using System.Text;

namespace PropertyManagement.Services;

/// <summary>
/// Parses a CSV bank statement into a list of credit transactions.
/// Handles column names and date formats from AIB, BOI, Revolut, and
/// generic bank exports. Only positive (credit) rows are returned.
/// </summary>
public class BankStatementParser
{
    // Supported date formats in priority order
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

        var headers = SplitLine(lines[0].TrimEnd('\r'))
            .Select(h => h.Trim('"').Trim().ToLowerInvariant())
            .ToArray();

        int dateCol   = FindColumn(headers, "date", "value date", "transaction date", "posted date");
        int amountCol = FindColumn(headers, "credit", "amount", "debit", "value");
        int descCol   = FindColumn(headers, "description", "narrative", "details", "memo", "transaction details");
        int refCol    = FindColumn(headers, "reference", "ref", "transaction id", "transaction ref");

        // Cannot parse without at least date and amount
        if (dateCol < 0 || amountCol < 0)
            return [];

        var results = new List<BankTransaction>();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitLine(line);
            int maxRequired = Math.Max(dateCol, amountCol);
            if (cols.Length <= maxRequired) continue;

            if (!TryParseDate(cols[dateCol], out var date))    continue;
            if (!TryParseAmount(cols[amountCol], out var amount)) continue;
            if (amount <= 0) continue; // skip debits and zero rows

            results.Add(new BankTransaction(
                Date:        date,
                Amount:      amount,
                Description: SafeGet(cols, descCol),
                Reference:   SafeGet(cols, refCol)
            ));
        }

        return results;
    }

    // --- Helpers ---

    private static int FindColumn(string[] headers, params string[] candidates)
    {
        foreach (var candidate in candidates)
            for (int i = 0; i < headers.Length; i++)
                if (headers[i].Contains(candidate, StringComparison.OrdinalIgnoreCase))
                    return i;
        return -1;
    }

    private static bool TryParseDate(string raw, out DateTime result)
    {
        raw = raw.Trim('"').Trim();
        return DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static bool TryParseAmount(string raw, out decimal result)
    {
        // Strip currency symbols, spaces, and thousands separators
        raw = raw.Trim('"').Trim()
                 .Replace("€", "").Replace("$", "").Replace("£", "")
                 .Replace(" ", "").Replace(",", "");
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Splits a CSV line respecting double-quoted fields that may contain commas.
    /// </summary>
    public static string[] SplitLine(string line)
    {
        var fields = new List<string>();
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
