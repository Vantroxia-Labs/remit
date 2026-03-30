using AegisEInvoicing.Domain.Exceptions;

namespace AegisEInvoicing.Application.Common.Extensions;

public static class ExceptionExtensions
{
    /// <summary>
    /// Throws NotFoundException if the entity is null
    /// </summary>
    public static T ThrowIfNull<T>(this T? entity, string resourceName, object key) where T : class
    {
        if (entity == null)
            throw new NotFoundException(resourceName, key);

        return entity;
    }

    /// <summary>
    /// Throws NotFoundException if the entity is null with custom message
    /// </summary>
    public static T ThrowIfNull<T>(this T? entity, string message) where T : class
    {
        if (entity == null)
            throw new NotFoundException(message);

        return entity;
    }

    /// <summary>
    /// Throws ConflictException if condition is true
    /// </summary>
    public static void ThrowIfConflict(this bool condition, string message, string? errorCode = null)
    {
        if (condition)
            throw new ConflictException(message, errorCode);
    }

    /// <summary>
    /// Throws BadRequestException if condition is true
    /// </summary>
    public static void ThrowIfBadRequest(this bool condition, string message, string? errorCode = null)
    {
        if (condition)
            throw new BadRequestException(message, errorCode);
    }

    /// <summary>
    /// Throws ForbiddenException if condition is true
    /// </summary>
    public static void ThrowIfForbidden(this bool condition, string message, string? errorCode = null)
    {
        if (condition)
            throw new ForbiddenException(message, errorCode);
    }

    /// <summary>
    /// Throws UnprocessableEntityException if condition is true
    /// </summary>
    public static void ThrowIfUnprocessable(this bool condition, string message, string? errorCode = null)
    {
        if (condition)
            throw new UnprocessableEntityException(message, errorCode);
    }
}

/// <summary>
/// Static helper class for common exception scenarios
/// </summary>
public static class Guard
{
    public static void NotNull<T>(T? value, string parameterName) where T : class
    {
        if (value == null)
            throw new BadRequestException($"Parameter '{parameterName}' cannot be null", "ParameterNull");
    }

    public static void NotNullOrEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
            throw new BadRequestException($"Parameter '{parameterName}' cannot be null or empty", "ParameterNullOrEmpty");
    }

    public static void NotNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BadRequestException($"Parameter '{parameterName}' cannot be null or whitespace", "ParameterNullOrWhiteSpace");
    }

    public static void Against(bool condition, string message, string? errorCode = null)
    {
        if (condition)
            throw new BadRequestException(message, errorCode);
    }

    public static void AgainstConflict(bool condition, string message, string? errorCode = null)
    {
        if (condition)
            throw new ConflictException(message, errorCode);
    }

    public static void RequiredPermission(bool hasPermission, string message = "Access denied")
    {
        if (!hasPermission)
            throw new ForbiddenException(message);
    }

    public static void Authenticated(bool isAuthenticated, string message = "Authentication required")
    {
        if (!isAuthenticated)
            throw new AuthenticationException(message);
    }
}