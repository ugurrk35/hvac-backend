namespace ECommerce.API.Services;

public interface ITurnstileValidator
{
    Task<TurnstileValidationResult> ValidateAsync(
        string? token,
        string? remoteIp,
        string? expectedAction = null,
        CancellationToken cancellationToken = default);
}

public sealed record TurnstileValidationResult(bool Success, string? ErrorCode = null)
{
    public static TurnstileValidationResult Valid() => new(true);
    public static TurnstileValidationResult Invalid(string errorCode) => new(false, errorCode);
}
