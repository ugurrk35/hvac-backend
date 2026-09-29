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
    public class ProductAttributeService : Service<ProductAttribute>, IProductAttributeService
    {
        private readonly IProductAttributeRepository _productAttributeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductAttributeService(IProductAttributeRepository productAttributeRepository, IUnitOfWork unitOfWork)
            : base(productAttributeRepository, unitOfWork)
        {
            _productAttributeRepository = productAttributeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ProductAttribute>> GetAttributesWithValuesAsync()
        {
            return await _productAttributeRepository.GetAttributesWithValuesAsync();
        }

        public async Task<ProductAttribute?> GetAttributeWithValuesByIdAsync(int id)
        {
            return await _productAttributeRepository.GetAttributeWithValuesByIdAsync(id);
        }

        public async Task<IEnumerable<ProductAttribute>> GetPersonalizationAttributesAsync()
        {
            return await _productAttributeRepository.GetPersonalizationAttributesAsync();
        }
    }
}
