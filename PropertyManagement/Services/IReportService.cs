using PropertyManagement.Models;

namespace PropertyManagement.Services;

public class OccupancyRow
{
    public string  PropertyName      { get; set; } = string.Empty;
    public string  PropertyCity      { get; set; } = string.Empty;
    public int     TotalUnits        { get; set; }
    public int     OccupiedUnits     { get; set; }
    public int     VacantUnits       => TotalUnits - OccupiedUnits;
    public int     OccupancyRate     => TotalUnits == 0 ? 0 : (int)Math.Round(100.0 * OccupiedUnits / TotalUnits);
    public decimal PotentialRevenue  { get; set; }
    public decimal ActualRevenue     { get; set; }
}

public class RentRollRow
{
    public string      PropertyName { get; set; } = string.Empty;
    public string      UnitNumber   { get; set; } = string.Empty;
    public string      TenantName   { get; set; } = string.Empty;
    public decimal     MonthlyRent  { get; set; }
    public DateTime    LeaseStart   { get; set; }
    public DateTime    LeaseEnd     { get; set; }
    public LeaseStatus Status       { get; set; }
    public DateTime    AsOfDate     { get; set; } = DateTime.Today;
    public int         DaysLeft     => (LeaseEnd - AsOfDate).Days;
}

public class OutstandingPaymentRow
{
    public string   TenantName          { get; set; } = string.Empty;
    public string   PropertyName        { get; set; } = string.Empty;
    public string   UnitNumber          { get; set; } = string.Empty;
    public decimal  MonthlyRent         { get; set; }
    public decimal  OutstandingBalance  { get; set; }
    public DateTime LeaseEnd            { get; set; }
}

public class ProfitLossRow
{
    public string  PropertyName  { get; set; } = string.Empty;
    public decimal RentRevenue   { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome     => RentRevenue - TotalExpenses;
    public Dictionary<ExpenseCategory, decimal> ExpenseByCategory { get; set; } = new();
}

public interface IReportService
{
    Task<List<OccupancyRow>>          GetOccupancySummaryAsync(int propertyId, DateTime? asOf = null);
    Task<List<RentRollRow>>           GetRentRollAsync(int propertyId, DateTime? asOf = null);
    Task<List<OutstandingPaymentRow>> GetOutstandingPaymentsAsync(int propertyId, DateTime? asOf = null);
    Task<List<ProfitLossRow>>         GetProfitLossAsync(int propertyId, DateTime from, DateTime to);
    Task<byte[]>                      ExportToExcelAsync(string reportType, int propertyId, DateTime? asOf = null, DateTime? plFrom = null, DateTime? plTo = null);
}
