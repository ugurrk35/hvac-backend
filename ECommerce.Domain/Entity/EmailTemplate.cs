namespace ECommerce.Domain.Entity;

public class EmailTemplate : BaseEntity
{
    public string TemplateKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
