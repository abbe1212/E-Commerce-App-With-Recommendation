using Ecommerce.Application.DTOs.Promotion;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Enums;
using Ecommerce.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Ecommerce.Application.Services.Implementations
{
    /// <summary>
    /// Service for managing promo code validation and application.
    /// </summary>
    public class PromoCodeService : IPromoCodeService
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PromoCodeService(IPromoCodeRepository promoCodeRepository, IUnitOfWork unitOfWork)
        {
            _promoCodeRepository = promoCodeRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets promo code details by its code string.
        /// </summary>
        public async Task<PromoCodeDto> GetPromoCodeByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var promoCode = await _promoCodeRepository.GetPromoCodeByCodeAsync(code.ToUpper().Trim());
            
            if (promoCode == null)
                return null;

            return new PromoCodeDto
            {
                PromoCodeID = promoCode.PromoCodeID,
                Code = promoCode.Code,
                Description = promoCode.Description,
                DiscountType = promoCode.DiscountType,
                DiscountValue = promoCode.DiscountValue,
                MaxUsage = promoCode.MaxUsage,
                UsedCount = promoCode.UsedCount,
                ExpirationDate = promoCode.ExpirationDate,
                IsActive = promoCode.IsActive,
                CreatedAt = promoCode.CreatedAt
            };
        }

        /// <summary>
        /// Validates if a promo code is valid for use.
        /// </summary>
        public async Task<bool> ValidatePromoCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var promoCode = await _promoCodeRepository.GetPromoCodeByCodeAsync(code.ToUpper().Trim());

            if (promoCode == null)
                return false;

            // Check if code is active
            if (!promoCode.IsActive)
                return false;

            // Check if code has expired
            if (promoCode.ExpirationDate < DateTime.UtcNow)
                return false;

            // Check if code has reached maximum usage
            if (promoCode.UsedCount >= promoCode.MaxUsage)
                return false;

            return true;
        }

        /// <summary>
        /// Applies a promo code to an order total and returns the discounted amount.
        /// </summary>
        public async Task<decimal> ApplyPromoCodeAsync(string code, decimal orderTotal)
        {
            if (string.IsNullOrWhiteSpace(code) || orderTotal <= 0)
                return 0;

            var promoCode = await _promoCodeRepository.GetPromoCodeByCodeAsync(code.ToUpper().Trim());

            if (promoCode == null || !await ValidatePromoCodeAsync(code))
                return 0;

            decimal discount = 0;

            switch (promoCode.DiscountType)
            {
                case DiscountType.Percentage:
                    // Calculate percentage discount
                    discount = orderTotal * (promoCode.DiscountValue / 100);
                    break;

                case DiscountType.FixedAmount:
                    // Apply fixed amount discount
                    discount = promoCode.DiscountValue;
                    // Ensure discount doesn't exceed order total
                    if (discount > orderTotal)
                        discount = orderTotal;
                    break;

                default:
                    discount = 0;
                    break;
            }

            return Math.Round(discount, 2);
        }

        /// <summary>
        /// Marks a promo code as used (increments usage count).
        /// </summary>
        public async Task UsePromoCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            var promoCode = await _promoCodeRepository.GetPromoCodeByCodeAsync(code.ToUpper().Trim());

            if (promoCode == null)
                return;

            await _promoCodeRepository.IncrementUsageCountAsync(promoCode.PromoCodeID);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
