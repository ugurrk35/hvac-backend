using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository'ler
        IProductRepository ProductRepository { get; }
        IShoppingCartRepository ShoppingCartRepository { get; }

        IPaymentMethodRepository PaymentMethodRepository { get; }
        IOrderStatusHistoryRepository OrderStatusHistoryRepository { get; }
        IBlogPostRepository BlogPostRepository { get; }
        IBlogPostCommentRepository BlogPostCommentRepository { get; }
        IBlogCategoryRepository BlogCategoryRepository { get; }
        IBlogTagRepository BlogTagRepository { get; }
       
        ICategoryRepository CategoryRepository { get; }
        IImageRepository ImageRepository { get;  }
        IProductTagRepository ProductTagRepository { get; }
        IProductProductTagRepository ProductProductTagRepository { get; }
        IProductRelatedRepository ProductRelatedRepository { get; }
        IProductImageRepository ProductImageRepository { get; }
        IProductAttributeRepository ProductAttributeRepository { get;  }
        IProductAttributeValueRepository ProductAttributeValueRepository { get;  }
        IProductAttributeCombinationRepository ProductAttributeCombinationRepository { get; }
        IProductAttributeCombinationValueRepository ProductAttributeCombinationValueRepository { get;}
        IProductPriceRepository ProductPriceRepository { get; }

        // Identity repository'leri
        IUserIdentityRepository UserRepository { get; }
        IRoleIdentityRepository RoleRepository { get; }

        // Transaction yönetimi
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();

        // Değişiklikleri kaydet
        Task<int> CompleteAsync();
    }
}
