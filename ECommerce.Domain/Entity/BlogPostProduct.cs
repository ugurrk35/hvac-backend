namespace ECommerce.Domain.Entity;

public class BlogPostProduct
{
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int SortOrder { get; set; }
}
