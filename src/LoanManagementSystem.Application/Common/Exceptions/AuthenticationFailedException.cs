namespace LoanManagementSystem.Application.Common.Exceptions;

/// <summary>Maps to a 401 in the API layer's exception-handling middleware — distinct from DomainException (400) since "wrong credentials" isn't a business-rule violation, it's an auth failure.</summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }
}
