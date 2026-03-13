using Xunit;
using Moq;
using FluentAssertions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Entities;
using AutoMapper;

namespace Ecommerce.Tests.Services
{
    public class CartServiceTests
    {
        [Fact]
        public async Task AddItemToCartAsync_NewItem_AddsToCart()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            
            var cart = new Cart { CartID = 1, UserID = "user1", Items = new List<CartItem>() };
            var product = new Product { ProductID = 1, StockQuantity = 10, IsAvailable = true };
            
            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockProductRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
            mockCartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            
            var service = new CartService(mockUnitOfWork.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object);
            
            // Act
            await service.AddItemToCartAsync("user1", 1, 2);
            
            // Assert
            mockCartRepo.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.Any(i => i.ProductID == 1 && i.Quantity == 2))), Times.Once);
            mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddItemToCartAsync_ExistingItem_IncrementsQuantity()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            
            var existingItem = new CartItem { CartItemID = 1, ProductID = 1, Quantity = 3 };
            var cart = new Cart { CartID = 1, UserID = "user1", Items = new List<CartItem> { existingItem } };
            var product = new Product { ProductID = 1, StockQuantity = 10, IsAvailable = true };
            
            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockProductRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
            mockCartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            
            var service = new CartService(mockUnitOfWork.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object);
            
            // Act
            await service.AddItemToCartAsync("user1", 1, 2);
            
            // Assert
            mockCartRepo.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.First().Quantity == 5)), Times.Once);
            mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddItemToCartAsync_OutOfStock_ThrowsArgumentException()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            
            var cart = new Cart { CartID = 1, UserID = "user1", Items = new List<CartItem>() };
            var product = new Product { ProductID = 1, StockQuantity = 1, IsAvailable = true };
            
            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockProductRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
            
            var service = new CartService(mockUnitOfWork.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object);
            
            // Act
            Func<Task> act = () => service.AddItemToCartAsync("user1", 1, 5);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("productId");
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_ValidQty_Updates()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            
            var cartItem = new CartItem { CartItemID = 5, ProductID = 1, Quantity = 3 };
            var cart = new Cart { CartID = 1, UserID = "user1", Items = new List<CartItem> { cartItem } };
            
            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockCartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            
            var service = new CartService(mockUnitOfWork.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object);
            
            // Act
            await service.UpdateItemQuantityAsync("user1", 5, 7);
            
            // Assert
            mockCartRepo.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.First().Quantity == 7)), Times.Once);
            mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateItemQuantityAsync_ZeroQty_RemovesItem()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            
            var cartItem = new CartItem { CartItemID = 5, ProductID = 1, Quantity = 3 };
            var cart = new Cart { CartID = 1, UserID = "user1", Items = new List<CartItem> { cartItem } };
            
            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockCartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            
            var service = new CartService(mockUnitOfWork.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object);
            
            // Act
            await service.UpdateItemQuantityAsync("user1", 5, 0);
            
            // Assert
            mockCartRepo.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.Count == 0)), Times.Once);
            mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveItemFromCartAsync_RemovesItem()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            
            var cartItem = new CartItem { CartItemID = 5, ProductID = 1, Quantity = 2 };
            var cart = new Cart { CartID = 1, UserID = "user1", Items = new List<CartItem> { cartItem } };
            
            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockCartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            
            var service = new CartService(mockUnitOfWork.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object);
            
            // Act
            await service.RemoveItemFromCartAsync("user1", 5);
            
            // Assert
            mockCartRepo.Verify(r => r.UpdateAsync(It.Is<Cart>(c => !c.Items.Any(i => i.CartItemID == 5))), Times.Once);
            mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ClearCartAsync_EmptiesCart()
        {
            // Arrange
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockCartRepo = new Mock<ICartRepository>();
            var mockProductRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            
            var items = new List<CartItem>
            {
                new CartItem { CartItemID = 1, ProductID = 1, Quantity = 2 },
                new CartItem { CartItemID = 2, ProductID = 2, Quantity = 1 },
                new CartItem { CartItemID = 3, ProductID = 3, Quantity = 5 }
            };
            var cart = new Cart { CartID = 1, UserID = "user1", Items = items };
            
            mockCartRepo.Setup(r => r.GetCartByUserIdAsync("user1")).ReturnsAsync(cart);
            mockCartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
            mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            
            var service = new CartService(mockUnitOfWork.Object, mockCartRepo.Object, mockProductRepo.Object, mockMapper.Object);
            
            // Act
            await service.ClearCartAsync("user1");
            
            // Assert
            mockCartRepo.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.Count == 0)), Times.Once);
            mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
