using Xunit;
using Moq;
using FluentAssertions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Enums;

namespace Ecommerce.Tests.Services
{
    public class PromoCodeServiceTests
    {
        [Fact]
        public async Task ValidatePromoCodeAsync_ValidCode_ReturnsTrue()
        {
            // Arrange
            var mockRepo = new Mock<IPromoCodeRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var promoCode = new PromoCode
            {
                Code = "SAVE10",
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                UsedCount = 0,
                MaxUsage = 10
            };

            mockRepo.Setup(r => r.GetPromoCodeByCodeAsync("SAVE10")).ReturnsAsync(promoCode);

            var service = new PromoCodeService(mockRepo.Object, mockUnitOfWork.Object);

            // Act
            var result = await service.ValidatePromoCodeAsync("SAVE10");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePromoCodeAsync_ExpiredCode_ReturnsFalse()
        {
            // Arrange
            var mockRepo = new Mock<IPromoCodeRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var promoCode = new PromoCode
            {
                Code = "EXPIRED10",
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddDays(-1),
                UsedCount = 0,
                MaxUsage = 10
            };

            mockRepo.Setup(r => r.GetPromoCodeByCodeAsync("EXPIRED10")).ReturnsAsync(promoCode);

            var service = new PromoCodeService(mockRepo.Object, mockUnitOfWork.Object);

            // Act
            var result = await service.ValidatePromoCodeAsync("EXPIRED10");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidatePromoCodeAsync_MaxUsageReached_ReturnsFalse()
        {
            // Arrange
            var mockRepo = new Mock<IPromoCodeRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var promoCode = new PromoCode
            {
                Code = "MAXED10",
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                UsedCount = 10,
                MaxUsage = 10
            };

            mockRepo.Setup(r => r.GetPromoCodeByCodeAsync("MAXED10")).ReturnsAsync(promoCode);

            var service = new PromoCodeService(mockRepo.Object, mockUnitOfWork.Object);

            // Act
            var result = await service.ValidatePromoCodeAsync("MAXED10");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ApplyPromoCodeAsync_PercentageDiscount_CalculatesCorrectly()
        {
            // Arrange
            var mockRepo = new Mock<IPromoCodeRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var promoCode = new PromoCode
            {
                Code = "PERCENT20",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 20,
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                UsedCount = 0,
                MaxUsage = 10
            };

            mockRepo.Setup(r => r.GetPromoCodeByCodeAsync("PERCENT20")).ReturnsAsync(promoCode);

            var service = new PromoCodeService(mockRepo.Object, mockUnitOfWork.Object);

            // Act
            var discount = await service.ApplyPromoCodeAsync("PERCENT20", 100m);

            // Assert
            discount.Should().Be(20m);
        }

        [Fact]
        public async Task ApplyPromoCodeAsync_FixedDiscount_CapsAtOrderTotal()
        {
            // Arrange
            var mockRepo = new Mock<IPromoCodeRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var promoCode = new PromoCode
            {
                Code = "FIXED200",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 200,
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                UsedCount = 0,
                MaxUsage = 10
            };

            mockRepo.Setup(r => r.GetPromoCodeByCodeAsync("FIXED200")).ReturnsAsync(promoCode);

            var service = new PromoCodeService(mockRepo.Object, mockUnitOfWork.Object);

            // Act
            var discount = await service.ApplyPromoCodeAsync("FIXED200", 100m);

            // Assert
            discount.Should().Be(100m);
        }
    }
}
