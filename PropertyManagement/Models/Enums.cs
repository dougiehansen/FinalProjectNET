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

public enum PaymentMethod
{
    BankTransfer,
    Cash,
    Cheque,
    CreditCard
}
