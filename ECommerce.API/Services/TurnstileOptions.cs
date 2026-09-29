namespace ECommerce.API.Services;

public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    // Disabled by default so a deployment cannot interrupt forms before the keys are configured.
    public bool Enabled { get; init; }
    public string SecretKey { get; init; } = string.Empty;
    public string? ExpectedHostname { get; init; }
}
