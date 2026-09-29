namespace ECommerce.API.Services;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }
    /// <summary>
    /// E-posta teslim sağlayıcısı. Resend seçildiğinde SMTP yerine HTTPS API kullanılır;
    /// bu, barındırma sağlayıcısının SMTP çıkışını engellediği ortamlarda güvenilir teslim sağlar.
    /// </summary>
    public string Provider { get; init; } = "Smtp";
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 465;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Kombi Klima Burada";
    public bool UseSslOnConnect { get; init; } = true;
    /// <summary>
    /// Only enable this when SMTP is reached through a private, trusted network
    /// (for example the internal Docker network used by the local Stalwart service).
    /// </summary>
    public bool AllowInvalidCertificate { get; init; }
    public int TimeoutSeconds { get; init; } = 15;

    /// <summary>
    /// The authenticated mailbox is a safe default sender for self-hosted SMTP
    /// installations. A separate FromAddress may still be supplied when needed.
    /// </summary>
    public string SenderAddress => string.IsNullOrWhiteSpace(FromAddress) ? User : FromAddress;

    public bool IsConfigured => Enabled &&
                                !string.IsNullOrWhiteSpace(Host) &&
                                !string.IsNullOrWhiteSpace(User) &&
                                !string.IsNullOrWhiteSpace(Password);

    public bool UsesResendApi => string.Equals(Provider, "Resend", StringComparison.OrdinalIgnoreCase);
}
