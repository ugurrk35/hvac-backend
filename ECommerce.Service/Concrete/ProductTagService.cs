using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class ProductTagService : Service<ProductTag>, IProductTagService
    {
        private readonly IProductTagRepository _productTagRepository;
        private readonly IUnitOfWork _unitOfWork;
        public ProductTagService(IUnitOfWork unitOfWork,IProductTagRepository productTagRepository) : base(productTagRepository,unitOfWork)
        {
            _productTagRepository = productTagRepository;
            _unitOfWork = unitOfWork;
        }

    

        public async Task<ProductTag?> GetTagWithProductsByIdAsync(int id)
        {
            return await _unitOfWork.ProductTagRepository.GetTagWithProductsByIdAsync(id);
        }
    }
}
