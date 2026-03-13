using Xunit;
using Moq;
using FluentAssertions;
using Ecoomerce.Web.Controllers;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Tests.Controllers
{
    public class CheckoutControllerTests
    {
        private static ClaimsPrincipal CreateUser(string userId) =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));

        [Fact]
        public async Task OrderComplete_WrongUser_ReturnsForbid()
        {
            // Arrange
            var mockCartService = new Mock<ICartService>();
            var mockProductService = new Mock<IProductService>();
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<CheckoutController>>();
            var mockActivityLogService = new Mock<IActivityLogService>();
            var mockPromoCodeService = new Mock<IPromoCodeService>();
            var mockShippingService = new Mock<IShippingService>();
            var mockEmailSenderService = new Mock<IEmailSenderService>();
            
            var controller = new CheckoutController(
                mockCartService.Object, 
                mockProductService.Object, 
                mockOrderService.Object,
                mockLogger.Object, 
                mockActivityLogService.Object, 
                mockPromoCodeService.Object,
                mockShippingService.Object, 
                mockEmailSenderService.Object);
            
            var user = CreateUser("user-A");
            controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext { User = user } 
            };
            
            mockOrderService.Setup(s => s.GetOrderDetailsAsync(1))
                .ReturnsAsync(new OrderDetailsDto { OrderID = 1, UserID = "user-B" });
            
            // Act
            var result = await controller.OrderComplete(1);
            
            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task OrderComplete_ValidUser_ReturnsView()
        {
            // Arrange
            var mockCartService = new Mock<ICartService>();
            var mockProductService = new Mock<IProductService>();
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<CheckoutController>>();
            var mockActivityLogService = new Mock<IActivityLogService>();
            var mockPromoCodeService = new Mock<IPromoCodeService>();
            var mockShippingService = new Mock<IShippingService>();
            var mockEmailSenderService = new Mock<IEmailSenderService>();
            
            var controller = new CheckoutController(
                mockCartService.Object, 
                mockProductService.Object, 
                mockOrderService.Object,
                mockLogger.Object, 
                mockActivityLogService.Object, 
                mockPromoCodeService.Object,
                mockShippingService.Object, 
                mockEmailSenderService.Object);
            
            var user = CreateUser("user-A");
            controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext { User = user } 
            };
            
            var orderDetails = new OrderDetailsDto { OrderID = 1, UserID = "user-A" };
            mockOrderService.Setup(s => s.GetOrderDetailsAsync(1))
                .ReturnsAsync(orderDetails);
            
            // Act
            var result = await controller.OrderComplete(1);
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult?.Model.Should().Be(orderDetails);
        }

        [Fact]
        public async Task OrderComplete_OrderNotFound_ReturnsNotFound()
        {
            // Arrange
            var mockCartService = new Mock<ICartService>();
            var mockProductService = new Mock<IProductService>();
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<CheckoutController>>();
            var mockActivityLogService = new Mock<IActivityLogService>();
            var mockPromoCodeService = new Mock<IPromoCodeService>();
            var mockShippingService = new Mock<IShippingService>();
            var mockEmailSenderService = new Mock<IEmailSenderService>();
            
            var controller = new CheckoutController(
                mockCartService.Object, 
                mockProductService.Object, 
                mockOrderService.Object,
                mockLogger.Object, 
                mockActivityLogService.Object, 
                mockPromoCodeService.Object,
                mockShippingService.Object, 
                mockEmailSenderService.Object);
            
            var user = CreateUser("user-A");
            controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext { User = user } 
            };
            
            mockOrderService.Setup(s => s.GetOrderDetailsAsync(999))
                .ReturnsAsync((OrderDetailsDto?)null);
            
            // Act
            var result = await controller.OrderComplete(999);
            
            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
