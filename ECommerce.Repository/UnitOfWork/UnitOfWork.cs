using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using ECommerce.Repository.Repo;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction _transaction;

        public IUserIdentityRepository UserRepository { get; private set; }
       public IPaymentMethodRepository PaymentMethodRepository { get; private set; }
        public IOrderStatusHistoryRepository OrderStatusHistoryRepository { get; private set; }
        public IBlogPostRepository BlogPostRepository { get; private set; }
        public IBlogCategoryRepository BlogCategoryRepository { get; private set; }
        public IBlogPostCommentRepository BlogPostCommentRepository { get; private set; }

        public IBlogTagRepository BlogTagRepository { get; private set; }

        public IRoleIdentityRepository RoleRepository { get; private set; }
        public IProductRepository ProductRepository { get; private set; }
        public ICategoryRepository CategoryRepository { get; private set; }
        public IImageRepository ImageRepository { get; private set; }
        public IShoppingCartRepository ShoppingCartRepository { get; private set; }

        public IProductTagRepository ProductTagRepository { get; private set; }
        public IProductProductTagRepository ProductProductTagRepository { get; private set; }
        public IProductRelatedRepository ProductRelatedRepository { get; private set; }
        
        public IProductImageRepository ProductImageRepository { get; private set; }

        public IProductAttributeRepository ProductAttributeRepository { get; private set; }
        public IProductAttributeValueRepository ProductAttributeValueRepository { get; private set; }
        public IProductAttributeCombinationRepository ProductAttributeCombinationRepository { get; private set; }
        public IProductAttributeCombinationValueRepository ProductAttributeCombinationValueRepository { get; private set; }
        public IProductPriceRepository ProductPriceRepository { get; private set; }


        public UnitOfWork(ApplicationDbContext context, IProductRepository productRepository, ICategoryRepository categoryRepository,
            IImageRepository imageRepository, IProductAttributeRepository productAttributeRepository, IProductAttributeValueRepository productAttributeValueRepository,
            IProductAttributeCombinationRepository productAttributeCombinationRepository, IProductAttributeCombinationValueRepository productAttributeCombinationValueRepository,
            IProductTagRepository productTagRepository, IProductProductTagRepository productProductTagRepository,
            IProductRelatedRepository productRelatedRepository,
            IPaymentMethodRepository paymentMethodRepository , IOrderStatusHistoryRepository orderStatusHistoryRepository, IShoppingCartRepository shoppingCartRepository,
            IProductImageRepository productImageRepository, IUserIdentityRepository userIdentityRepository,IRoleIdentityRepository roleIdentityRepository,
            IProductPriceRepository productPriceRepository,IBlogPostRepository blogPostRepository,IBlogCategoryRepository blogCategoryRepository,
            IBlogPostCommentRepository blogPostCommentRepository,IBlogTagRepository blogTagRepository )
        {
            _context = context;
            BlogPostRepository = blogPostRepository;
            BlogCategoryRepository = blogCategoryRepository;
            BlogPostCommentRepository = blogPostCommentRepository;
            BlogTagRepository = BlogTagRepository;
            PaymentMethodRepository = paymentMethodRepository;
            OrderStatusHistoryRepository = orderStatusHistoryRepository;
            ShoppingCartRepository = shoppingCartRepository;
            UserRepository=userIdentityRepository;
            RoleRepository = roleIdentityRepository;
            ProductImageRepository = productImageRepository;
            ProductProductTagRepository = productProductTagRepository;
            ProductRelatedRepository = productRelatedRepository;
            ProductTagRepository = productTagRepository;
            ProductRepository = productRepository;
            CategoryRepository = categoryRepository;
            ImageRepository = imageRepository;
            ProductAttributeRepository = productAttributeRepository;
            ProductAttributeValueRepository = productAttributeValueRepository;
            ProductAttributeCombinationRepository = productAttributeCombinationRepository;
            ProductAttributeCombinationValueRepository = productAttributeCombinationValueRepository;
            ProductPriceRepository = productPriceRepository;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
            }
        }

        public async Task CommitAsync()
        {
            if (_transaction != null)
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            else
            {
                // transaction yoksa sadece SaveChanges yap
                await _context.SaveChangesAsync();

            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
