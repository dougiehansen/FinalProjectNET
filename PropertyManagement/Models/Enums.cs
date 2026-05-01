namespace PropertyManagement.Models;

public enum LeaseStatus
{
    Active,
    Expired,
    Terminated
}

public enum SignatureStatus
{
    Pending,
    Signed,
    Declined
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
    BankTransfer,
    Cash,
    Cheque,
    CreditCard
}
