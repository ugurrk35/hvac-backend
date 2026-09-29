using System.Text.Json;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public sealed class BackInStockAutomationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackInStockAutomationWorker> _logger;
    public BackInStockAutomationWorker(IServiceScopeFactory scopeFactory, ILogger<BackInStockAutomationWorker> logger) { _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await QueueEligibleSubscriptions(stoppingToken); await QueueAbandonedCarts(stoppingToken); await QueuePriceDropSubscriptions(stoppingToken); await QueuePostPurchaseCrossSell(stoppingToken); }
            catch (Exception exception) { _logger.LogError(exception, "Stok bildirim işleri kuyruğa alınamadı."); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task QueueEligibleSubscriptions(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var subscriptions = await db.BackInStockSubscriptions.Include(item => item.Product)
            .Where(item => !item.IsDeleted && !item.IsNotified && item.Product != null && item.Product.IsPublished && !item.Product.IsDeleted && item.Product.Quantity > 0)
            .Take(250).ToListAsync(cancellationToken);
        if (subscriptions.Count == 0) return;
        var keys = subscriptions.Select(item => $"back-in-stock:{item.Id}").ToList();
        var existing = await db.MarketingAutomationJobs.Where(item => keys.Contains(item.DeduplicationKey)).Select(item => item.DeduplicationKey).ToListAsync(cancellationToken);
        foreach (var subscription in subscriptions.Where(item => !existing.Contains($"back-in-stock:{item.Id}")))
            db.MarketingAutomationJobs.Add(new MarketingAutomationJob { Recipient = subscription.Email, DeduplicationKey = $"back-in-stock:{subscription.Id}", Type = MarketingAutomationType.BackInStock, Channel = MarketingChannel.Email, ScheduledAt = DateTime.UtcNow, PayloadJson = JsonSerializer.Serialize(new { subscriptionId = subscription.Id, productId = subscription.ProductId, productName = subscription.Product!.Name }) });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task QueueAbandonedCarts(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var carts = await db.ShoppingCarts.Include(item => item.User).ThenInclude(user => user!.MarketingConsent).Include(item => item.CartItems)
            .Where(item => !item.IsDeleted && !item.IsOrdered && item.UserId != null && item.User != null && item.User.Email != null && item.User.MarketingConsent != null && item.User.MarketingConsent.EmailMarketing && item.CartItems.Any() && (item.LastModifiedAt ?? item.CreatedAt) <= cutoff)
            .OrderBy(item => item.LastModifiedAt ?? item.CreatedAt).Take(250).ToListAsync(cancellationToken);
        if (carts.Count == 0) return;
        var keys = carts.Select(item => $"abandoned-cart:{item.Id}").ToList();
        var existing = await db.MarketingAutomationJobs.Where(item => keys.Contains(item.DeduplicationKey)).Select(item => item.DeduplicationKey).ToListAsync(cancellationToken);
        foreach (var cart in carts.Where(item => !existing.Contains($"abandoned-cart:{item.Id}")))
            db.MarketingAutomationJobs.Add(new MarketingAutomationJob { UserId = cart.UserId, Recipient = cart.User!.Email!, DeduplicationKey = $"abandoned-cart:{cart.Id}", Type = MarketingAutomationType.AbandonedCart, Channel = MarketingChannel.Email, ScheduledAt = DateTime.UtcNow, PayloadJson = JsonSerializer.Serialize(new { cartId = cart.Id, itemCount = cart.CartItems.Count, totalAmount = cart.TotalAmount }) });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task QueuePriceDropSubscriptions(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var subscriptions = await db.PriceDropSubscriptions.Include(item => item.Product).Include(item => item.User).ThenInclude(user => user!.MarketingConsent)
            .Where(item => !item.IsDeleted && !item.IsNotified && item.Product != null && item.Product.IsPublished && !item.Product.IsDeleted && (item.Product.DiscountPrice ?? item.Product.BasePrice) < item.ReferencePrice && item.User != null && item.User.MarketingConsent != null && item.User.MarketingConsent.EmailMarketing)
            .Take(250).ToListAsync(cancellationToken);
        if (subscriptions.Count == 0) return;
        var keys = subscriptions.Select(item => $"price-drop:{item.Id}").ToList();
        var existing = await db.MarketingAutomationJobs.Where(item => keys.Contains(item.DeduplicationKey)).Select(item => item.DeduplicationKey).ToListAsync(cancellationToken);
        foreach (var subscription in subscriptions.Where(item => !existing.Contains($"price-drop:{item.Id}")))
            db.MarketingAutomationJobs.Add(new MarketingAutomationJob { UserId = subscription.UserId, Recipient = subscription.Email, DeduplicationKey = $"price-drop:{subscription.Id}", Type = MarketingAutomationType.PriceDrop, Channel = MarketingChannel.Email, ScheduledAt = DateTime.UtcNow, PayloadJson = JsonSerializer.Serialize(new { subscriptionId = subscription.Id, productId = subscription.ProductId, oldPrice = subscription.ReferencePrice, newPrice = subscription.Product!.DiscountPrice ?? subscription.Product.BasePrice }) });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task QueuePostPurchaseCrossSell(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var orders = await db.Orders.Include(item => item.User).ThenInclude(user => user!.MarketingConsent).Include(item => item.OrdersItems)
            .Where(item => !item.IsDeleted && item.PaymentStatus == PaymentStatus.Paid && item.UserId != null && item.User != null && item.User.Email != null && item.User.MarketingConsent != null && item.User.MarketingConsent.EmailMarketing && item.CreatedAt <= cutoff)
            .OrderBy(item => item.CreatedAt).Take(250).ToListAsync(cancellationToken);
        if (orders.Count == 0) return;
        var keys = orders.Select(item => $"post-purchase-cross-sell:{item.Id}").ToList();
        var existing = await db.MarketingAutomationJobs.Where(item => keys.Contains(item.DeduplicationKey)).Select(item => item.DeduplicationKey).ToListAsync(cancellationToken);
        foreach (var order in orders.Where(item => !existing.Contains($"post-purchase-cross-sell:{item.Id}")))
            db.MarketingAutomationJobs.Add(new MarketingAutomationJob { UserId = order.UserId, Recipient = order.User!.Email!, DeduplicationKey = $"post-purchase-cross-sell:{order.Id}", Type = MarketingAutomationType.PostPurchaseCrossSell, Channel = MarketingChannel.Email, ScheduledAt = DateTime.UtcNow, PayloadJson = JsonSerializer.Serialize(new { orderId = order.Id, orderNumber = order.OrderNumber, purchasedProductIds = order.OrdersItems.Where(item => !item.IsGift).Select(item => item.ProductId).Distinct().ToList() }) });
        await db.SaveChangesAsync(cancellationToken);
    }
}
