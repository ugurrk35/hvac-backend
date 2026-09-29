using System.Text.Json;
using ECommerce.API.Controllers.Admin;
using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.UserDtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECommerce.Tests.Security;

public class AdminAuthControllerTests
{
    [Fact]
    public async Task GetAllUsers_ReturnsSafeUserDtos()
    {
        var service = new Mock<IUserService>();
        service.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ApplicationUser>
        {
            new() { Id = 1, Email = "user@example.test", FirstName = "Ada", PasswordHash = "must-not-leak", SecurityStamp = "must-not-leak" }
        });
        var controller = new AuthController(service.Object);

        var result = await controller.GetAllUsers();

        var response = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<List<UserDto>>(response.Value);
        Assert.Single(users);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(response.Value));
        Assert.DoesNotContain("SecurityStamp", JsonSerializer.Serialize(response.Value));
    }
}
