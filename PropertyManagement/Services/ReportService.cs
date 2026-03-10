using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public record OccupancyRow(string Property, int TotalUnits, int Occupied, int Vacant, decimal OccupancyRate);
public record RentRollRow(string Property, string Unit, string Tenant, decimal MonthlyRent, DateTime LeaseStart, DateTime LeaseEnd, LeaseStatus Status);
public record OutstandingPaymentRow(string Tenant, string Unit, string Property, decimal MonthlyRent, decimal TotalPaid, decimal OutstandingBalance, int DaysOverdue);
public record MaintenanceLogRow(int Id, string Property, string Unit, string Title, UrgencyLevel Urgency, MaintenanceStatus Status, string AssignedTo, DateTime Submitted, DateTime? Completed, decimal? Cost);

public interface IReportService
{
    Task<List<OccupancyRow>> GetOccupancyReportAsync(int? propertyId = null);
    Task<List<RentRollRow>> GetRentRollAsync(int? propertyId = null);
    Task<List<OutstandingPaymentRow>> GetOutstandingPaymentsAsync(int? propertyId = null);
    Task<List<MaintenanceLogRow>> GetMaintenanceLogAsync(int? propertyId = null, DateTime? from = null, DateTime? to = null);
    byte[] ExportToExcel<T>(IEnumerable<T> rows, string sheetName);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<List<OccupancyRow>> GetOccupancyReportAsync(int? propertyId = null)
    {
        var query = _db.Properties.Include(p => p.Units).AsQueryable();
        if (propertyId.HasValue) query = query.Where(p => p.Id == propertyId);

        var props = await query.Where(p => p.IsActive).ToListAsync();
        return props.Select(p =>
        {
            var units = p.Units.Where(u => u.IsActive).ToList();
            var occupied = units.Count(u => u.IsOccupied);
            var total = units.Count;
            return new OccupancyRow(p.Name, total, occupied, total - occupied,
                total == 0 ? 0 : Math.Round((decimal)occupied / total * 100, 1));
        }).ToList();
    }

    public async Task<List<RentRollRow>> GetRentRollAsync(int? propertyId = null)
    {
        var query = _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit).ThenInclude(u => u.Property)
            .AsQueryable();

        if (propertyId.HasValue) query = query.Where(l => l.Unit.PropertyId == propertyId);

        var leases = await query.Where(l => l.Status == LeaseStatus.Active).ToListAsync();

        return leases.Select(l => new RentRollRow(
            l.Unit.Property.Name, l.Unit.UnitNumber, l.Tenant.FullName,
            l.MonthlyRent, l.StartDate, l.EndDate, l.Status)).ToList();
    }

    public async Task<List<OutstandingPaymentRow>> GetOutstandingPaymentsAsync(int? propertyId = null)
    {
        var query = _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit).ThenInclude(u => u.Property)
            .Include(l => l.RentPayments)
            .Where(l => l.Status == LeaseStatus.Active)
            .AsQueryable();

        if (propertyId.HasValue) query = query.Where(l => l.Unit.PropertyId == propertyId);

        var leases = await query.ToListAsync();
        var result = new List<OutstandingPaymentRow>();

        foreach (var l in leases)
        {
            var monthsElapsed = Math.Max(0, (int)Math.Ceiling((DateTime.Today - l.StartDate).TotalDays / 30.0));
            var totalDue = l.MonthlyRent * monthsElapsed;
            var totalPaid = l.RentPayments.Sum(p => p.Amount);
            var balance = totalDue - totalPaid;
            if (balance > 0)
            {
                var daysOverdue = (int)(DateTime.Today - l.StartDate.AddDays(30 * monthsElapsed)).TotalDays;
                result.Add(new OutstandingPaymentRow(
                    l.Tenant.FullName, l.Unit.UnitNumber, l.Unit.Property.Name,
                    l.MonthlyRent, totalPaid, balance, Math.Max(0, daysOverdue)));
            }
        }

        return result.OrderByDescending(r => r.OutstandingBalance).ToList();
    }

    public async Task<List<MaintenanceLogRow>> GetMaintenanceLogAsync(int? propertyId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Unit)
            .Include(m => m.AssignedTo)
            .AsQueryable();

        if (propertyId.HasValue) query = query.Where(m => m.PropertyId == propertyId);
        if (from.HasValue) query = query.Where(m => m.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(m => m.CreatedAt <= to.Value.AddDays(1));

        var requests = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

        return requests.Select(m => new MaintenanceLogRow(
            m.Id, m.Property.Name, m.Unit.UnitNumber, m.Title,
            m.UrgencyLevel, m.Status,
            m.AssignedTo?.FullName ?? "Unassigned",
            m.CreatedAt, m.CompletionDate, m.EstimatedCost)).ToList();
    }

    public byte[] ExportToExcel<T>(IEnumerable<T> rows, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        var list = rows.ToList();

        if (!list.Any())
        {
            ws.Cell(1, 1).Value = "No data found.";
            using var ms2 = new MemoryStream();
            wb.SaveAs(ms2);
            return ms2.ToArray();
        }

        var props = typeof(T).GetProperties();

        // Header row
        for (int i = 0; i < props.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = props[i].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.SteelBlue;
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        for (int r = 0; r < list.Count; r++)
        {
            for (int c = 0; c < props.Length; c++)
            {
                var val = props[c].GetValue(list[r]);
                var cell = ws.Cell(r + 2, c + 1);
                if (val is DateTime dt) cell.Value = dt.ToString("dd/MM/yyyy");
                else if (val is decimal dec) cell.Value = dec;
                else if (val is int i) cell.Value = i;
                else if (val is bool b) cell.Value = b ? "Yes" : "No";
                else cell.Value = val?.ToString() ?? "";
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
