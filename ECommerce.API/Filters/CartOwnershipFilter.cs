using ECommerce.Service.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ECommerce.API.Filters
{
    /// <summary>Requires the authenticated user or guest cart identifier to own a cart route.</summary>
    public sealed class CartOwnershipFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.RouteData.Values.TryGetValue("cartId", out var routeValue)
                || !int.TryParse(routeValue?.ToString(), out var cartId))
            {
                await next();
                return;
            }

            var carts = context.HttpContext.RequestServices.GetRequiredService<IShoppingCartService>();
            var cart = await carts.GetCartWithDetailsAsync(cartId);
            if (cart == null)
            {
                context.Result = new NotFoundObjectResult(new { message = "Sepet bulunamadı." });
                return;
            }

            var user = context.HttpContext.User;
            if (user.IsInRole("Admin"))
            {
                await next();
                return;
            }

            if (cart.UserId.HasValue)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
                if (user.Identity?.IsAuthenticated == true && userId == cart.UserId.Value.ToString())
                {
                    await next();
                    return;
                }
            }
            else if (cart.GuestIdentifier.HasValue
                && Guid.TryParse(context.HttpContext.Request.Headers["X-Guest-Identifier"], out var guestIdentifier)
                && guestIdentifier == cart.GuestIdentifier.Value)
            {
                await next();
                return;
            }

            context.Result = new ForbidResult();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireCartOwnershipAttribute : TypeFilterAttribute
    {
        public RequireCartOwnershipAttribute() : base(typeof(CartOwnershipFilter)) { }
    }
}
