using System.Net;
using Yina.Common.Abstractions.Errors;

namespace YinaCRM.Infrastructure.Support;

public static class InfrastructureErrors
{
    public static Error InvalidConfiguration(string code, string message) =>
        Error.Create(code, message, statusCode: 500);

    public static Error ExternalDependency(string code, string message, HttpStatusCode? statusCode = null)
    {
        var httpStatus = statusCode.HasValue ? (int)statusCode.Value : 502;
        return Error.Create(code, message, httpStatus);
    }

    public static Error AuthenticationFailure(string message) =>
        Errors.Unauthorized("AUTHENTICATION_FAILED", message);

    public static Error AuthorizationFailure(string message) =>
        Errors.Forbidden("AUTHORIZATION_FAILED", message);

    public static Error ValidationFailure(string message) =>
        Errors.Validation("INFRA_VALIDATION_FAILED", message);

    public static Error Unexpected(string message) =>
        Errors.Failure("INFRA_UNEXPECTED", message);
}
