namespace ECommerce.Domain.Entity;

/// <summary>Üyelikten bağımsız, çift onaylı e-posta bülteni aboneliği.</summary>
public class NewsletterSubscription : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public string UnsubscribeToken { get; set; } = string.Empty;
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
