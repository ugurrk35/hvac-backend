using ECommerce.API.Controllers;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ECommerce.Domain.Entity;
using FluentAssertions;

namespace ECommerce.Tests.ProductAttribute
{
    public class ProductAttributeControllerTests
    {
        private readonly Mock<IProductAttributeService> _serviceMock;
        private readonly ProductAttributeController _controller;

        public ProductAttributeControllerTests()
        {
            _serviceMock = new Mock<IProductAttributeService>();
            _controller = new ProductAttributeController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnListOfProductAttributeDto()
        {
            // Arrange
            var attributes = new List<ECommerce.Domain.Entity.ProductAttribute> { new ECommerce.Domain.Entity.ProductAttribute { Id = 1, Name = "Color" } };
            _serviceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(attributes);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as DataResponse<List<ProductAttributeDto>>;
            response.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetById_ExistingId_ShouldReturnAttribute()
        {
            var attribute = new ECommerce.Domain.Entity.ProductAttribute { Id = 1, Name = "Color" };
            _serviceMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(attribute);

            var result = await _controller.GetById(1);

            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as DataResponse<ProductAttributeDto>;
            response.Data.Name.Should().Be("Color");
        }

        [Fact]
        public async Task GetById_InvalidId_ShouldReturnNotFound()
        {
            _serviceMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((ECommerce.Domain.Entity.ProductAttribute)null);

            var result = await _controller.GetById(999);

            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_ValidDto_ShouldReturnCreatedDto()
        {
            var createDto = new ProductAttributeCreateDto { Name = "Size" };
            var entity = new ECommerce.Domain.Entity.ProductAttribute { Id = 1, Name = "Size" };
            _serviceMock.Setup(s => s.AddAsync(It.IsAny<ECommerce.Domain.Entity.ProductAttribute>())).ReturnsAsync(entity);

            var result = await _controller.Create(createDto);

            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as DataResponse<ProductAttributeDto>;
            response.Data.Id.Should().Be(1);
        }

        //[Fact]
        //public async Task Update_ValidDto_ShouldReturnUpdatedDto()
        //{
        //    var updateDto = new ProductAttributeUpdateDto { Id = 1, Name = "Updated" };
        //    var entity = new ECommerce.Domain.Entity.ProductAttribute { Id = 1, Name = "Updated" };
        //    var dto = new ProductAttributeDto { Id = 1, Name = "Updated" };

        //    _mapperMock.Setup(m => m.Map<ECommerce.Domain.Entity.ProductAttribute>(updateDto)).Returns(entity);
        //    _serviceMock
        //        .Setup(s => s.UpdateAsync(It.IsAny<ECommerce.Domain.Entity.ProductAttribute>()))
        //        .Returns(Task.CompletedTask); // No ReturnsAsync, just Task

        //    _mapperMock.Setup(m => m.Map<ProductAttributeDto>(entity)).Returns(dto);

        //    var result = await _controller.Update(updateDto);

        //    var okResult = result.Result as OkObjectResult;
        //    var response = okResult.Value as DataResponse<ProductAttributeDto>;
        //    response.Data.Name.Should().Be("Updated");
        //}

        [Fact]
        public async Task Delete_ValidId_ShouldReturnSuccess()
        {
            var productAttributeEntity = new ECommerce.Domain.Entity.ProductAttribute
            {
                Id = 1,
                Name = "Color",
                IsPersonalizationText = false,
                TextPrompt = "",
                MaxLength = null
            };
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(productAttributeEntity);

            var result = await _controller.Delete(1);

          
            var objectResult = result.Result as OkObjectResult;
            var response = objectResult.Value as BaseResponse;

            response.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_InvalidId_ShouldReturnNotFound()
        {
            // Arrange: Service returns null for GetByIdAsync
            _serviceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((ECommerce.Domain.Entity.ProductAttribute)null);

            // Act
            var result = await _controller.Delete(999);

            // Assert
            var notFound = result.Result as NotFoundObjectResult;
            notFound.Should().NotBeNull();
        }

        [Fact]
        public async Task GetWithValues_ShouldReturnAttributesWithValues()
        {
            var attributes = new List<ECommerce.Domain.Entity.ProductAttribute> { new ECommerce.Domain.Entity.ProductAttribute { Id = 1, Name = "Color" } };
            _serviceMock.Setup(x => x.GetAttributesWithValuesAsync()).ReturnsAsync(attributes);

            var result = await _controller.GetAttributesWithValues();

            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as DataResponse<List<ProductAttributeDto>>;
            response.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetWithValuesById_ShouldReturnAttribute()
        {
            var attr = new ECommerce.Domain.Entity.ProductAttribute { Id = 1, Name = "Color" };
            _serviceMock.Setup(x => x.GetAttributeWithValuesByIdAsync(1)).ReturnsAsync(attr);

            var result = await _controller.GetAttributeWithValuesById(1);

            var ok = result.Result as OkObjectResult;
            var data = ok.Value as DataResponse<ProductAttributeDto>;
            data.Data.Name.Should().Be("Color");
        }

        [Fact]
        public async Task GetPersonalizationAttributes_ShouldReturnList()
        {
            var attributes = new List<ECommerce.Domain.Entity.ProductAttribute> { new ECommerce.Domain.Entity.ProductAttribute { Id = 1, Name = "Note", IsPersonalizationText = true } };
            _serviceMock.Setup(x => x.GetPersonalizationAttributesAsync()).ReturnsAsync(attributes);

            var result = await _controller.GetPersonalizationAttributes();

            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as DataResponse<List<ProductAttributeDto>>;
            response.Data[0].IsPersonalizationText.Should().BeTrue();
        }
    }
}
