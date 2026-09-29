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
    public class ProductAttributeCombinationValueService : Service<ProductAttributeCombinationValue>, IProductAttributeCombinationValueService
    {
        private readonly IProductAttributeCombinationValueRepository _productAttributeCombinationValueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductAttributeCombinationValueService(IProductAttributeCombinationValueRepository productAttributeCombinationValueRepository, IUnitOfWork unitOfWork)
            : base(productAttributeCombinationValueRepository, unitOfWork)
        {
            _productAttributeCombinationValueRepository = productAttributeCombinationValueRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ProductAttributeCombinationValue>> GetValuesByCombinationIdAsync(int combinationId)
        {
            return await _productAttributeCombinationValueRepository.GetValuesByCombinationIdAsync(combinationId);
        }

        public async Task<ProductAttributeCombinationValue?> GetValueWithDetailsAsync(int id)
        {
            return await _productAttributeCombinationValueRepository.GetValueWithDetailsAsync(id);
        }
    }
}
