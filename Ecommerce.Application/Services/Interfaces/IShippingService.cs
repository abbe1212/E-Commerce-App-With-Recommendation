using Ecommerce.Application.DTOs.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface IShippingService
    {
        Task<decimal> CalculateShippingCostAsync(string fromAddress, string toAddress, decimal weight);
        Task<IEnumerable<ShippingProviderDto>> GetAvailableProvidersAsync();
        Task<ShippingQuoteDto> GetShippingQuoteAsync(ShippingRequestDto request);
        Task<TrackingInfoDto> GetTrackingInfoAsync(string trackingNumber);
        decimal CalculateShippingCost(string shippingMethod);
    }
}
