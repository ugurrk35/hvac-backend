using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService,
            IDateTime dateTime) : base(options)
        {
            _currentUserService = currentUserService;
            _dateTime = dateTime;
        }
        public virtual DbSet<BlogCategory> BlogCategories { get; set; }

        public virtual DbSet<BlogPost> BlogPosts { get; set; }
        public virtual DbSet<BlogPostProduct> BlogPostProducts { get; set; }
        public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }


        public virtual DbSet<BlogPostImage> BlogPostImages { get; set; }

        public virtual DbSet<BlogPostTag> BlogPostTags { get; set; }
        public virtual DbSet<BlogPostComment> BlogPostComments { get; set; }

        public virtual DbSet<ProductPrice>  ProductPrices { get; set; }
        public virtual DbSet<CustomerGroup>  CustomerGroups { get; set; }


        
        public virtual DbSet<BlogTag> BlogTags { get; set; }

        public virtual DbSet<SeoFriendlyImage> SeoFriendlyImages { get; set; }

        public virtual DbSet<SeoMetadata> SeoMetadata { get; set; }
        public virtual DbSet<ProductReview> ProductReviews { get; set; }
        public virtual DbSet<ProductReviewPhoto> ProductReviewPhotos { get; set; }
        public virtual DbSet<ProductQuestion> ProductQuestions { get; set; }
        public virtual DbSet<ProductTag> ProductTags { get; set; }
        public virtual DbSet<ProductProductTag> ProductProductTags { get; set; }
        public virtual DbSet<ApplicationUser> Users { get; set; }
        public virtual DbSet<ApplicationRole> Roles { get; set; }

        public virtual DbSet<Session> Sessions { get; set; }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Image> Images { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<OrderNote> OrderNotes { get; set; }
        public virtual DbSet<OrderRefund> OrderRefunds { get; set; }
        public virtual DbSet<OrderReturnRequest> OrderReturnRequests { get; set; }
        public virtual DbSet<OrderReturnRequestItem> OrderReturnRequestItems { get; set; }
        public virtual DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public virtual DbSet<CustomerFavorite> CustomerFavorites { get; set; }
        public virtual DbSet<BackInStockSubscription> BackInStockSubscriptions { get; set; }
        public virtual DbSet<CustomerMarketingConsent> CustomerMarketingConsents { get; set; }
        public virtual DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; }
        public virtual DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }
        public virtual DbSet<MarketingAutomationJob> MarketingAutomationJobs { get; set; }
        public virtual DbSet<PriceDropSubscription> PriceDropSubscriptions { get; set; }
        public virtual DbSet<OrderAttribution> OrderAttributions { get; set; }
        public virtual DbSet<AdvertisingSpend> AdvertisingSpends { get; set; }
        public virtual DbSet<WebVitalMetric> WebVitalMetrics { get; set; }
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductRelated> ProductRelateds { get; set; }

        public virtual DbSet<ProductAttribute> ProductAttributes { get; set; }
        public virtual DbSet<ProductAttributeCombination> ProductAttributeCombinations { get; set; }
        public virtual DbSet<ProductAttributeCombinationValue> ProductAttributeCombinationValues { get; set; }
        public virtual DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
        public virtual DbSet<ProductImage> ProductImages { get; set; }

        public virtual DbSet<ShippingMethod> ShippingMethods { get; set; }
        public virtual DbSet<ShippingMethodRateOverride> ShippingMethodRateOverrides { get; set; }
        public virtual DbSet<ShippingCheckoutSettings> ShippingCheckoutSettings { get; set; }
        public virtual DbSet<Campaign> Campaigns { get; set; }
        public virtual DbSet<ProductCampaignPackage> ProductCampaignPackages { get; set; }
        public virtual DbSet<ProductCampaignLocationRule> ProductCampaignLocationRules { get; set; }
        public virtual DbSet<ProductCampaignLookupGroup> ProductCampaignLookupGroups { get; set; }
        public virtual DbSet<ProductCampaignLookupOption> ProductCampaignLookupOptions { get; set; }
        public virtual DbSet<ProductCampaignEvent> ProductCampaignEvents { get; set; }

        public virtual DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }
        public virtual DbSet<CartItemAttributeSelection> CartItemAttributeSelections { get; set; }

        public virtual DbSet<MarketingIntegrationSettings> MarketingIntegrationSettings { get; set; }
        public virtual DbSet<HomeBestsellersSettings> HomeBestsellersSettings { get; set; }
        public virtual DbSet<HomeFeaturedSettings> HomeFeaturedSettings { get; set; }
        public virtual DbSet<WhatsAppSettings> WhatsAppSettings { get; set; }
        public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }
        public virtual DbSet<FooterSettings> FooterSettings { get; set; }
        public virtual DbSet<ProductDetailSettings> ProductDetailSettings { get; set; }
        public virtual DbSet<CmsPage> CmsPages { get; set; }
        public virtual DbSet<ContactMessage> ContactMessages { get; set; }
        public virtual DbSet<DomainRoute> DomainRoutes { get; set; }
        public virtual DbSet<RedirectRule> RedirectRules { get; set; }


        
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedBy = _currentUserService.UserId;
                        entry.Entity.CreatedAt = _dateTime.Now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedBy = _currentUserService.UserId;
                        entry.Entity.LastModifiedAt = _dateTime.Now;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
     //   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
     //=> optionsBuilder.UseSqlServer("Server=DESKTOP-RCK0R5G\\SQLEXPRESS; Database=commerceLast; Trusted_Connection=true;Integrated Security = True; MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;");

        //protected override void OnModelCreating(ModelBuilder builder)
        //{
        //    builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        //    base.OnModelCreating(builder);
        //}
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            builder.Entity<Session>()
    .HasOne(s => s.User)
    .WithMany(u => u.Sessions)
    .HasForeignKey(s => s.UserId)
    .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Product>()
    .Property(p => p.RowVersion)
    .IsConcurrencyToken()
    .ValueGeneratedNever();

            builder.Entity<Order>(entity =>
            {
                entity.Property(item => item.ArchiveReason).HasMaxLength(1000);
                entity.Property(item => item.PaytrMerchantOid).HasMaxLength(64);
                entity.HasIndex(item => new { item.IsDeleted, item.ArchivedAt });
                entity.HasIndex(item => item.PaytrMerchantOid).IsUnique();
            });
            builder.Entity<OrderStatusHistory>(entity => entity.Property(item => item.EventType).HasMaxLength(32).HasDefaultValue("status"));
            builder.Entity<ProductDetailSettings>()
                .HasIndex(settings => settings.CategoryId)
                .IsUnique();
            builder.Entity<OrderRefund>(entity =>
            {
                entity.Property(item => item.IdempotencyKey).HasMaxLength(100);
                entity.HasIndex(item => item.IdempotencyKey).IsUnique();
            });
            builder.Entity<OrderNote>(entity =>
            {
                entity.Property(item => item.Content).IsRequired().HasMaxLength(2000);
                entity.HasIndex(item => new { item.OrderId, item.CreatedAt });
                entity.HasOne(item => item.Order).WithMany().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            // ProductReview yapılandırması
            builder.Entity<ProductReview>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ReviewerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ReviewerEmail)
                    .HasMaxLength(150);

                entity.Property(e => e.Title)
                    .HasMaxLength(150);

                entity.Property(e => e.Content)
                    .IsRequired();

                entity.Property(e => e.Rating)
                    .IsRequired();

                entity.Property(e => e.ReviewDate)
        .HasDefaultValueSql("now()");

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ProductReviewPhoto>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasOne(p => p.ProductReview)
                      .WithMany(r => r.Photos)
                      .HasForeignKey(p => p.ProductReviewId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Image)
                      .WithMany()
                      .HasForeignKey(p => p.ImageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ProductQuestion configuration
            builder.Entity<ProductQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AuthorName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.AuthorEmail)
                    .HasMaxLength(150);

                entity.Property(e => e.QuestionText)
                    .IsRequired();

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ProductTag yapılandırması
            builder.Entity<ProductTag>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Slug)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(e => e.Slug)
                    .IsUnique();
            });

            // ProductProductTag yapılandırması
            builder.Entity<ProductProductTag>(entity =>
            {
                entity.HasKey(e => new { e.ProductId, e.ProductTagId });

                entity.HasOne(pt => pt.Product)
                    .WithMany(p => p.ProductProductTags)
                    .HasForeignKey(pt => pt.ProductId);

                entity.HasOne(pt => pt.ProductTag)
                    .WithMany(t => t.ProductProductTags)
                    .HasForeignKey(pt => pt.ProductTagId);
            });

            // ProductRelated (self-referencing many-to-many) yapılandırması
            builder.Entity<ProductRelated>(entity =>
            {
                entity.HasKey(e => new { e.ProductId, e.RelatedProductId });

                entity.HasOne(e => e.Product)
                      .WithMany(p => p.RelatedProducts)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RelatedProduct)
                      .WithMany(p => p.RelatedByProducts)
                      .HasForeignKey(e => e.RelatedProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.SortOrder).HasDefaultValue(0);
            });

            builder.Entity<CartItem>()
                .HasMany(ci => ci.AttributeSelections)
                .WithOne(sel => sel.CartItem)
                .HasForeignKey(sel => sel.CartItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductCampaignPackage>(entity =>
            {
                entity.Property(item => item.Title).HasMaxLength(200).IsRequired();
                entity.Property(item => item.Description).HasMaxLength(1000);
                entity.Property(item => item.StartingPrice).HasPrecision(18, 2);
                entity.HasIndex(item => new { item.ProductId, item.IsDeleted });
                entity.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<ProductCampaignEvent>(entity =>
            {
                entity.Property(item => item.EventType).HasMaxLength(64).IsRequired();
                entity.Property(item => item.VisitorId).HasMaxLength(100);
                entity.Property(item => item.MetadataJson).HasColumnType("text");
                entity.HasIndex(item => new { item.ProductCampaignPackageId, item.EventType, item.CreatedAt });
            });
            builder.Entity<ProductCampaignLocationRule>(entity =>
            {
                entity.Property(item => item.City).HasMaxLength(100).IsRequired();
                entity.Property(item => item.District).HasMaxLength(100);
                entity.Property(item => item.PriceAdjustment).HasPrecision(18, 2);
                entity.HasIndex(item => new { item.ProductCampaignPackageId, item.City, item.District });
                entity.HasOne(item => item.ProductCampaignPackage).WithMany(item => item.LocationRules).HasForeignKey(item => item.ProductCampaignPackageId).OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<ProductCampaignLookupGroup>(entity =>
            {
                entity.Property(item => item.Code).HasMaxLength(80).IsRequired();
                entity.Property(item => item.Label).HasMaxLength(200).IsRequired();
                entity.HasIndex(item => new { item.ProductCampaignPackageId, item.Code }).IsUnique();
                entity.HasOne(item => item.ProductCampaignPackage).WithMany(item => item.LookupGroups).HasForeignKey(item => item.ProductCampaignPackageId).OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<ProductCampaignLookupOption>(entity =>
            {
                entity.Property(item => item.Label).HasMaxLength(250).IsRequired();
                entity.Property(item => item.PriceAdjustment).HasPrecision(18, 2);
                entity.HasOne(item => item.ProductCampaignLookupGroup).WithMany(item => item.Options).HasForeignKey(item => item.ProductCampaignLookupGroupId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CustomerAddress>(entity =>
            {
                entity.Property(item => item.Title).HasMaxLength(80).IsRequired();
                entity.Property(item => item.RecipientName).HasMaxLength(160).IsRequired();
                entity.Property(item => item.City).HasMaxLength(100).IsRequired();
                entity.Property(item => item.District).HasMaxLength(100).IsRequired();
                entity.Property(item => item.AddressLine).HasMaxLength(1000).IsRequired();
                entity.HasOne(item => item.User).WithMany(user => user.CustomerAddresses).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(item => new { item.UserId, item.IsDefault });
            });

            builder.Entity<OrderReturnRequestItem>(entity =>
            {
                entity.HasOne(item => item.OrderReturnRequest).WithMany(request => request.Items).HasForeignKey(item => item.OrderReturnRequestId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.OrderItem).WithMany().HasForeignKey(item => item.OrderItemId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CustomerFavorite>(entity =>
            {
                entity.HasIndex(item => new { item.UserId, item.ProductId }).IsUnique();
                entity.HasOne(item => item.User).WithMany(user => user.Favorites).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.Product).WithMany(product => product.Favorites).HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<BackInStockSubscription>(entity =>
            {
                entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
                entity.HasIndex(item => new { item.ProductId, item.Email }).IsUnique();
                entity.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CustomerMarketingConsent>(entity =>
            {
                entity.HasIndex(item => item.UserId).IsUnique();
                entity.HasOne(item => item.User).WithOne(user => user.MarketingConsent).HasForeignKey<CustomerMarketingConsent>(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<EmailConfirmationToken>(entity =>
            {
                entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
                entity.Property(item => item.Purpose).HasMaxLength(64).IsRequired();
                entity.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
                entity.HasIndex(item => new { item.Purpose, item.TokenHash }).IsUnique();
                entity.HasIndex(item => new { item.Email, item.Purpose, item.UsedAt });
                entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<NewsletterSubscription>(entity =>
            {
                entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
                entity.Property(item => item.UnsubscribeToken).HasMaxLength(128).IsRequired();
                entity.HasIndex(item => item.Email).IsUnique();
                entity.HasIndex(item => item.UnsubscribeToken).IsUnique();
            });

            builder.Entity<BlogPostProduct>(entity =>
            {
                entity.HasKey(item => new { item.BlogPostId, item.ProductId });
                entity.HasOne(item => item.BlogPost).WithMany(item => item.RelatedProducts).HasForeignKey(item => item.BlogPostId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<RedirectRule>(entity => { entity.Property(item => item.SourcePath).HasMaxLength(500).IsRequired(); entity.Property(item => item.TargetPath).HasMaxLength(500).IsRequired(); entity.HasIndex(item => item.SourcePath).IsUnique(); });

            builder.Entity<MarketingAutomationJob>(entity =>
            {
                entity.Property(item => item.Recipient).HasMaxLength(320).IsRequired();
                entity.HasIndex(item => item.DeduplicationKey).IsUnique();
                entity.Property(item => item.PayloadJson).HasColumnType("text").IsRequired();
                entity.HasIndex(item => new { item.Status, item.ScheduledAt });
                entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<PriceDropSubscription>(entity =>
            {
                entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
                entity.HasIndex(item => new { item.ProductId, item.Email }).IsUnique();
                entity.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<OrderAttribution>(entity =>
            {
                entity.HasIndex(item => item.OrderId).IsUnique();
                entity.HasOne(item => item.Order).WithMany().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AdvertisingSpend>(entity =>
            {
                entity.Property(item => item.Platform).HasMaxLength(60).IsRequired();
                entity.Property(item => item.Campaign).HasMaxLength(200);
                entity.Property(item => item.Currency).HasMaxLength(8).IsRequired();
                entity.Property(item => item.Amount).HasPrecision(18, 2);
                entity.HasIndex(item => new { item.Platform, item.Campaign, item.SpendDate });
            });

            builder.Entity<WebVitalMetric>(entity =>
            {
                entity.Property(item => item.Name).HasMaxLength(20).IsRequired();
                entity.Property(item => item.Path).HasMaxLength(500);
                entity.Property(item => item.VisitorId).HasMaxLength(120);
                entity.HasIndex(item => new { item.Name, item.CreatedAt });
            });

            // CMS pages
            builder.Entity<CmsPage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Slug).IsRequired();
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.Property(e => e.ContentHtml).HasColumnType("text");
            });

            // Contact messages
            builder.Entity<ContactMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Subject).HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsResolved);
            });

            // Domain routes
            builder.Entity<DomainRoute>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Slug).IsRequired();
                entity.Property(e => e.EntityType).IsRequired();
                entity.HasIndex(e => e.Slug).IsUnique();
            });
            builder.Entity<EmailTemplate>(entity =>
            {
                entity.Property(item => item.TemplateKey).HasMaxLength(80).IsRequired();
                entity.Property(item => item.Subject).HasMaxLength(250).IsRequired();
                entity.Property(item => item.HtmlBody).HasColumnType("text").IsRequired();
                entity.HasIndex(item => item.TemplateKey).IsUnique();
            });
        }

    }
}
