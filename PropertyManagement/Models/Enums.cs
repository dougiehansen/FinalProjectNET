namespace PropertyManagement.Models;

public enum UserRole
{
    Administrator,
    PropertyManager,
    MaintenanceStaff,
    AccountingTeam
}

public enum LeaseStatus
{
    Active,
    Expired,
    Terminated,
    PendingRenewal
}

public enum MaintenanceStatus
{
    Open,
    Assigned,
    InProgress,
    Completed,
    Cancelled
}

public enum UrgencyLevel
{
    Low,
    Medium,
    High,
    Emergency
}

public enum PaymentMethod
{
    Cash,
    Check,
    BankTransfer,
    CreditCard,
    DirectDebit,
    Other
}

public enum ReportType
{
    OccupancySummary,
    RentRoll,
    OutstandingPayments,
    MaintenanceLog,
    FinancialSummary
}
