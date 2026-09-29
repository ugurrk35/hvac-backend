namespace ECommerce.Domain.Entity;

/// <summary>Tek kullanımlık üyelik ve pazarlama izin bağlantılarını saklar.</summary>
public class EmailConfirmationToken : BaseEntity
{
    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
