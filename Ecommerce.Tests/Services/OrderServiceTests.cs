using Xunit;
using Moq;
using FluentAssertions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Entities;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Core.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Tests.Services
{
    public class OrderServiceTests
    {
        [Fact]
        public async Task CreateOrderAsync_HappyPath_ReturnsOrder()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockOrderRepo = new Mock<IOrderRepository>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            var mockLogger = new Mock<ILogger<OrderService>>();

            var cart = new Cart
            {
                CartID = 1,
                UserID = "user1",
                Items = new List<CartItem>
                {
                    new CartItem { CartItemID = 1, ProductID = 1, Quantity = 2 }
                }
            };

            var products = new List<Product>
            {
                new Product { ProductID = 1, Name = "Test Product", Price = 50m, StockQuantity = 10, IsAvailable = true }
            };

            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockProductRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(products);
            mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            mockProductRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            mockCartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
            mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>())).Returns(new OrderDto { OrderID = 1, TotalAmount = 108m });

            var service = new OrderService(mockUnitOfWork.Object, mockOrderRepo.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object, mockLogger.Object);

            // Act
            var result = await service.CreateOrderAsync("user1", "123 Street", "CreditCard");

            // Assert
            result.Should().NotBeNull();
            result.TotalAmount.Should().Be(108m);
            mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
            mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_InsufficientStock_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockOrderRepo = new Mock<IOrderRepository>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            var mockLogger = new Mock<ILogger<OrderService>>();

            var cart = new Cart
            {
                CartID = 1,
                UserID = "user1",
                Items = new List<CartItem>
                {
                    new CartItem { CartItemID = 1, ProductID = 1, Quantity = 5 }
                }
            };

            var products = new List<Product>
            {
                new Product { ProductID = 1, Name = "Test Product", Price = 50m, StockQuantity = 2, IsAvailable = true }
            };

            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockProductRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(products);
            mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var service = new OrderService(mockUnitOfWork.Object, mockOrderRepo.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object, mockLogger.Object);

            // Act
            Func<Task> act = async () => await service.CreateOrderAsync("user1", "123 Street", "CreditCard");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*out of stock*");
            mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelOrderAsync_RestoresStock()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockOrderRepo = new Mock<IOrderRepository>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            var mockLogger = new Mock<ILogger<OrderService>>();

            var order = new Order
            {
                OrderID = 1,
                UserID = "user1",
                Status = OrderStatus.pending,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { OrderItemID = 1, ProductID = 1, Quantity = 3, UnitPrice = 50m }
                }
            };

            var products = new List<Product>
            {
                new Product { ProductID = 1, Name = "Test Product", Price = 50m, StockQuantity = 7, IsAvailable = true }
            };

            mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
            mockProductRepo.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(products);
            mockProductRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            mockOrderRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var service = new OrderService(mockUnitOfWork.Object, mockOrderRepo.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object, mockLogger.Object);

            // Act
            await service.CancelOrderAsync(1);

            // Assert
            mockProductRepo.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.StockQuantity == 10)), Times.Once);
            mockOrderRepo.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.cancelled)), Times.Once);
            mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelOrderAsync_AlreadyCancelled_NoOp()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockOrderRepo = new Mock<IOrderRepository>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            var mockLogger = new Mock<ILogger<OrderService>>();

            var order = new Order
            {
                OrderID = 1,
                UserID = "user1",
                Status = OrderStatus.cancelled,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { OrderItemID = 1, ProductID = 1, Quantity = 3, UnitPrice = 50m }
                }
            };

            mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

            var service = new OrderService(mockUnitOfWork.Object, mockOrderRepo.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object, mockLogger.Object);

            // Act
            await service.CancelOrderAsync(1);

            // Assert
            mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
            mockProductRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
            mockOrderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
    }
}
