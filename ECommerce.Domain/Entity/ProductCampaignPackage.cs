namespace ECommerce.Domain.Entity;

/// <summary>Her üründe seçilebilen, şehir ve hizmet farklarını içeren genel montaj paketi.</summary>
public class ProductCampaignPackage : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public bool RequiresExistingDevicePhoto { get; set; }
    public int SortOrder { get; set; }
    public ICollection<ProductCampaignLocationRule> LocationRules { get; set; } = new List<ProductCampaignLocationRule>();
    public ICollection<ProductCampaignLookupGroup> LookupGroups { get; set; } = new List<ProductCampaignLookupGroup>();
}

public class ProductCampaignEvent : AuditableEntity
{
    public int ProductId { get; set; }
    public int? ProductCampaignPackageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? VisitorId { get; set; }
    public string? MetadataJson { get; set; }
}

public class ProductCampaignLocationRule : AuditableEntity
{
    public int ProductCampaignPackageId { get; set; }
    public ProductCampaignPackage ProductCampaignPackage { get; set; } = null!;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public decimal PriceAdjustment { get; set; }
    public int SortOrder { get; set; }
}

public class ProductCampaignLookupGroup : AuditableEntity
{
    public int ProductCampaignPackageId { get; set; }
    public ProductCampaignPackage ProductCampaignPackage { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public ICollection<ProductCampaignLookupOption> Options { get; set; } = new List<ProductCampaignLookupOption>();
}

public class ProductCampaignLookupOption : AuditableEntity
{
    public int ProductCampaignLookupGroupId { get; set; }
    public ProductCampaignLookupGroup ProductCampaignLookupGroup { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public decimal PriceAdjustment { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}
