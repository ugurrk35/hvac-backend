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
    public class ProductAttributeValueService : Service<ProductAttributeValue>, IProductAttributeValueService
    {
        private readonly IProductAttributeValueRepository _productAttributeValueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductAttributeValueService(IProductAttributeValueRepository productAttributeValueRepository, IUnitOfWork unitOfWork)
            : base(productAttributeValueRepository, unitOfWork)
        {
            _productAttributeValueRepository = productAttributeValueRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ProductAttributeValue>> GetValuesByAttributeIdAsync(int attributeId)
        {
            return await _productAttributeValueRepository.GetValuesByAttributeIdAsync(attributeId);
        }

        public async Task<ProductAttributeValue?> GetValueWithAttributeAsync(int id)
        {
            return await _productAttributeValueRepository.GetValueWithAttributeAsync(id);
        }
    }
}
