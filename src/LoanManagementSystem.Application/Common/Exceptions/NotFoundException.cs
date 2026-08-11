namespace LoanManagementSystem.Application.Common.Exceptions;

/// <summary>Maps to a 404 in the API layer's exception-handling middleware.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }
}
