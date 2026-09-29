using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ECommerce.API.Filters;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using ECommerce.API.Services;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireCartOwnership]
    //[Produces("application/json")]
    public class ShoppingCartController : ControllerBase
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILogger<ShoppingCartController> _logger;
        private readonly ProductCampaignQuoteService _campaignQuotes;
        private readonly IWebHostEnvironment _environment;

        public ShoppingCartController(
            IShoppingCartService shoppingCartService,
            ILogger<ShoppingCartController> logger,
            ProductCampaignQuoteService campaignQuotes,
            IWebHostEnvironment environment)
        {
            _shoppingCartService = shoppingCartService;
            _logger = logger;
            _campaignQuotes = campaignQuotes;
            _environment = environment;
        }
        /// <summary>
        /// Kullanıcı için yeni sepet oluşturur
        /// </summary>
        [HttpPost("user/{userId:int}")]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUserCart(int userId)
        {
            if (!CurrentUserOwns(userId)) return Forbid();
            try
            {
                var cart = await _shoppingCartService.CreateNewCartAsync(userId, null);

                return CreatedAtAction(nameof(GetCart), new { cartId = cart.Id },
                    DataResponse<ShoppingCart>.CreateSuccess(cart, "Kullanıcı sepeti başarıyla oluşturuldu"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user cart for user {UserId}", userId);
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcı sepeti oluşturma işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Misafir kullanıcı için yeni sepet oluşturur
        /// </summary>
        [HttpPost("guest")]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGuestCart([FromBody] CreateGuestCartRequestDto request)
        {
            try
            {
                var guestId = request.GuestIdentifier ?? Guid.NewGuid();
                var cart = await _shoppingCartService.CreateNewCartAsync(null, guestId);

                return CreatedAtAction(nameof(GetGuestCart), new { guestIdentifier = guestId },
                    DataResponse<ShoppingCart>.CreateSuccess(cart, "Misafir sepeti başarıyla oluşturuldu"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating guest cart");
                return StatusCode(500, BaseResponse.CreateFailure("Misafir sepeti oluşturma işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Yeni bir sepet oluşturur
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCart([FromBody] CreateCartRequestDto request)
        {
            try
            {
                // Kullanıcı ID'si veya misafir kimliği en az birisi olmalı
                if (!request.UserId.HasValue && !request.GuestIdentifier.HasValue)
                {
                    return BadRequest(BaseResponse.CreateFailure("Kullanıcı ID'si veya misafir kimliği belirtilmelidir"));
                }

                // Her ikisi de belirtilmemeli
                if (request.UserId.HasValue && request.GuestIdentifier.HasValue)
                {
                    return BadRequest(BaseResponse.CreateFailure("Kullanıcı ID'si ve misafir kimliği aynı anda belirtilemez"));
                }

                var cart = await _shoppingCartService.CreateNewCartAsync(request.UserId, request.GuestIdentifier);

                return CreatedAtAction(nameof(GetCart), new { cartId = cart.Id },
                    DataResponse<ShoppingCart>.CreateSuccess(cart, "Yeni sepet başarıyla oluşturuldu"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new cart");
                return StatusCode(500, BaseResponse.CreateFailure("Sepet oluşturma işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepeti detaylarıyla birlikte getirir
        /// </summary>
        [HttpGet("{cartId:int}")]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCart(int cartId)
        {
            try
            {
                var cart = await _shoppingCartService.GetCartWithDetailsAsync(cartId);
                if (cart == null)
                    return NotFound(BaseResponse.CreateFailure($"Sepet bulunamadı. ID: {cartId}"));

                return Ok(DataResponse<ShoppingCart>.CreateSuccess(cart, "Sepet başarıyla getirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Sepet getirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Kullanıcının aktif sepetini getirir
        /// </summary>
        [HttpGet("user/{userId:int}")]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUserCart(int userId)
        {
            if (!CurrentUserOwns(userId)) return Forbid();
            try
            {
                var cart = await _shoppingCartService.GetActiveCartByUserIdAsync(userId);
                if (cart == null)
                    return NotFound(BaseResponse.CreateFailure($"Kullanıcının aktif sepeti bulunamadı. Kullanıcı ID: {userId}"));

                return Ok(DataResponse<ShoppingCart>.CreateSuccess(cart, "Kullanıcı sepeti başarıyla getirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
                return StatusCode(500, BaseResponse.CreateFailure("Kullanıcı sepeti getirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Misafir kullanıcının sepetini getirir
        /// </summary>
        [HttpGet("guest/{guestIdentifier:guid}")]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGuestCart(Guid guestIdentifier)
        {
            if (!GuestHeaderMatches(guestIdentifier)) return Forbid();
            try
            {
                var cart = await _shoppingCartService.GetCartByGuestIdAsync(guestIdentifier);
                if (cart == null)
                    return NotFound(BaseResponse.CreateFailure($"Misafir kullanıcının sepeti bulunamadı. ID: {guestIdentifier}"));

                return Ok(DataResponse<ShoppingCart>.CreateSuccess(cart, "Misafir sepeti başarıyla getirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving guest cart {GuestId}", guestIdentifier);
                return StatusCode(500, BaseResponse.CreateFailure("Misafir sepeti getirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Yeni sepet oluşturur veya mevcut aktif sepeti getirir
        /// </summary>
        [HttpPost("create-or-get")]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(DataResponse<ShoppingCart>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrGetActiveCart([FromBody] CreateCartRequestDto request)
        {
            try
            {
                var cart = await _shoppingCartService.CreateOrGetActiveCartAsync(request.UserId, request.GuestIdentifier);

                var isNewCart = cart.CreatedAt >= DateTime.UtcNow.AddMinutes(-1);

                if (isNewCart)
                    return CreatedAtAction(nameof(GetCart), new { cartId = cart.Id },
                        DataResponse<ShoppingCart>.CreateSuccess(cart, "Yeni sepet oluşturuldu"));
                else
                    return Ok(DataResponse<ShoppingCart>.CreateSuccess(cart, "Mevcut sepet getirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or getting cart");
                return StatusCode(500, BaseResponse.CreateFailure("Sepet oluşturma/getirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepet ürünlerini getirir
        /// </summary>
        [HttpGet("{cartId:int}/items")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<CartItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCartItems(int cartId)
        {
            try
            {
                var items = await _shoppingCartService.GetCartItemsAsync(cartId);
                return Ok(DataResponse<IReadOnlyList<CartItem>>.CreateSuccess(items, "Sepet ürünleri başarıyla getirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart items for cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Sepet ürünleri getirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepet ürünlerini ürün bilgileriyle birlikte getirir
        /// </summary>
        [HttpGet("{cartId:int}/items/detailed")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<CartItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCartItemsWithProductInfo(int cartId)
        {
            try
            {
                var items = await _shoppingCartService.GetCartItemsWithProductInfoAsync(cartId);
                return Ok(DataResponse<IReadOnlyList<CartItemDto>>.CreateSuccess(items, "Detaylı sepet ürünleri başarıyla getirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving detailed cart items for cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Detaylı sepet ürünleri getirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepete ürün ekler
        /// </summary>
        [HttpPost("{cartId:int}/items")]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItemToCart(int cartId, [FromBody] AddItemToCartRequestDto request)
        {
            try
            {
                var cartItem = new CartItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    PersonalizationText = string.IsNullOrWhiteSpace(request.PersonalizationText)
                        ? null
                        : request.PersonalizationText.Trim(),
                    ProductAttributeCombinationId = request.ProductAttributeCombinationId,
                    AttributeSelections = (request.Attributes ?? new List<CartItemAttributeSelectionRequestDto>())
                        .Select(attr => new CartItemAttributeSelection
                        {
                            ProductAttributeId = attr.ProductAttributeId,
                            ProductAttributeValueId = attr.ProductAttributeValueId,
                            ProductAttributeCombinationId = attr.ProductAttributeCombinationId,
                            PersonalizationText = string.IsNullOrWhiteSpace(attr.PersonalizationText)
                                ? null
                                : attr.PersonalizationText.Trim()
                        })
                        .ToList()
                };

                await _shoppingCartService.AddItemToCartAsync(cartId, cartItem);
                return Ok(BaseResponse.CreateSuccess("Ürün sepete başarıyla eklendi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün sepete ekleme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>Adds a campaign package using a server-authoritative price quote.</summary>
        [HttpPost("{cartId:int}/campaign-items")]
        public async Task<IActionResult> AddCampaignItemToCart(int cartId, [FromBody] AddCampaignItemRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Quantity < 1) return BadRequest(BaseResponse.CreateFailure("Miktar 1'den büyük olmalıdır."));
                if (!string.IsNullOrWhiteSpace(request.AddressLine) && request.AddressLine.Trim().Length > 1000) return BadRequest(BaseResponse.CreateFailure("Adres en fazla 1000 karakter olabilir."));
                if (request.PreferredDate is { } preferredDate && preferredDate < DateOnly.FromDateTime(DateTime.UtcNow)) return BadRequest(BaseResponse.CreateFailure("Keşif tarihi geçmiş bir tarih olamaz."));
                if ((request.PhotoUrls ?? []).Any(url => !url.StartsWith("/uploads/campaigns/", StringComparison.Ordinal))) return BadRequest(BaseResponse.CreateFailure("Geçersiz kampanya fotoğrafı."));
                var campaignPhotoDirectory = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "campaigns");
                if ((request.PhotoUrls ?? []).Any(url => !System.IO.File.Exists(Path.Combine(campaignPhotoDirectory, Path.GetFileName(url))))) return BadRequest(BaseResponse.CreateFailure("Yüklenen kampanya fotoğrafı bulunamadı."));
                var package = await _campaignQuotes.GetActivePackageByIdAsync(request.PackageId, cancellationToken);
                if (package?.RequiresExistingDevicePhoto == true && !(request.PhotoUrls?.Any() ?? false)) return BadRequest(BaseResponse.CreateFailure("Bu kampanya için mevcut cihaz fotoğrafı zorunludur."));
                var quote = await _campaignQuotes.QuoteAsync(request.PackageId, new ProductCampaignQuoteRequest(request.ProductId, request.City, request.District, request.OptionIds), cancellationToken);
                var snapshot = JsonSerializer.Serialize(new
                {
                    quote.PackageId, quote.Title, quote.StartingPrice, quote.City, quote.District,
                    request.AddressLine, request.PreferredDate, photos = request.PhotoUrls, quote.LocationAdjustment, quote.Selections, quote.Total
                });
                await _shoppingCartService.AddItemToCartAsync(cartId, new CartItem
                {
                    ProductId = quote.ProductId,
                    Quantity = request.Quantity,
                    ProductCampaignPackageId = quote.PackageId,
                    UnitPriceSnapshot = quote.Total,
                    CampaignSnapshotJson = snapshot
                });
                return Ok(DataResponse<ProductCampaignQuoteResult>.CreateSuccess(quote, "Kampanyalı ürün sepete eklendi."));
            }
            catch (ArgumentException exception) { return BadRequest(BaseResponse.CreateFailure(exception.Message)); }
        }

        /// <summary>
        /// Sepetten ürün kaldırır
        /// </summary>
        [HttpDelete("{cartId:int}/items/{cartItemId:int}")]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveItemFromCart(int cartId, int cartItemId)
        {
            try
            {
                await _shoppingCartService.RemoveItemFromCartAsync(cartId, cartItemId);
                return Ok(BaseResponse.CreateSuccess("Ürün sepetten başarıyla kaldırıldı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün sepetten kaldırma işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepetteki ürün miktarını günceller
        /// </summary>
        [HttpPut("{cartId:int}/items/{cartItemId:int}/quantity")]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCartItemQuantity(int cartId, int cartItemId, [FromBody] UpdateQuantityRequestDto request)
        {
            try
            {
                await _shoppingCartService.UpdateCartItemQuantityAsync(cartId, cartItemId, request.Quantity);
                return Ok(BaseResponse.CreateSuccess("Ürün miktarı başarıyla güncellendi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item quantity in cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün miktarı güncelleme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepeti temizler
        /// </summary>
        [HttpDelete("{cartId:int}/items")]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ClearCart(int cartId)
        {
            try
            {
                await _shoppingCartService.ClearCartAsync(cartId);
                return Ok(BaseResponse.CreateSuccess("Sepet başarıyla temizlendi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Sepet temizleme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepet toplam tutarını hesaplar
        /// </summary>
        [HttpGet("{cartId:int}/total")]
        [ProducesResponseType(typeof(DataResponse<CartTotalResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCartTotal(int cartId)
        {
            try
            {
                var total = await _shoppingCartService.CalculateCartTotalAsync(cartId);
                var response = new CartTotalResponseDto { Total = total };
                return Ok(DataResponse<CartTotalResponseDto>.CreateSuccess(response, "Sepet toplamı başarıyla hesaplandı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cart total for cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Sepet toplamı hesaplama işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>Returns the server-authoritative cart price breakdown.</summary>
        [HttpGet("{cartId:int}/quote")]
        [ProducesResponseType(typeof(DataResponse<CartQuoteDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCartQuote(int cartId, [FromQuery] int? shippingMethodId = null, [FromQuery] string? couponCode = null, [FromQuery] string? city = null, [FromQuery] string? district = null)
        {
            try
            {
                var quote = await _shoppingCartService.GetCartQuoteAsync(cartId, shippingMethodId, couponCode, city, district);
                return Ok(DataResponse<CartQuoteDto>.CreateSuccess(quote, "Sepet fiyat özeti hazırlandı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error quoting cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Sepet fiyat özeti hazırlanırken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepetteki ürün sayısını getirir
        /// </summary>
        [HttpGet("{cartId:int}/count")]
        [ProducesResponseType(typeof(DataResponse<CartItemCountResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCartItemCount(int cartId)
        {
            try
            {
                var count = await _shoppingCartService.GetCartItemCountAsync(cartId);
                var response = new CartItemCountResponseDto { Count = count };
                return Ok(DataResponse<CartItemCountResponseDto>.CreateSuccess(response, "Sepet ürün sayısı başarıyla getirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Sepet ürün sayısı getirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepet checkout için geçerli mi kontrol eder
        /// </summary>
        [HttpGet("{cartId:int}/validate")]
        [ProducesResponseType(typeof(DataResponse<CartValidationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateCartForCheckout(int cartId)
        {
            try
            {
                var isValid = await _shoppingCartService.ValidateCartForCheckoutAsync(cartId);
                var response = new CartValidationResponseDto { IsValid = isValid };
                return Ok(DataResponse<CartValidationResponseDto>.CreateSuccess(response, "Sepet doğrulama işlemi tamamlandı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart {CartId} for checkout", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Sepet doğrulama işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepetteki tüm ürünlerin stokta olup olmadığını kontrol eder
        /// </summary>
        [HttpGet("{cartId:int}/stock-check")]
        [ProducesResponseType(typeof(DataResponse<StockCheckResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckCartStock(int cartId)
        {
            try
            {
                var inStock = await _shoppingCartService.AreAllItemsInStockAsync(cartId);
                var response = new StockCheckResponseDto { InStock = inStock };
                return Ok(DataResponse<StockCheckResponseDto>.CreateSuccess(response, "Stok kontrolü tamamlandı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking stock for cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Stok kontrolü işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Sepete kampanyaları uygular
        /// </summary>
        [HttpPost("{cartId:int}/apply-campaigns")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<DiscountResult>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApplyCartCampaigns(int cartId)
        {
            try
            {
                var campaigns = await _shoppingCartService.ApplyCartCampaignsAsync(cartId);
                return Ok(DataResponse<IReadOnlyList<DiscountResult>>.CreateSuccess(campaigns, "Kampanyalar başarıyla uygulandı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying campaigns for cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Kampanya uygulama işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Misafir sepetini kullanıcı sepetine birleştirir
        /// </summary>
        [HttpPost("merge-guest-cart")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MergeGuestCartToUser([FromBody] MergeCartRequestDto request)
        {
            if (!CurrentUserOwns(request.UserId) || !GuestHeaderMatches(request.GuestIdentifier)) return Forbid();
            try
            {
                await _shoppingCartService.MergeGuestCartToUserAsync(request.GuestIdentifier, request.UserId);
                return Ok(BaseResponse.CreateSuccess("Misafir sepeti kullanıcı sepetine başarıyla birleştirildi"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging guest cart to user");
                return StatusCode(500, BaseResponse.CreateFailure("Sepet birleştirme işlemi sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Kullanıcının aktif sepeti olup olmadığını kontrol eder
        /// </summary>
        [HttpGet("user/{userId:int}/has-active-cart")]
        [ProducesResponseType(typeof(DataResponse<HasActiveCartResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HasActiveCart(int userId)
        {
            if (!CurrentUserOwns(userId)) return Forbid();
            try
            {
                var hasActiveCart = await _shoppingCartService.HasActiveCartAsync(userId);
                var response = new HasActiveCartResponseDto { HasActiveCart = hasActiveCart };
                return Ok(DataResponse<HasActiveCartResponseDto>.CreateSuccess(response, "Aktif sepet kontrolü tamamlandı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active cart for user {UserId}", userId);
                return StatusCode(500, BaseResponse.CreateFailure("Aktif sepet kontrolü sırasında bir hata oluştu"));
            }
        }

        private bool CurrentUserOwns(int userId)
        {
            if (User.IsInRole("Admin")) return true;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return User.Identity?.IsAuthenticated == true && claim == userId.ToString();
        }

        private bool GuestHeaderMatches(Guid guestIdentifier)
            => Guid.TryParse(Request.Headers["X-Guest-Identifier"], out var headerGuestIdentifier)
                && headerGuestIdentifier == guestIdentifier;

        /// <summary>
        /// Sepette belirli bir ürün var mı kontrol eder
        /// </summary>
        [HttpGet("{cartId:int}/products/{productId:int}/exists")]
        [ProducesResponseType(typeof(DataResponse<ProductExistsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IsProductInCart(int cartId, int productId)
        {
            try
            {
                var exists = await _shoppingCartService.IsProductInCartAsync(cartId, productId);
                var response = new ProductExistsResponseDto { Exists = exists };
                return Ok(DataResponse<ProductExistsResponseDto>.CreateSuccess(response, "Ürün varlık kontrolü tamamlandı"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product existence in cart {CartId}", cartId);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün varlık kontrolü sırasında bir hata oluştu"));
            }
        }
    }

    #region Request DTOs
    public class CreateCartRequestDto
    {
        public int? UserId { get; set; }
        public Guid? GuestIdentifier { get; set; }
    }

    public class AddItemToCartRequestDto
    {
        [Required(ErrorMessage = "Ürün ID'si gereklidir")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        public int Quantity { get; set; }

        //[Required(ErrorMessage = "Birim fiyat gereklidir")]
        //[Range(0.01, double.MaxValue, ErrorMessage = "Birim fiyat 0'dan büyük olmalıdır")]
        //public decimal UnitPrice { get; set; }

        public string? PersonalizationText { get; set; }

        public int? ProductAttributeCombinationId { get; set; }

        public List<CartItemAttributeSelectionRequestDto>? Attributes { get; set; }
    }

    public class AddCampaignItemRequestDto
    {
        [Required] public int ProductId { get; set; }
        [Required] public int PackageId { get; set; }
        [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
        [Required] public string? City { get; set; }
        public string? District { get; set; }
        public List<int>? OptionIds { get; set; }
        public string? AddressLine { get; set; }
        public DateOnly? PreferredDate { get; set; }
        public List<string>? PhotoUrls { get; set; }
    }

    public class UpdateQuantityRequestDto
    {
        [Required(ErrorMessage = "Miktar gereklidir")]
        [Range(0, int.MaxValue, ErrorMessage = "Miktar 0 veya daha büyük olmalıdır")]
        public int Quantity { get; set; }
    }

    public class MergeCartRequestDto
    {
        [Required(ErrorMessage = "Misafir kimliği gereklidir")]
        public Guid GuestIdentifier { get; set; }

        [Required(ErrorMessage = "Kullanıcı ID'si gereklidir")]
        public int UserId { get; set; }
    }
    #endregion

    #region Response DTOs
    public class CartTotalResponseDto
    {
        public decimal Total { get; set; }
    }

    public class CartItemCountResponseDto
    {
        public int Count { get; set; }
    }

    public class CartValidationResponseDto
    {
        public bool IsValid { get; set; }
    }
    public class CreateGuestCartRequestDto
    {
        public Guid? GuestIdentifier { get; set; }
    }
    public class StockCheckResponseDto
    {
        public bool InStock { get; set; }
    }

    public class HasActiveCartResponseDto
    {
        public bool HasActiveCart { get; set; }
    }

    public class ProductExistsResponseDto
    {
        public bool Exists { get; set; }
    }

    public class CartItemAttributeSelectionRequestDto
    {
        [Required(ErrorMessage = "Ürün özelliği ID'si gereklidir")]
        public int ProductAttributeId { get; set; }

        public int? ProductAttributeValueId { get; set; }

        public int? ProductAttributeCombinationId { get; set; }

        public string? PersonalizationText { get; set; }
    }
    #endregion
}
