using ECommerce.Service.Abstract;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminShoppingCartController : ControllerBase
    {
        private readonly IShoppingCartService _shoppingCartService;
        public AdminShoppingCartController(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }

        // Tüm sepetleri listele
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShoppingCart>>> GetAllCarts()
        {
            var carts = await _shoppingCartService.GetAllCartsAsync();
            return Ok(DataResponse<IEnumerable<ShoppingCart>>.CreateSuccess(carts));
        }

        // Sepet detayını getir
        [HttpGet("{cartId}")]
        public async Task<ActionResult<ShoppingCartDto>> GetCartById(int cartId)
        {
            var cart = await _shoppingCartService.GetCartWithDetailsDtoAsync(cartId);
            if (cart == null)
                return NotFound(BaseResponse.CreateFailure("Sepet bulunamadı."));
            return Ok(DataResponse<ShoppingCartDto>.CreateSuccess(cart));
        }

        // Sepeti sil
        [HttpDelete("{cartId}")]
        public async Task<IActionResult> DeleteCart(int cartId)
        {
            var result = await _shoppingCartService.DeleteCartAsync(cartId);
            if (!result)
                return NotFound(BaseResponse.CreateFailure("Sepet bulunamadı veya silinemedi."));
            return Ok(BaseResponse.CreateSuccess("Sepet başarıyla silindi."));
        }
    }
}
