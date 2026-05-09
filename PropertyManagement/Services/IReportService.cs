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
    public int         DaysLeft     => (LeaseEnd - DateTime.Today).Days;
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

public interface IReportService
{
    Task<List<OccupancyRow>>          GetOccupancySummaryAsync(int propertyId);
    Task<List<RentRollRow>>           GetRentRollAsync(int propertyId);
    Task<List<OutstandingPaymentRow>> GetOutstandingPaymentsAsync(int propertyId);
    Task<byte[]>                      ExportToExcelAsync(string reportType, int propertyId);
}
