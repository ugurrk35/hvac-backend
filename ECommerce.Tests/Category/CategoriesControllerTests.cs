using ECommerce.API.Controllers;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.CategoryDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Domain.Entity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ECommerce.Tests.Category
{
    public class CategoriesControllerTests
    {
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            _categoryServiceMock = new Mock<ICategoryService>();
            _controller = new CategoriesController(_categoryServiceMock.Object);
        }

        [Fact]
        public async Task GetById_ShouldReturnCategory_WhenExists()
        {
            // Arrange
            var category = new ECommerce.Domain.Entity.Category { Id = 1, Name = "Test" };

            _categoryServiceMock.Setup(s => s.GetByIdWithProductsAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DataResponse<CategoryDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Test", response.Data.Name);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenCategoryIsNull()
        {
            // Arrange
            _categoryServiceMock.Setup(s => s.GetByIdWithProductsAsync(999))
                .ReturnsAsync((ECommerce.Domain.Entity.Category)null);

            // Act
            var result = await _controller.GetById(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<DataResponse<CategoryDto>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedCategory_WhenValid()
        {
            // Arrange
            var createDto = new CategoryCreateDto { Name = "New Category" };
            var category = new ECommerce.Domain.Entity.Category { Id = 1, Name = "New Category" };
            _categoryServiceMock.Setup(s => s.ValidateCategoryAsync(It.IsAny<ECommerce.Domain.Entity.Category>())).ReturnsAsync(new List<string>());
            _categoryServiceMock.Setup(s => s.CreateCategoryAsync(It.IsAny<ECommerce.Domain.Entity.Category>())).ReturnsAsync(category);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DataResponse<CategoryDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("New Category", response.Data.Name);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
        {
            // Arrange
            var createDto = new CategoryCreateDto { Name = "" };
            var category = new ECommerce.Domain.Entity.Category();
            var errors = new List<string> { "Name is required." };

            _categoryServiceMock.Setup(s => s.ValidateCategoryAsync(It.IsAny<ECommerce.Domain.Entity.Category>())).ReturnsAsync(errors);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<DataResponse<CategoryDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Name is required.", response.Errors);
        }

        [Fact]
        public async Task Delete_ShouldReturnSuccess_WhenCategoryIsDeleted()
        {
            // Arrange
            _categoryServiceMock.Setup(s => s.DeleteCategoryAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<BaseResponse>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
        {
            // Arrange
            _categoryServiceMock.Setup(s => s.DeleteCategoryAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<BaseResponse>(notFoundResult.Value);
            Assert.False(response.Success);
        }
    }
}
