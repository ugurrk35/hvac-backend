using ECommerce.Domain.Dtos;
using ECommerce.Service.Dtos.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos
{
    public class ShoppingCartDto
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsOrdered { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public UserDto? User { get; set; }
        public List<CartItemDto> CartItems { get; set; } = new();
    }
}
