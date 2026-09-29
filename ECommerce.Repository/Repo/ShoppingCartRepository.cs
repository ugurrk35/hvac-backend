using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using ECommerce.Repository.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

public class ShoppingCartRepository : GenericRepository<ShoppingCart>, IShoppingCartRepository
{
    private readonly ILogger<ShoppingCartRepository> _logger;

    public ShoppingCartRepository(ApplicationDbContext dbContext, ILogger<ShoppingCartRepository> logger)
        : base(dbContext)
    {
        _logger = logger;
    }

    private static decimal GetEffectiveUnitPrice(Product product, ProductAttributeCombination? combination)
    {
        if (combination is { Price: > 0 } && combination.ProductId == product.Id)
            return combination.Price;

        return product.DiscountPrice is > 0 and var discount && discount < product.BasePrice
            ? discount
            : product.BasePrice;
    }

    public async Task<ShoppingCart?> GetCartWithDetailsAsync(int cartId)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.AttributeSelections)
                        .ThenInclude(sel => sel.ProductAttribute)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.AttributeSelections)
                        .ThenInclude(sel => sel.ProductAttributeValue)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductAttributeCombination)
                        .ThenInclude(pac => pac.ProductAttributeCombinationValues)
                            .ThenInclude(pacv => pacv.ProductAttribute)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductAttributeCombination)
                        .ThenInclude(pac => pac.ProductAttributeCombinationValues)
                            .ThenInclude(pacv => pacv.ProductAttributeValue)
                .Include(c => c.User)
                .Include(c => c.Orders)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart with details for cartId: {CartId}", cartId);
            throw;
        }
    }

    public async Task<ShoppingCart?> GetCartByUserIdAsync(int userId)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<ShoppingCart?> GetCartByGuestIdentifierAsync(Guid guestIdentifier)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.GuestIdentifier == guestIdentifier && !c.IsOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for guestIdentifier: {GuestIdentifier}", guestIdentifier);
            throw;
        }
    }

    public async Task<bool> HasActiveCartAsync(int userId)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .AsNoTracking()
                .AnyAsync(c => c.UserId == userId && !c.IsOrdered && c.CartItems.Any());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking active cart for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetCartItemCountAsync(int cartId)
    {
        try
        {
            return await _dbContext.CartItems
                .AsNoTracking()
                .Where(ci => ci.ShoppingCartId == cartId)
                .SumAsync(ci => ci.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart item count for cartId: {CartId}", cartId);
            throw;
        }
    }

    public async Task<decimal> GetCartTotalAmountAsync(int cartId)
    {
        try
        {
            var items = await _dbContext.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.ProductAttributeCombination)
                .AsNoTracking()
                .Where(ci => ci.ShoppingCartId == cartId)
                .ToListAsync();

            return items.Sum(item => GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination) * item.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cart total for cartId: {CartId}", cartId);
            throw;
        }
    }

    public async Task<bool> IsCartValidAsync(int cartId)
    {
        try
        {
            var hasInvalidItems = await _dbContext.CartItems
                .Include(ci => ci.Product)
                .AsNoTracking()
                .Where(ci => ci.ShoppingCartId == cartId)
                .AnyAsync(ci => !ci.Product.IsActive);

            return !hasInvalidItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart for cartId: {CartId}", cartId);
            throw;
        }
    }

    public async Task<bool> IsCartEmptyAsync(int cartId)
    {
        try
        {
            return !await _dbContext.CartItems
                .AsNoTracking()
                .AnyAsync(ci => ci.ShoppingCartId == cartId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cart is empty for cartId: {CartId}", cartId);
            throw;
        }
    }

    public async Task<ShoppingCart?> GetLastModifiedCartAsync(int userId)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastModifiedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last modified cart for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task MergeGuestCartToUserAsync(Guid guestIdentifier, int userId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var guestCart = await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.GuestIdentifier == guestIdentifier && !c.IsOrdered);

            if (guestCart == null || !guestCart.CartItems.Any())
            {
                await transaction.CommitAsync();
                return;
            }

            var userCart = await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsOrdered);

            if (userCart == null)
            {
                // Convert guest cart to user cart
                guestCart.UserId = userId;
                guestCart.GuestIdentifier = null;
                guestCart.LastModifiedAt = DateTime.UtcNow;
            }
            else
            {
                // Merge items from guest cart to user cart
                foreach (var guestItem in guestCart.CartItems.ToList())
                {
                    var existingItem = userCart.CartItems
                        .FirstOrDefault(ui => ui.ProductId == guestItem.ProductId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity += guestItem.Quantity;
                        existingItem.LastModifiedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        guestItem.ShoppingCartId = userCart.Id;
                        guestItem.LastModifiedAt = DateTime.UtcNow;
                        userCart.CartItems.Add(guestItem);
                    }
                }

                userCart.LastModifiedAt = DateTime.UtcNow;
                _dbContext.ShoppingCarts.Remove(guestCart);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error merging guest cart {GuestId} to user {UserId}", guestIdentifier, userId);
            throw;
        }
    }

    public async Task<ShoppingCart?> GetActiveCartByUserIdAsync(int userId)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active cart for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<ShoppingCart?> GetCartByGuestIdAsync(Guid guestIdentifier)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.GuestIdentifier == guestIdentifier && !c.IsOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for guestId: {GuestIdentifier}", guestIdentifier);
            throw;
        }
    }

    public async Task<bool> IsProductInCartAsync(int cartId, int productId)
    {
        try
        {
            return await _dbContext.CartItems
                .AsNoTracking()
                .AnyAsync(ci => ci.ShoppingCartId == cartId && ci.ProductId == productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product {ProductId} is in cart {CartId}", productId, cartId);
            throw;
        }
    }

    public async Task<CartItem?> GetCartItemByIdAsync(int cartId, int cartItemId)
    {
        try
        {
            return await _dbContext.CartItems
                .Include(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.ShoppingCartId == cartId && ci.Id == cartItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart item {CartItemId} for cart {CartId}", cartItemId, cartId);
            throw;
        }
    }

    public async Task<IReadOnlyList<CartItem>> GetCartItemsAsync(int cartId)
    {
        try
        {
            return await _dbContext.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.AttributeSelections)
                    .ThenInclude(sel => sel.ProductAttribute)
                .Include(ci => ci.AttributeSelections)
                    .ThenInclude(sel => sel.ProductAttributeValue)
                .AsNoTracking()
                .Where(ci => ci.ShoppingCartId == cartId)
                .OrderBy(ci => ci.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart items for cartId: {CartId}", cartId);
            throw;
        }
    }

    public async Task AddItemToCartAsync(int cartId, CartItem item)
    {
        try
        {
            if (cartId <= 0)
                throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.ProductId <= 0)
                throw new ArgumentException("Product ID must be greater than zero.");

            if (item.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            // Ürün bilgilerini çek
            var product = await _dbContext.Products.FindAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product {item.ProductId} not found.");

            ProductAttributeCombination? selectedCombination = null;
            if (item.ProductAttributeCombinationId.HasValue)
            {
                selectedCombination = await _dbContext.ProductAttributeCombinations
                    .FirstOrDefaultAsync(pac => pac.Id == item.ProductAttributeCombinationId.Value &&
                        pac.ProductId == item.ProductId && pac.IsActive && !pac.IsDeleted);

                if (selectedCombination == null)
                    throw new InvalidOperationException($"Product attribute combination {item.ProductAttributeCombinationId.Value} is not valid for product {item.ProductId}.");
            }

            if (item.AttributeSelections != null && item.AttributeSelections.Any())
            {
                foreach (var selection in item.AttributeSelections)
                {
                    var attributeExists = await _dbContext.ProductAttributes
                        .AsNoTracking()
                        .AnyAsync(attr => attr.Id == selection.ProductAttributeId);

                    if (!attributeExists)
                        throw new InvalidOperationException($"Product attribute {selection.ProductAttributeId} not found.");

                    if (selection.ProductAttributeValueId.HasValue)
                    {
                        var value = await _dbContext.ProductAttributeValues
                            .AsNoTracking()
                            .FirstOrDefaultAsync(val => val.Id == selection.ProductAttributeValueId.Value);

                        if (value == null || value.ProductAttributeId != selection.ProductAttributeId)
                            throw new InvalidOperationException($"Product attribute value {selection.ProductAttributeValueId} is not valid for attribute {selection.ProductAttributeId}.");
                    }

                    var combinationId = selection.ProductAttributeCombinationId ?? item.ProductAttributeCombinationId;
                    if (combinationId.HasValue)
                    {
                        var combinationValueQuery = _dbContext.ProductAttributeCombinationValues
                            .AsNoTracking()
                            .Where(cv => cv.ProductAttributeCombinationId == combinationId.Value && cv.ProductAttributeId == selection.ProductAttributeId);

                        if (selection.ProductAttributeValueId.HasValue)
                        {
                            combinationValueQuery = combinationValueQuery.Where(cv => cv.ProductAttributeValueId == selection.ProductAttributeValueId.Value);
                        }

                        var combinationValue = await combinationValueQuery.FirstOrDefaultAsync();

                        if (combinationValue == null)
                            throw new InvalidOperationException($"Selected attribute option is not valid for combination {combinationId.Value}.");

                        selection.ProductAttributeCombinationId = combinationId.Value;
                        selection.ProductAttributeCombinationValueId = combinationValue.Id;

                        if (string.IsNullOrWhiteSpace(selection.PersonalizationText) && !string.IsNullOrWhiteSpace(combinationValue.PersonalizationText))
                        {
                            selection.PersonalizationText = combinationValue.PersonalizationText.Trim();
                        }
                    }

                    selection.PersonalizationText = string.IsNullOrWhiteSpace(selection.PersonalizationText)
                        ? null
                        : selection.PersonalizationText.Trim();

                    selection.CreatedAt = DateTime.UtcNow;
                    selection.LastModifiedAt = DateTime.UtcNow;
                }
            }

            // CartItem bilgileri
            item.ShoppingCartId = cartId;
            item.CreatedAt = DateTime.UtcNow;
            item.LastModifiedAt = DateTime.UtcNow;
            item.PersonalizationText = string.IsNullOrWhiteSpace(item.PersonalizationText)
                ? null
                : item.PersonalizationText.Trim();

            await _dbContext.CartItems.AddAsync(item);

            // Sepeti güncelle (toplam fiyat)
            var cart = await _dbContext.ShoppingCarts.FindAsync(cartId);
            if (cart == null)
                throw new InvalidOperationException($"Shopping cart {cartId} not found.");

            cart.LastModifiedAt = DateTime.UtcNow;
            var effectiveUnitPrice = GetEffectiveUnitPrice(product, selectedCombination);
            cart.TotalAmount += effectiveUnitPrice * item.Quantity; // Toplam fiyatı güncelle

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Added product {ProductId} to cart {CartId}, total updated to {Total}",
                item.ProductId, cartId, cart.TotalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart {CartId}", cartId);
            throw;
        }
    }


    public async Task RemoveItemFromCartAsync(int cartId, int cartItemId)
    {
        try
        {
            var item = await _dbContext.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.ProductAttributeCombination)
                .FirstOrDefaultAsync(ci => ci.ShoppingCartId == cartId && ci.Id == cartItemId);

            if (item != null)
            {
                var productPrice = item.Product == null ? 0m : GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination);
                var totalDelta = productPrice * item.Quantity;

                _dbContext.CartItems.Remove(item);

                var cart = await _dbContext.ShoppingCarts.FindAsync(cartId);
                if (cart != null)
                {
                    cart.LastModifiedAt = DateTime.UtcNow;
                    cart.TotalAmount = Math.Max(0m, cart.TotalAmount - totalDelta);
                }

                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart {CartId}", cartId);
            throw;
        }
    }

    public async Task UpdateCartItemQuantityAsync(int cartId, int cartItemId, int newQuantity)
    {
        try
        {
            var item = await _dbContext.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.ProductAttributeCombination)
                .FirstOrDefaultAsync(ci => ci.ShoppingCartId == cartId && ci.Id == cartItemId);

            if (item != null)
            {
                var oldQuantity = item.Quantity;
                item.Quantity = newQuantity;
                item.LastModifiedAt = DateTime.UtcNow;

                var cart = await _dbContext.ShoppingCarts.FindAsync(cartId);
                if (cart != null)
                {
                    cart.LastModifiedAt = DateTime.UtcNow;
                    var unitPrice = item.Product == null ? 0m : GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination);
                    var deltaQuantity = newQuantity - oldQuantity;
                    cart.TotalAmount = Math.Max(0m, cart.TotalAmount + (unitPrice * deltaQuantity));
                }

                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item quantity for cart {CartId}", cartId);
            throw;
        }
    }

    public async Task ClearCartAsync(int cartId)
    {
        try
        {
            var items = await _dbContext.CartItems
            .Where(ci => ci.ShoppingCartId == cartId)
                .ToListAsync();

            var cart = await _dbContext.ShoppingCarts.FindAsync(cartId);
            if (items.Any())
            {
                _dbContext.CartItems.RemoveRange(items);
            }

            if (cart != null)
            {
                cart.LastModifiedAt = DateTime.UtcNow;
                cart.TotalAmount = 0m;
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart {CartId}", cartId);
            throw;
        }
    }

    public async Task<decimal> CalculateCartTotalAsync(int cartId)
    {
        try
        {
            var items = await _dbContext.CartItems
                .AsNoTracking()
                .Include(ci => ci.Product)
                .Include(ci => ci.ProductAttributeCombination)
                .Where(ci => ci.ShoppingCartId == cartId)
                .ToListAsync();

            return items.Sum(item => GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination) * item.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cart total for cartId: {CartId}", cartId);
            throw;
        }
    }


    public async Task<bool> AreAllItemsInStockAsync(int cartId)
    {
        try
        {
            var outOfStockItems = await _dbContext.CartItems
                .Include(ci => ci.Product)
                .AsNoTracking()
                .Where(ci => ci.ShoppingCartId == cartId)
                .AnyAsync(ci => ci.Product.Quantity < ci.Quantity);

            return !outOfStockItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock for cart {CartId}", cartId);
            throw;
        }
    }

    public async Task<ShoppingCart?> GetCartBySessionIdAsync(int sessionId)
    {
        try
        {
            return await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && !c.IsOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for sessionId: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<IReadOnlyList<CartItemDto>> GetCartItemsWithProductInfoAsync(int cartId)
    {
        try
        {
            var cartItems = await _dbContext.CartItems
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.ProductImages)
                        .ThenInclude(pi => pi.Image)
                .Include(ci => ci.AttributeSelections)
                    .ThenInclude(sel => sel.ProductAttribute)
                .Include(ci => ci.AttributeSelections)
                    .ThenInclude(sel => sel.ProductAttributeValue)
                .Include(ci => ci.AttributeSelections)
                    .ThenInclude(sel => sel.ProductAttributeCombination)
                .Include(ci => ci.ProductAttributeCombination)
                    .ThenInclude(pac => pac.ProductAttributeCombinationValues)
                        .ThenInclude(pacv => pacv.ProductAttribute)
                .Include(ci => ci.ProductAttributeCombination)
                    .ThenInclude(pac => pac.ProductAttributeCombinationValues)
                        .ThenInclude(pacv => pacv.ProductAttributeValue)
                .AsNoTracking()
                .Where(ci => ci.ShoppingCartId == cartId)
                .OrderBy(ci => ci.Product.Name)
                .ToListAsync();

            return cartItems.Select(ci =>
            {
                var imageUrl = ci.Product?.ProductImages
                    ?.OrderBy(pi => pi.SortOrder)
                    .Select(pi => pi.Image?.Url)
                    .FirstOrDefault();

                var attributes = new List<CartItemAttributeDto>();

                var attributeSelections = ci.AttributeSelections?.OrderBy(sel => sel.ProductAttribute?.Name ?? string.Empty)
                    .ThenBy(sel => sel.ProductAttributeValue?.Value ?? string.Empty)
                    .ThenBy(sel => sel.Id)
                    .ToList();

                if (attributeSelections != null && attributeSelections.Any())
                {
                    foreach (var selection in attributeSelections)
                    {
                        attributes.Add(new CartItemAttributeDto
                        {
                            AttributeId = selection.ProductAttributeId,
                            AttributeName = selection.ProductAttribute?.Name ?? string.Empty,
                            AttributeValueId = selection.ProductAttributeValueId,
                            AttributeValue = selection.ProductAttributeValue?.Value,
                            PersonalizationText = selection.PersonalizationText
                        });
                    }
                }
                else
                {
                    var combinationValues = ci.ProductAttributeCombination?.ProductAttributeCombinationValues;
                    if (combinationValues != null)
                    {
                        var selectedValues = combinationValues
                            .Where(v => v != null)
                            .GroupBy(v => v.ProductAttributeId)
                            .SelectMany(group =>
                            {
                                var ordered = group
                                    .OrderByDescending(v => v.ProductAttributeValueId.HasValue ? 1 : 0)
                                    .ThenByDescending(v => v.LastModifiedAt ?? v.CreatedAt)
                                    .ToList();

                                var results = new List<ProductAttributeCombinationValue>();

                                var primary = ordered
                                    .FirstOrDefault(v => v.ProductAttributeValueId.HasValue)
                                    ?? ordered.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.PersonalizationText))
                                    ?? ordered.FirstOrDefault();

                                if (primary != null)
                                {
                                    results.Add(primary);
                                }

                                var personalizationExtras = ordered
                                    .Where(v => v.Id != primary?.Id && !string.IsNullOrWhiteSpace(v.PersonalizationText))
                                    .GroupBy(v => (v.PersonalizationText ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                                    .Select(g => g.OrderByDescending(v => v.LastModifiedAt ?? v.CreatedAt).First());

                                results.AddRange(personalizationExtras);

                                return results;
                            })
                            .OrderBy(v => v.ProductAttribute != null ? v.ProductAttribute.Name : string.Empty)
                            .ThenBy(v => v.ProductAttributeValueId ?? int.MaxValue)
                            .ThenBy(v => v.Id);

                        foreach (var value in selectedValues)
                        {
                            attributes.Add(new CartItemAttributeDto
                            {
                                AttributeId = value.ProductAttributeId,
                                AttributeName = value.ProductAttribute != null ? value.ProductAttribute.Name : string.Empty,
                                AttributeValueId = value.ProductAttributeValueId,
                                AttributeValue = value.ProductAttributeValue != null ? value.ProductAttributeValue.Value : null,
                                PersonalizationText = value.PersonalizationText
                            });
                        }
                    }
                }

                var hasVariantPrice = ci.ProductAttributeCombination is { Price: > 0 } combination &&
                    combination.ProductId == ci.ProductId;
                var basePrice = hasVariantPrice ? ci.ProductAttributeCombination!.Price : ci.Product?.BasePrice ?? 0m;
                var discountPrice = !hasVariantPrice && ci.Product?.DiscountPrice is > 0 and var discount && discount < basePrice
                    ? discount
                    : (decimal?)null;
                var unitPrice = discountPrice ?? basePrice;

                return new CartItemDto
                {
                    CartItemId = ci.Id,
                    ProductId = ci.ProductId,
                    ProductAttributeCombinationId = ci.ProductAttributeCombinationId,
                    ProductName = ci.Product?.Name ?? string.Empty,
                    Quantity = ci.Quantity,

                    // 👇 Burası önemli
                    Price = unitPrice,
                    BasePrice = basePrice,
                    DiscountPrice = discountPrice,
                    EffectivePrice = unitPrice,
                    TotalPrice = unitPrice * ci.Quantity,

                    PersonalizationText = ci.PersonalizationText,
                    ImageUrl = imageUrl,
                    Attributes = attributes
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart items with product info for cartId: {CartId}", cartId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DiscountResult>> ApplyCartCampaignsAsync(int cartId)
    {
        try
        {
            var cartItems = await _dbContext.CartItems
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Category)
                .AsNoTracking()
                .Where(ci => ci.ShoppingCartId == cartId)
                .ToListAsync();

            var discounts = new List<DiscountResult>();

            foreach (var item in cartItems)
            {
                var itemTotal = item.Product.BasePrice * item.Quantity;
                decimal discountAmount = 0;
                string campaignName = "";
                string description = "";

                // Category-based discount
                if (item.Product.Category?.Name == "Electronics")
                {
                    discountAmount = itemTotal * 0.15m;
                    campaignName = "Electronics %15 İndirim";
                    description = "Elektronik ürünlerinde %15 indirim";
                }
                // Quantity-based discount
                else if (item.Quantity >= 3)
                {
                    discountAmount = itemTotal * 0.10m;
                    campaignName = "3 Al 2 Öde";
                    description = "3 ve üzeri alışverişlerde %10 indirim";
                }
                // Default discount
                else
                {
                    discountAmount = itemTotal * 0.05m;
                    campaignName = "Genel %5 İndirim";
                    description = "Tüm ürünlerde %5 indirim";
                }

                discounts.Add(new DiscountResult
                {
                    ProductId = item.ProductId,
                    DiscountAmount = discountAmount,
                    CampaignName = campaignName,
                    Description = description
                });
            }

            return discounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying campaigns for cart {CartId}", cartId);
            throw;
        }
    }

    // Additional helper method for getting cart summary
    public async Task<CartSummaryDto> GetCartSummaryAsync(int cartId)
    {
        try
        {
            var cart = await _dbContext.ShoppingCarts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductAttributeCombination)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cartId);

            if (cart == null)
                return new CartSummaryDto();

            return new CartSummaryDto
            {
                CartId = cartId,
                ItemCount = cart.CartItems.Sum(ci => ci.Quantity),
                TotalAmount = cart.CartItems.Sum(ci => GetEffectiveUnitPrice(ci.Product, ci.ProductAttributeCombination) * ci.Quantity),
                IsEmpty = !cart.CartItems.Any(),
                LastModified = cart.LastModifiedAt,
                HasOutOfStockItems = cart.CartItems.Any(ci => ci.Product.Quantity < ci.Quantity)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart summary for cartId: {CartId}", cartId);
            throw;
        }
    }
    public async Task<ShoppingCart?> GetCartWithItems(int cartId)
    {
        var cart = await _dbContext.ShoppingCarts
               .Include(c => c.CartItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product.Category)
               .Include(c => c.CartItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product.ProductProductTags)
                .Include(c => c.CartItems)
                    .ThenInclude(item => item.Product)
                        .ThenInclude(product => product.ProductImages)
                            .ThenInclude(productImage => productImage.Image)
               .Include(c => c.CartItems)
                    .ThenInclude(item => item.ProductAttributeCombination)
               .Include(c => c.CartItems)
                    .ThenInclude(item => item.AttributeSelections)
                        .ThenInclude(selection => selection.ProductAttribute)
               .Include(c => c.CartItems)
                    .ThenInclude(item => item.AttributeSelections)
                        .ThenInclude(selection => selection.ProductAttributeValue)
                .FirstOrDefaultAsync(c => c.Id == cartId);

        return cart;

    }

    public async Task<IReadOnlyList<ShoppingCart>> GetAllCartsAsync()
    {
        return await _dbContext.ShoppingCarts
    .AsSplitQuery()
    .Include(c => c.CartItems)
    .Include(c => c.User)
    .Include(c => c.Orders)
    .Select(c => new ShoppingCart
    {
        Id = c.Id,
        CreatedAt = c.CreatedAt,
        User = c.User,
        CartItems = c.CartItems,
        TotalAmount = c.TotalAmount,
        Orders = c.Orders.Select(o => new Order
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            TotalAmount = o.TotalAmount
        }).ToList()
    })
    .OrderByDescending(c => c.CreatedAt)
    .ToListAsync();

    }


    public async Task<ShoppingCart?> GetByIdAsync(int id)
    {
        return await _dbContext.ShoppingCarts
            .Include(c => c.CartItems)
            .Include(c => c.User)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task DeleteAsync(int id)
    {
        var cart = await _dbContext.ShoppingCarts.FindAsync(id);
        if (cart != null)
        {
            _dbContext.ShoppingCarts.Remove(cart);
            await _dbContext.SaveChangesAsync();
        }
    }
}

// Supporting DTOs
public class CartSummaryDto
{
    public int CartId { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsEmpty { get; set; }
    public DateTime? LastModified { get; set; }
    public bool HasOutOfStockItems { get; set; }
}
