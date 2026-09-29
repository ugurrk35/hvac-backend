using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommerce.Service.Concrete.ShoppingCartService;

namespace ECommerce.Service.Abstract
{
    public interface IShoppingCartService : IService<ShoppingCart>
    {
        // IShoppingCartService interface'ine eklenecek method tanımları:

        /// <summary>
        /// Yeni bir sepet oluşturur (mevcut sepeti kontrol etmez)
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si (opsiyonel)</param>
        /// <param name="guestIdentifier">Misafir kimliği (opsiyonel)</param>
        /// <returns>Oluşturulan sepet</returns>
        Task<ShoppingCart> CreateNewCartAsync(int? userId = null, Guid? guestIdentifier = null);

        /// <summary>
        /// Sepeti sipariş durumuna geçirir
        /// </summary>
        /// <param name="cartId">Sepet ID'si</param>
        /// <returns></returns>
        Task MarkCartAsOrderedAsync(int cartId);
        Task<ShoppingCart?> GetCartWithDetailsAsync(int cartId);
        Task<ShoppingCartDto?> GetCartWithDetailsDtoAsync(int cartId);
        /// <summary>
        /// Kullanıcının tüm sepetlerini getirir (aktif ve sipariş verilmiş olanlar dahil)
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Kullanıcının sepetleri</returns>
        Task<IReadOnlyList<ShoppingCart>> GetUserCartsAsync(int userId);

        /// <summary>
        /// Sepet durumunu günceller
        /// </summary>
        /// <param name="cartId">Sepet ID'si</param>
        /// <param name="isOrdered">Sipariş verildi mi?</param>
        /// <returns></returns>
        Task UpdateCartStatusAsync(int cartId, bool isOrdered);

        // Repository'de de bu method gerekli olacak:
        // Task<IReadOnlyList<ShoppingCart>> GetUserCartsAsync(int userId);
        //Task<ShoppingCart?> GetCartWithDetailsAsync(int cartId);
        Task<ShoppingCart?> GetCartByUserIdAsync(int userId);
        Task<ShoppingCart?> GetCartByGuestIdentifierAsync(Guid guestIdentifier);
        Task<bool> HasActiveCartAsync(int userId);
        Task<int> GetCartItemCountAsync(int cartId);
        Task<decimal> GetCartTotalAmountAsync(int cartId);
        Task<bool> IsCartValidAsync(int cartId);
        Task<bool> IsCartEmptyAsync(int cartId);
        Task<ShoppingCart?> GetLastModifiedCartAsync(int userId);
        Task MergeGuestCartToUserAsync(Guid guestIdentifier, int userId);
        Task<ShoppingCart?> GetActiveCartByUserIdAsync(int userId);
        Task<ShoppingCart?> GetCartByGuestIdAsync(Guid guestIdentifier);
        Task<bool> IsProductInCartAsync(int cartId, int productId);
        Task<IReadOnlyList<CartItem>> GetCartItemsAsync(int cartId);
        Task AddItemToCartAsync(int cartId, CartItem item);
        Task RemoveItemFromCartAsync(int cartId, int cartItemId);
        Task UpdateCartItemQuantityAsync(int cartId, int cartItemId, int newQuantity);
        Task ClearCartAsync(int cartId);
        Task<decimal> CalculateCartTotalAsync(int cartId);
        Task<CartQuoteDto> GetCartQuoteAsync(int cartId, int? shippingMethodId = null, string? couponCode = null, string? city = null, string? district = null);
        Task<bool> AreAllItemsInStockAsync(int cartId);
        Task<ShoppingCart?> GetCartBySessionIdAsync(int sessionId);
        Task<IReadOnlyList<CartItemDto>> GetCartItemsWithProductInfoAsync(int cartId);
        Task<IReadOnlyList<DiscountResult>> ApplyCartCampaignsAsync(int cartId);

        // Ek iş kuralı metodları
        Task<bool> ValidateCartForCheckoutAsync(int cartId);
        Task<ShoppingCart> CreateOrGetActiveCartAsync(int? userId = null, Guid? guestIdentifier = null);
        Task<IReadOnlyList<ShoppingCart>> GetAllCartsAsync();
        Task<ShoppingCart?> GetCartByIdAsync(int cartId);
        Task<bool> DeleteCartAsync(int cartId);
    }
}
