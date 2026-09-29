using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ECommerce.Domain.Entity
{
    public class Order : AuditableEntity
    {
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public Guid? GuestIdentifier { get; set; }
        
        public string? OrderNumber { get; set; }
        public string? CargoTracking { get; set; }

        //msaifir alışverişi yaptıysa kayıt olmadan
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int ShoppingCartId { get; set; }
        public ShoppingCart ShoppingCart { get; set; }
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal CampaignDiscountTotal { get; set; }
        public string? AppliedCampaignIds { get; set; }
        public int OrderStatusId { get; set; }
        public string Notes { get; set; }


        // OrderStatusId ile ilişkilendirilmiş enum
        public OrderStatus OrderStatus
        {
            get { return (OrderStatus)OrderStatusId; }
            set { OrderStatusId = (int)value; }
        }

        public int? PaymentMethodId { get; set; }

        [ForeignKey("PaymentMethodId")]
        public PaymentMethod? PaymentMethod { get; set; }

       
        public int? ShippingMethodId { get; set; }

        [ForeignKey("ShippingMethodId")]
        public ShippingMethod? ShippingMethod { get; set; }

        public ICollection<OrderItem> OrdersItems { get; set; }
        public int? PaymentStatusId { get; set; }

        public PaymentStatus? PaymentStatus
        {
            get { return (PaymentStatus?)PaymentStatusId; }
            set { PaymentStatusId = (int?)value; }
        }

        public string? PaymentReference { get; set; } // PayTR veya başka sağlayıcılardan gelen referans
        /// <summary>PayTR'ye gönderilen benzersiz merchant_oid. Sağlayıcı callback ve iade işlemlerinde kullanılır.</summary>
        public string? PaytrMerchantOid { get; set; }
        /// <summary>Başarılı ödeme e-postasının SMTP tarafından kabul edildiği zaman.</summary>
        public DateTime? PaymentEmailSentAt { get; set; }

        // Siparişler fiziksel olarak silinmez. Arşive alınan kaydın nedeni, zamanı ve işlemi yapan
        // yönetici burada saklanır; IsDeleted alanı da aktif sipariş sorgularından gizlemek için kullanılır.
        public string? ArchiveReason { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public int? ArchivedByUserId { get; set; }

        public Address? ShippingAddress { get; set; }

        public Address? BillingAddress { get; set; }
        // Business logic
        public bool CanBeCancelled => OrderStatus == OrderStatus.Pending || OrderStatus == OrderStatus.Processing;
        public bool CanBeReturned => OrderStatus == OrderStatus.Delivered && CreatedAt.AddDays(14) >= DateTime.UtcNow;


    }
}
