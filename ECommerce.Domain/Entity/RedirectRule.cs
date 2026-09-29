namespace ECommerce.Domain.Entity;
public class RedirectRule : AuditableEntity
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 301;
}
