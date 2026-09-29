using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Dtos;
using ECommerce.Service.Dtos.UserDtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class ShoppingCartService : Service<ShoppingCart>, IShoppingCartService
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShoppingCartService> _logger;
    private readonly IPriceQuoteService _priceQuoteService;

    public ShoppingCartService(
        IShoppingCartRepository shoppingCartRepository,
        IUnitOfWork unitOfWork,
        ILogger<ShoppingCartService> logger,
        IPriceQuoteService priceQuoteService) 
        : base(shoppingCartRepository, unitOfWork)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _priceQuoteService = priceQuoteService;
    }
        public async Task<ShoppingCart?> GetCartWithDetailsAsync(int cartId)
        {
            if (cartId <= 0)
                throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

            return await _shoppingCartRepository.GetCartWithDetailsAsync(cartId);
        }
        public async Task<ShoppingCartDto?> GetCartWithDetailsDtoAsync(int cartId)
        {
            if (cartId <= 0)
                throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

            var cart = await _shoppingCartRepository.GetCartWithDetailsAsync(cartId);

            if (cart == null) return null;

            decimal GetUnitPrice(CartItem item) => item.Product == null
                ? 0m
                : _priceQuoteService.GetEffectiveUnitPrice(item.Product, item.ProductAttributeCombination);

            return new ShoppingCartDto
            {
                Id = cart.Id,
                TotalAmount = cart.CartItems.Sum(item => GetUnitPrice(item) * item.Quantity),
                IsOrdered = cart.IsOrdered,
                CreatedAt = cart.CreatedAt,
                LastModifiedAt = (DateTime)cart.LastModifiedAt,
                User = cart.User == null ? null : new UserDto
                {
                    FirstName = cart.User.FirstName,
                    LastName = cart.User.LastName,
                    Email = cart.User.Email
                },
                CartItems = cart.CartItems.Select(ci =>
                {
                    var attributes = new List<CartItemAttributeDto>();

                    var attributeSelections = ci.AttributeSelections?
                        .OrderBy(sel => sel.ProductAttribute?.Name ?? string.Empty)
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

                    return new CartItemDto
                    {
                        CartItemId = ci.Id,
                        ProductId = ci.ProductId,
                        ProductAttributeCombinationId = ci.ProductAttributeCombinationId,
                        ProductName = ci.Product.Name,
                        Quantity = ci.Quantity,
                        Price = GetUnitPrice(ci),
                        TotalPrice = GetUnitPrice(ci) * ci.Quantity,
                        PersonalizationText = ci.PersonalizationText,
                        Attributes = attributes,
                    };
                }).ToList()
            };
        }


        public async Task<ShoppingCart?> GetCartByUserIdAsync(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

        return await _shoppingCartRepository.GetCartByUserIdAsync(userId);
    }

    public async Task<ShoppingCart?> GetCartByGuestIdentifierAsync(Guid guestIdentifier)
    {
        if (guestIdentifier == Guid.Empty)
            throw new ArgumentException("Guest identifier cannot be empty.", nameof(guestIdentifier));

        return await _shoppingCartRepository.GetCartByGuestIdentifierAsync(guestIdentifier);
    }

    public async Task<bool> HasActiveCartAsync(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

        return await _shoppingCartRepository.HasActiveCartAsync(userId);
    }

    public async Task<int> GetCartItemCountAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        return await _shoppingCartRepository.GetCartItemCountAsync(cartId);
    }

    public async Task<decimal> GetCartTotalAmountAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        return await _shoppingCartRepository.GetCartTotalAmountAsync(cartId);
    }

    public async Task<bool> IsCartValidAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        return await _shoppingCartRepository.IsCartValidAsync(cartId);
    }

    public async Task<bool> IsCartEmptyAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        return await _shoppingCartRepository.IsCartEmptyAsync(cartId);
    }

    public async Task<ShoppingCart?> GetLastModifiedCartAsync(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

        return await _shoppingCartRepository.GetLastModifiedCartAsync(userId);
    }

    public async Task MergeGuestCartToUserAsync(Guid guestIdentifier, int userId)
    {
        if (guestIdentifier == Guid.Empty)
            throw new ArgumentException("Guest identifier cannot be empty.", nameof(guestIdentifier));
        
        if (userId <= 0)
            throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

        try
        {
            _logger.LogInformation("Merging guest cart {GuestId} to user {UserId}", guestIdentifier, userId);
            await _shoppingCartRepository.MergeGuestCartToUserAsync(guestIdentifier, userId);
            _logger.LogInformation("Successfully merged guest cart to user cart");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging guest cart {GuestId} to user {UserId}", guestIdentifier, userId);
            throw;
        }
    }

    public async Task<ShoppingCart?> GetActiveCartByUserIdAsync(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

        return await _shoppingCartRepository.GetActiveCartByUserIdAsync(userId);
    }

    public async Task<ShoppingCart?> GetCartByGuestIdAsync(Guid guestIdentifier)
    {
        if (guestIdentifier == Guid.Empty)
            throw new ArgumentException("Guest identifier cannot be empty.", nameof(guestIdentifier));

        return await _shoppingCartRepository.GetCartByGuestIdAsync(guestIdentifier);
    }

    public async Task<bool> IsProductInCartAsync(int cartId, int productId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));
        
        if (productId <= 0)
            throw new ArgumentException("Product ID must be greater than zero.", nameof(productId));

        return await _shoppingCartRepository.IsProductInCartAsync(cartId, productId);
    }

    public async Task<IReadOnlyList<CartItem>> GetCartItemsAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        return await _shoppingCartRepository.GetCartItemsAsync(cartId);
    }

    public async Task AddItemToCartAsync(int cartId, CartItem item)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));
        
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        
        if (item.ProductId <= 0)
            throw new ArgumentException("Product ID must be greater than zero.");
        
        if (item.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        item.PersonalizationText = string.IsNullOrWhiteSpace(item.PersonalizationText)
            ? null
            : item.PersonalizationText.Trim();

        if (item.ProductAttributeCombinationId.HasValue && item.ProductAttributeCombinationId.Value <= 0)
        {
            item.ProductAttributeCombinationId = null;
        }

        foreach (var selection in item.AttributeSelections ?? Enumerable.Empty<CartItemAttributeSelection>())
        {
            if (selection.ProductAttributeCombinationId.HasValue && selection.ProductAttributeCombinationId.Value <= 0)
            {
                selection.ProductAttributeCombinationId = null;
            }

            if (selection.ProductAttributeValueId.HasValue && selection.ProductAttributeValueId.Value <= 0)
            {
                selection.ProductAttributeValueId = null;
            }

            selection.PersonalizationText = string.IsNullOrWhiteSpace(selection.PersonalizationText)
                ? null
                : selection.PersonalizationText.Trim();
        }

        var incomingAttributeFingerprint = BuildAttributeSelectionFingerprint(item.AttributeSelections);

        // İş kuralı: Aynı ürün sepette var mı kontrol et
        var cartItems = await _shoppingCartRepository.GetCartItemsAsync(cartId);
        var currentItem = cartItems.FirstOrDefault(x =>
            x.ProductId == item.ProductId &&
            x.ProductAttributeCombinationId == item.ProductAttributeCombinationId &&
            string.Equals(x.PersonalizationText ?? string.Empty, item.PersonalizationText ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(BuildAttributeSelectionFingerprint(x.AttributeSelections), incomingAttributeFingerprint, StringComparison.Ordinal));

        if (currentItem != null)
        {
            await _shoppingCartRepository.UpdateCartItemQuantityAsync(cartId, currentItem.Id, currentItem.Quantity + item.Quantity);
            _logger.LogInformation("Updated quantity for cart item {CartItemId} (product {ProductId}) in cart {CartId}", currentItem.Id, item.ProductId, cartId);
        }
        else
        {
            var stockAvailable = await _shoppingCartRepository.AreAllItemsInStockAsync(cartId);
            await _shoppingCartRepository.AddItemToCartAsync(cartId, item);
            _logger.LogInformation("Added new product {ProductId} to cart {CartId}", item.ProductId, cartId);
        }
    }

    private static string BuildAttributeSelectionFingerprint(IEnumerable<CartItemAttributeSelection> selections)
    {
        if (selections == null)
        {
            return string.Empty;
        }

        var ordered = selections
            .Select(sel => new
            {
                sel.ProductAttributeId,
                ProductAttributeValueId = sel.ProductAttributeValueId ?? 0,
                CombinationId = sel.ProductAttributeCombinationId ?? 0,
                Text = (sel.PersonalizationText ?? string.Empty).Trim().ToLowerInvariant()
            })
            .OrderBy(x => x.ProductAttributeId)
            .ThenBy(x => x.ProductAttributeValueId)
            .ThenBy(x => x.CombinationId)
            .ThenBy(x => x.Text)
            .ToList();

        if (!ordered.Any())
        {
            return string.Empty;
        }

        return string.Join("|", ordered.Select(x => $"{x.ProductAttributeId}:{x.ProductAttributeValueId}:{x.CombinationId}:{x.Text}"));
    }

    public async Task RemoveItemFromCartAsync(int cartId, int cartItemId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));
        
        if (cartItemId <= 0)
            throw new ArgumentException("Cart item ID must be greater than zero.", nameof(cartItemId));

        // İş kuralı: Ürün sepette var mı kontrol et
        var cartItem = await _shoppingCartRepository.GetCartItemByIdAsync(cartId, cartItemId);
        if (cartItem is null)
        {
            _logger.LogWarning("Attempted to remove non-existent cart item {CartItemId} from cart {CartId}", cartItemId, cartId);
            throw new InvalidOperationException($"Cart item {cartItemId} is not in cart {cartId}");
        }

        await _shoppingCartRepository.RemoveItemFromCartAsync(cartId, cartItemId);
        _logger.LogInformation("Removed cart item {CartItemId} from cart {CartId}", cartItemId, cartId);
    }

    public async Task UpdateCartItemQuantityAsync(int cartId, int cartItemId, int newQuantity)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));
        
        if (cartItemId <= 0)
            throw new ArgumentException("Cart item ID must be greater than zero.", nameof(cartItemId));
        
        if (newQuantity < 0)
            throw new ArgumentException("Quantity cannot be negative.", nameof(newQuantity));

        // İş kuralı: Ürün sepette var mı kontrol et
        var cartItem = await _shoppingCartRepository.GetCartItemByIdAsync(cartId, cartItemId);
        if (cartItem is null)
        {
            _logger.LogWarning("Attempted to update quantity for non-existent cart item {CartItemId} in cart {CartId}", cartItemId, cartId);
            throw new InvalidOperationException($"Cart item {cartItemId} is not in cart {cartId}");
        }

        // İş kuralı: Quantity 0 ise ürünü kaldır
        if (newQuantity == 0)
        {
            await _shoppingCartRepository.RemoveItemFromCartAsync(cartId, cartItemId);
            _logger.LogInformation("Removed cart item {CartItemId} from cart {CartId} due to zero quantity", cartItemId, cartId);
        }
        else
        {
            await _shoppingCartRepository.UpdateCartItemQuantityAsync(cartId, cartItemId, newQuantity);
            _logger.LogInformation("Updated quantity to {Quantity} for cart item {CartItemId} in cart {CartId}", newQuantity, cartItemId, cartId);
        }
    }

    public async Task ClearCartAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        // İş kuralı: Sepet boş mu kontrol et
        var isEmpty = await _shoppingCartRepository.IsCartEmptyAsync(cartId);
        if (isEmpty)
        {
            _logger.LogInformation("Cart {CartId} is already empty", cartId);
            return;
        }

        await _shoppingCartRepository.ClearCartAsync(cartId);
        _logger.LogInformation("Cleared cart {CartId}", cartId);
    }

    public async Task<decimal> CalculateCartTotalAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        return (await _priceQuoteService.GetCartQuoteAsync(cartId)).GrandTotal;
    }

    public Task<CartQuoteDto> GetCartQuoteAsync(int cartId, int? shippingMethodId = null, string? couponCode = null, string? city = null, string? district = null)
        => _priceQuoteService.GetCartQuoteAsync(cartId, shippingMethodId, couponCode, city, district);

    public async Task<bool> AreAllItemsInStockAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        // İş kuralı: Sepet boşsa false döndür
        var isEmpty = await _shoppingCartRepository.IsCartEmptyAsync(cartId);
        if (isEmpty)
        {
            _logger.LogWarning("Stock check attempted on empty cart {CartId}", cartId);
            return false;
        }

        return await _shoppingCartRepository.AreAllItemsInStockAsync(cartId);
    }

    public async Task<ShoppingCart?> GetCartBySessionIdAsync(int sessionId)
    {
        if (sessionId <= 0)
            throw new ArgumentException("Session ID must be greater than zero.", nameof(sessionId));

        return await _shoppingCartRepository.GetCartBySessionIdAsync(sessionId);
    }

    public async Task<IReadOnlyList<CartItemDto>> GetCartItemsWithProductInfoAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        return await _shoppingCartRepository.GetCartItemsWithProductInfoAsync(cartId);
    }

    public async Task<IReadOnlyList<DiscountResult>> ApplyCartCampaignsAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        // İş kuralı: Sepet boşsa kampanya uygulanmaz
        var isEmpty = await _shoppingCartRepository.IsCartEmptyAsync(cartId);
        if (isEmpty)
        {
            _logger.LogInformation("No campaigns applied to empty cart {CartId}", cartId);
            return new List<DiscountResult>();
        }

        // İş kuralı: Minimum tutar kontrolü (örnek: 100 TL üzeri için kampanya)
        var cartTotal = await _shoppingCartRepository.CalculateCartTotalAsync(cartId);
        if (cartTotal < 100)
        {
            _logger.LogInformation("Cart {CartId} total {Total} is below minimum for campaigns", cartId, cartTotal);
            return new List<DiscountResult>();
        }

        return await _shoppingCartRepository.ApplyCartCampaignsAsync(cartId);
    }

    // Ek iş kuralı metodları
    public async Task<bool> ValidateCartForCheckoutAsync(int cartId)
    {
        if (cartId <= 0)
            throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

        // Sepet var mı?
        var cart = await _shoppingCartRepository.GetCartWithDetailsAsync(cartId);
        if (cart == null)
        {
            _logger.LogWarning("Cart {CartId} not found for checkout validation", cartId);
            return false;
        }

        // Sepet boş mu?
        if (await _shoppingCartRepository.IsCartEmptyAsync(cartId))
        {
            _logger.LogWarning("Cart {CartId} is empty, cannot checkout", cartId);
            return false;
        }

        // Sepet geçerli mi? (ürünler aktif mi?)
        if (!await _shoppingCartRepository.IsCartValidAsync(cartId))
        {
            _logger.LogWarning("Cart {CartId} contains inactive products", cartId);
            return false;
        }

        // Stok kontrolü
        if (!await _shoppingCartRepository.AreAllItemsInStockAsync(cartId))
        {
            _logger.LogWarning("Cart {CartId} contains out-of-stock items", cartId);
            return false;
        }

        _logger.LogInformation("Cart {CartId} is valid for checkout", cartId);
        return true;
    }

    public async Task<ShoppingCart> CreateOrGetActiveCartAsync(int? userId = null, Guid? guestIdentifier = null)
    {
        if (userId.HasValue && userId <= 0)
            throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

        if (guestIdentifier.HasValue && guestIdentifier == Guid.Empty)
            throw new ArgumentException("Guest identifier cannot be empty.", nameof(guestIdentifier));

        if (!userId.HasValue && !guestIdentifier.HasValue)
            throw new ArgumentException("Either userId or guestIdentifier must be provided.");

        ShoppingCart? cart = null;

        if (userId.HasValue)
        {
            cart = await _shoppingCartRepository.GetActiveCartByUserIdAsync(userId.Value);
        }
        else if (guestIdentifier.HasValue)
        {
            cart = await _shoppingCartRepository.GetCartByGuestIdAsync(guestIdentifier.Value);
        }

        if (cart == null)
        {
            // Yeni sepet oluştur
            cart = new ShoppingCart
            {
                UserId = userId,
                GuestIdentifier = guestIdentifier,
                IsOrdered = false,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            await _shoppingCartRepository.AddAsync(cart);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Created new cart for {UserType} {Id}", 
                userId.HasValue ? "user" : "guest", 
                userId?.ToString() ?? guestIdentifier?.ToString());
        }

        return cart;
    }
        /// <summary>
        /// Her zaman yeni bir sepet oluşturur (mevcut sepeti kontrol etmez)
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si (opsiyonel)</param>
        /// <param name="guestIdentifier">Misafir kimliği (opsiyonel)</param>
        /// <returns>Oluşturulan yeni sepet</returns>
        public async Task<ShoppingCart> CreateNewCartAsync(int? userId = null, Guid? guestIdentifier = null)
        {
            if (userId.HasValue && userId <= 0)
                throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

            if (guestIdentifier.HasValue && guestIdentifier == Guid.Empty)
                throw new ArgumentException("Guest identifier cannot be empty.", nameof(guestIdentifier));

            if (!userId.HasValue && !guestIdentifier.HasValue)
                throw new ArgumentException("Either userId or guestIdentifier must be provided.");

            // İş kuralı: Kullanıcı ID'si ve misafir kimliği aynı anda belirtilemez
            if (userId.HasValue && guestIdentifier.HasValue)
                throw new ArgumentException("Both userId and guestIdentifier cannot be provided at the same time.");

            try
            {
                // Her zaman yeni sepet oluştur
                var cart = new ShoppingCart
                {
                    UserId = userId,
                    GuestIdentifier = guestIdentifier,
                    IsOrdered = false,
                    TotalAmount = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    CartItems = new List<CartItem>()
                };

                await _shoppingCartRepository.AddAsync(cart);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Created new cart {CartId} for {UserType} {Id}",
                    cart.Id,
                    userId.HasValue ? "user" : "guest",
                    userId?.ToString() ?? guestIdentifier?.ToString());

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new cart for {UserType} {Id}",
                    userId.HasValue ? "user" : "guest",
                    userId?.ToString() ?? guestIdentifier?.ToString());
                throw;
            }
        }

        /// <summary>
        /// Sepeti sipariş durumuna geçirir
        /// </summary>
        /// <param name="cartId">Sepet ID'si</param>
        /// <returns></returns>
        public async Task MarkCartAsOrderedAsync(int cartId)
        {
            if (cartId <= 0)
                throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

            try
            {
                var cart = await _shoppingCartRepository.GetByIdAsync(cartId);
                if (cart == null)
                {
                    _logger.LogWarning("Cart {CartId} not found for marking as ordered", cartId);
                    throw new InvalidOperationException($"Cart {cartId} not found");
                }

                if (cart.IsOrdered)
                {
                    _logger.LogInformation("Cart {CartId} is already marked as ordered", cartId);
                    return;
                }

                // İş kuralı: Sipariş verilmeden önce sepet doğrulanmalı
                var isValid = await ValidateCartForCheckoutAsync(cartId);
                if (!isValid)
                {
                    _logger.LogWarning("Cart {CartId} is not valid for ordering", cartId);
                    throw new InvalidOperationException($"Cart {cartId} is not valid for checkout");
                }

                cart.IsOrdered = true;
                cart.LastModifiedAt = DateTime.UtcNow;

                await _shoppingCartRepository.UpdateAsync(cart);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Cart {CartId} marked as ordered", cartId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking cart {CartId} as ordered", cartId);
                throw;
            }
        }

        ///// <summary>
        ///// Kullanıcının tüm sepetlerini getirir (aktif ve sipariş verilmiş olanlar dahil)
        ///// </summary>
        ///// <param name="userId">Kullanıcı ID'si</param>
        ///// <returns>Kullanıcının sepetleri</returns>
        //public async Task<IReadOnlyList<ShoppingCart>> GetUserCartsAsync(int userId)
        //{
        //    if (userId <= 0)
        //        throw new ArgumentException("User ID must be greater than zero.", nameof(userId));

        //    try
        //    {
        //        return await _shoppingCartRepository.GetCartByUserIdAsync(userId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting carts for user {UserId}", userId);
        //        throw;
        //    }
        //}

        /// <summary>
        /// Sepet durumunu günceller
        /// </summary>
        /// <param name="cartId">Sepet ID'si</param>
        /// <param name="isOrdered">Sipariş verildi mi?</param>
        /// <returns></returns>
        public async Task UpdateCartStatusAsync(int cartId, bool isOrdered)
        {
            if (cartId <= 0)
                throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));

            try
            {
                var cart = await _shoppingCartRepository.GetByIdAsync(cartId);
                if (cart == null)
                {
                    _logger.LogWarning("Cart {CartId} not found for status update", cartId);
                    throw new InvalidOperationException($"Cart {cartId} not found");
                }

                if (cart.IsOrdered == isOrdered)
                {
                    _logger.LogInformation("Cart {CartId} status is already {Status}", cartId, isOrdered ? "ordered" : "active");
                    return;
                }

                // İş kuralı: Sipariş verilmiş sepet tekrar aktif hale getirilemez
                if (cart.IsOrdered && !isOrdered)
                {
                    _logger.LogWarning("Cannot reactivate ordered cart {CartId}", cartId);
                    throw new InvalidOperationException($"Cannot reactivate ordered cart {cartId}");
                }

                cart.IsOrdered = isOrdered;
                cart.LastModifiedAt = DateTime.UtcNow;

                await _shoppingCartRepository.UpdateAsync(cart);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Cart {CartId} status updated to {Status}", cartId, isOrdered ? "ordered" : "active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart {CartId} status", cartId);
                throw;
            }
        }

        public async Task<IReadOnlyList<ShoppingCart>> GetAllCartsAsync()
        {
            return await _shoppingCartRepository.GetAllCartsAsync();
        }

        public async Task<ShoppingCart?> GetCartByIdAsync(int cartId)
        {
            if (cartId <= 0)
                throw new ArgumentException("Cart ID must be greater than zero.", nameof(cartId));
            return await _shoppingCartRepository.GetByIdAsync(cartId);
        }

        public async Task<bool> DeleteCartAsync(int cartId)
        {
            var cart = await _shoppingCartRepository.GetByIdAsync(cartId);
            if (cart == null)
                return false;
            await _shoppingCartRepository.DeleteAsync(cart);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public Task<IReadOnlyList<ShoppingCart>> GetUserCartsAsync(int userId)
        {
            throw new NotImplementedException();
        }
    }
}
