namespace LoanManagementSystem.Domain.Promises;

/// <summary>Requirements §20. Pending/Rescheduled are the "still active, being tracked" states; Kept/Missed/Cancelled are terminal — see PromiseToPay's transition methods for which moves are allowed from which state.</summary>
public enum PromiseStatus
{
    Pending,
    Kept,
    Missed,
    Rescheduled,
    Cancelled,
}
