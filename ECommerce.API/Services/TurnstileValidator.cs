using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Services;

public sealed class TurnstileValidator : ITurnstileValidator
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TurnstileOptions _options;
    private readonly ILogger<TurnstileValidator> _logger;

    public TurnstileValidator(
        IHttpClientFactory httpClientFactory,
        IOptions<TurnstileOptions> options,
        ILogger<TurnstileValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TurnstileValidationResult> ValidateAsync(
        string? token,
        string? remoteIp,
        string? expectedAction = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return TurnstileValidationResult.Valid();
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogError("Turnstile is enabled but its secret key is not configured.");
            return TurnstileValidationResult.Invalid("configuration-error");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return TurnstileValidationResult.Invalid("missing-input-response");
        }

        var values = new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = token
        };
        if (!string.IsNullOrWhiteSpace(remoteIp)) values["remoteip"] = remoteIp;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, VerifyUrl)
            {
                Content = new FormUrlEncodedContent(values)
            };
            using var response = await _httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Turnstile verification endpoint returned HTTP {StatusCode}.", response.StatusCode);
                return TurnstileValidationResult.Invalid("verification-unavailable");
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileSiteverifyResponse>(cancellationToken: cancellationToken);
            if (result is null || !result.Success)
            {
                var errorCode = result?.ErrorCodes?.FirstOrDefault() ?? "invalid-input-response";
                _logger.LogInformation("Turnstile verification failed: {ErrorCode}", errorCode);
                return TurnstileValidationResult.Invalid(errorCode);
            }

            if (!string.IsNullOrWhiteSpace(_options.ExpectedHostname) &&
                !string.Equals(_options.ExpectedHostname, result.Hostname, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Turnstile hostname mismatch. Expected {ExpectedHostname}, received {Hostname}.", _options.ExpectedHostname, result.Hostname);
                return TurnstileValidationResult.Invalid("hostname-mismatch");
            }

            if (!string.IsNullOrWhiteSpace(expectedAction) &&
                !string.Equals(expectedAction, result.Action, StringComparison.Ordinal))
            {
                _logger.LogWarning("Turnstile action mismatch. Expected {ExpectedAction}, received {Action}.", expectedAction, result.Action);
                return TurnstileValidationResult.Invalid("action-mismatch");
            }

            return TurnstileValidationResult.Valid();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Turnstile verification request failed.");
            return TurnstileValidationResult.Invalid("verification-unavailable");
        }
    }

    private sealed class TurnstileSiteverifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; init; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; init; }

        [JsonPropertyName("action")]
        public string? Action { get; init; }
    }
}
