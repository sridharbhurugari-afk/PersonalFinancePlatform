namespace PFP.Contracts.Exceptions;

/// <summary>
/// Base type for exceptions whose message is safe to show to API clients.
/// Anything not deriving from this type is treated as an unexpected failure (HTTP 500, generic message).
/// Lives in Contracts (no framework dependencies) so Domain projects can throw it.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string errorCode, string message, int statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public string ErrorCode { get; }

    public int StatusCode { get; }
}

public sealed class NotFoundException(string resource, object key)
    : AppException("resource_not_found", $"{resource} '{key}' was not found.", 404);

public sealed class ConflictException(string message, string errorCode = "conflict")
    : AppException(errorCode, message, 409);

/// <summary>Raised when a domain invariant is violated (e.g. non-positive amount, inverted date range).</summary>
public sealed class DomainRuleException(string message, string errorCode = "domain_rule_violation")
    : AppException(errorCode, message, 422);

public sealed class ForbiddenException(string message = "You do not have access to this resource.")
    : AppException("forbidden", message, 403);
