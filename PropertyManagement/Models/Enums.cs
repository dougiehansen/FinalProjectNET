namespace PropertyManagement.Models;

public enum LeaseStatus
{
    Active,
    Expired,
    Terminated
}

public enum MaintenanceStatus
{
    Open,
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
    BankTransfer,
    Cash,
    Cheque,
    CreditCard
}
