using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IShoppingCartRepository : IRepository<ShoppingCart>

    {

        Task<ShoppingCart> GetCartWithItems(int cartId);

        Task<ShoppingCart?> GetCartByUserIdAsync(int userId);
        Task<ShoppingCart?> GetCartByGuestIdentifierAsync(Guid guestIdentifier);
        Task<bool> HasActiveCartAsync(int userId);
        Task<int> GetCartItemCountAsync(int cartId);
        Task<ShoppingCart?> GetLastModifiedCartAsync(int userId);

        Task MergeGuestCartToUserAsync(Guid guestIdentifier, int userId);
        Task<decimal> GetCartTotalAmountAsync(int cartId);
        Task<bool> IsCartValidAsync(int cartId);

        Task<bool> IsCartEmptyAsync(int cartId);
        Task<ShoppingCart?> GetCartWithDetailsAsync(int cartId);
        // Aktif sepet (user)
        Task<ShoppingCart?> GetActiveCartByUserIdAsync(int userId);

        // Aktif sepet (guest)
        Task<ShoppingCart?> GetCartByGuestIdAsync(Guid guestIdentifier);

        // Ürün sepette mi
        Task<bool> IsProductInCartAsync(int cartId, int productId);
        Task<CartItem?> GetCartItemByIdAsync(int cartId, int cartItemId);

        // Sepet öğeleri
        Task<IReadOnlyList<CartItem>> GetCartItemsAsync(int cartId);

        // Sepete ürün ekle
        Task AddItemToCartAsync(int cartId, CartItem item);

        // Sepetten ürün çıkar
        Task RemoveItemFromCartAsync(int cartId, int cartItemId);

        // Sepetteki ürün miktarını değiştir
        Task UpdateCartItemQuantityAsync(int cartId, int cartItemId, int newQuantity);

        // Sepeti boşalt
        Task ClearCartAsync(int cartId);

        // Sepet toplamını hesapla
        Task<decimal> CalculateCartTotalAsync(int cartId);

        // Sepet ürün stoklarını kontrol et (checkout öncesi)
        Task<bool> AreAllItemsInStockAsync(int cartId);

        // Oturum ID'sine göre sepet (mobil, session tabanlı senaryolar için)
        Task<ShoppingCart?> GetCartBySessionIdAsync(int sessionId);

        // Sepet ürünlerini DTO olarak getir
        Task<IReadOnlyList<CartItemDto>> GetCartItemsWithProductInfoAsync(int cartId);

        // Sepet ürünlerini kampanyalara göre değerlendir
        Task<IReadOnlyList<DiscountResult>> ApplyCartCampaignsAsync(int cartId);

        Task<IReadOnlyList<ShoppingCart>> GetAllCartsAsync();
        Task<ShoppingCart?> GetByIdAsync(int cartId);
        Task DeleteAsync(ShoppingCart cart);
    }

}
