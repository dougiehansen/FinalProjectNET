using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<List<OccupancyRow>> GetOccupancySummaryAsync(int propertyId, DateTime? asOf = null)
    {
        var date = asOf?.Date ?? DateTime.Today;

        var unitsQuery = _db.Units
            .Include(u => u.Property)
            .Include(u => u.Leases)
            .Where(u => u.IsActive && u.Property.IsActive);

        if (propertyId > 0)
            unitsQuery = unitsQuery.Where(u => u.PropertyId == propertyId);

        var units = await unitsQuery.ToListAsync();

        return units
            .GroupBy(u => u.Property)
            .Select(g =>
            {
                var occupied = g.Where(u => u.Leases.Any(l => l.StartDate <= date && l.EndDate >= date)).ToList();
                return new OccupancyRow
                {
                    PropertyName     = g.Key.Name,
                    PropertyCity     = string.IsNullOrEmpty(g.Key.City) ? g.Key.State : g.Key.City,
                    TotalUnits       = g.Count(),
                    OccupiedUnits    = occupied.Count,
                    PotentialRevenue = g.Sum(u => u.MonthlyRent),
                    ActualRevenue    = occupied.Sum(u => u.MonthlyRent)
                };
            })
            .OrderBy(r => r.PropertyName)
            .ToList();
    }

    public async Task<List<RentRollRow>> GetRentRollAsync(int propertyId, DateTime? asOf = null)
    {
        var date = asOf?.Date ?? DateTime.Today;

        var query = _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit).ThenInclude(u => u.Property)
            .Where(l => l.StartDate <= date && l.EndDate >= date);

        if (propertyId > 0)
            query = query.Where(l => l.Unit.PropertyId == propertyId);

        var leases = await query.OrderBy(l => l.Unit.Property.Name).ThenBy(l => l.Unit.UnitNumber).ToListAsync();

        return leases.Select(l => new RentRollRow
        {
            PropertyName = l.Unit.Property.Name,
            UnitNumber   = l.Unit.UnitNumber,
            TenantName   = $"{l.Tenant.FirstName} {l.Tenant.LastName}",
            MonthlyRent  = l.MonthlyRent,
            LeaseStart   = l.StartDate,
            LeaseEnd     = l.EndDate,
            Status       = l.Status,
            AsOfDate     = date
        }).ToList();
    }

    public async Task<List<OutstandingPaymentRow>> GetOutstandingPaymentsAsync(int propertyId, DateTime? asOf = null)
    {
        var date = asOf?.Date ?? DateTime.Today;

        var query = _db.Leases
            .Include(l => l.Tenant)
            .Include(l => l.Unit).ThenInclude(u => u.Property)
            .Include(l => l.RentPayments)
            .Where(l => l.StartDate <= date && l.EndDate >= date);

        if (propertyId > 0)
            query = query.Where(l => l.Unit.PropertyId == propertyId);

        var leases = await query.ToListAsync();

        return leases
            .Select(l =>
            {
                var months    = (int)Math.Max(1, Math.Floor((date - l.StartDate).TotalDays / 30.44));
                var totalPaid = l.RentPayments.Where(p => p.PaymentDate <= date).Sum(p => p.Amount);
                var balance   = Math.Max(0, months * l.MonthlyRent - totalPaid);
                return new OutstandingPaymentRow
                {
                    TenantName         = $"{l.Tenant.FirstName} {l.Tenant.LastName}",
                    PropertyName       = l.Unit.Property.Name,
                    UnitNumber         = l.Unit.UnitNumber,
                    MonthlyRent        = l.MonthlyRent,
                    OutstandingBalance = balance,
                    LeaseEnd           = l.EndDate
                };
            })
            .Where(r => r.OutstandingBalance > 0)
            .OrderByDescending(r => r.OutstandingBalance)
            .ToList();
    }

    public async Task<byte[]> ExportToExcelAsync(string reportType, int propertyId, DateTime? asOf = null)
    {
        using var wb = new XLWorkbook();

        switch (reportType)
        {
            case "OccupancySummary":
            {
                var rows = await GetOccupancySummaryAsync(propertyId, asOf);
                var ws   = wb.AddWorksheet("Occupancy Summary");
                string[] headers = ["Property", "City", "Total Units", "Occupied", "Vacant", "Occupancy %", "Actual Revenue (€)", "Potential Revenue (€)"];
                WriteHeaders(ws, headers);
                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i]; int row = i + 2;
                    ws.Cell(row, 1).Value = r.PropertyName;
                    ws.Cell(row, 2).Value = r.PropertyCity;
                    ws.Cell(row, 3).Value = r.TotalUnits;
                    ws.Cell(row, 4).Value = r.OccupiedUnits;
                    ws.Cell(row, 5).Value = r.VacantUnits;
                    ws.Cell(row, 6).Value = r.OccupancyRate;
                    ws.Cell(row, 7).Value = (double)r.ActualRevenue;
                    ws.Cell(row, 8).Value = (double)r.PotentialRevenue;
                }
                ws.Columns().AdjustToContents();
                break;
            }
            case "RentRoll":
            {
                var rows = await GetRentRollAsync(propertyId, asOf);
                var ws   = wb.AddWorksheet("Rent Roll");
                string[] headers = ["Property", "Unit", "Tenant", "Monthly Rent (€)", "Lease Start", "Lease End", "Days Left"];
                WriteHeaders(ws, headers);
                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i]; int row = i + 2;
                    ws.Cell(row, 1).Value = r.PropertyName;
                    ws.Cell(row, 2).Value = r.UnitNumber;
                    ws.Cell(row, 3).Value = r.TenantName;
                    ws.Cell(row, 4).Value = (double)r.MonthlyRent;
                    ws.Cell(row, 5).Value = r.LeaseStart.ToString("dd/MM/yyyy");
                    ws.Cell(row, 6).Value = r.LeaseEnd.ToString("dd/MM/yyyy");
                    ws.Cell(row, 7).Value = r.DaysLeft;
                }
                ws.Columns().AdjustToContents();
                break;
            }
            case "OutstandingPayments":
            {
                var rows = await GetOutstandingPaymentsAsync(propertyId, asOf);
                var ws   = wb.AddWorksheet("Outstanding Payments");
                string[] headers = ["Tenant", "Property", "Unit", "Monthly Rent (€)", "Outstanding Balance (€)", "Lease End"];
                WriteHeaders(ws, headers);
                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i]; int row = i + 2;
                    ws.Cell(row, 1).Value = r.TenantName;
                    ws.Cell(row, 2).Value = r.PropertyName;
                    ws.Cell(row, 3).Value = r.UnitNumber;
                    ws.Cell(row, 4).Value = (double)r.MonthlyRent;
                    ws.Cell(row, 5).Value = (double)r.OutstandingBalance;
                    ws.Cell(row, 6).Value = r.LeaseEnd.ToString("dd/MM/yyyy");
                }
                ws.Columns().AdjustToContents();
                break;
            }
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }
}
