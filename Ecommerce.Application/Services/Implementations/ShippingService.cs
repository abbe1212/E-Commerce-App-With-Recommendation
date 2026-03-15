using Ecommerce.Application.DTOs.Shipping;
using Ecommerce.Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ecommerce.Application.Services.Implementations
{
    // This is a MOCK implementation for development and testing.
    public class ShippingService : IShippingService
    {
        public Task<decimal> CalculateShippingCostAsync(string fromAddress, string toAddress, decimal weight)
        {
            // Simulate a simple calculation: a base fee plus a fee per kilogram.
            decimal baseCost = 50.0m;
            decimal costPerKg = 10.0m;
            decimal totalCost = baseCost + (weight * costPerKg);
            return Task.FromResult(totalCost);
        }

        public decimal CalculateShippingCost(string shippingMethod)
        {
            return shippingMethod?.ToLower() switch
            {
                "express" => 150.0m,
                "free" => 0.0m,
                _ => 50.0m
            };
        }

        public Task<IEnumerable<ShippingProviderDto>> GetAvailableProvidersAsync()
        {
            // In a real app, this data would come from a database or configuration.
            var providers = new List<ShippingProviderDto>
            {
                new ShippingProviderDto { ProviderId = 1, Name = "Standard Shipping", BaseCost = 50.0m, EstimatedDeliveryDays = 5 },
                new ShippingProviderDto { ProviderId = 2, Name = "Express Shipping", BaseCost = 150.0m, EstimatedDeliveryDays = 2 }
            };
            return Task.FromResult<IEnumerable<ShippingProviderDto>>(providers);
        }

        public async Task<ShippingQuoteDto> GetShippingQuoteAsync(ShippingRequestDto request)
        {
            var cost = await CalculateShippingCostAsync(request.FromAddress, request.ToAddress, request.WeightInKg);

            var quote = new ShippingQuoteDto
            {
                ProviderId = 1,
                ProviderName = "Standard Shipping (Mock)",
                ServiceLevel = "Standard",
                TotalCost = cost,
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(5),
                CurrencyCode = "EGP"
            };

            return quote;
        }

        public Task<TrackingInfoDto> GetTrackingInfoAsync(string trackingNumber)
        {
            // Simulate fetching tracking info for any given tracking number.
            var info = new TrackingInfoDto
            {
                TrackingNumber = trackingNumber,
                ProviderName = "Standard Shipping (Mock)",
                CurrentStatus = "In Transit",
                StatusDescription = "Your package has left the sorting facility.",
                LastUpdated = DateTime.UtcNow.AddHours(-4),
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(3),
                TrackingHistory = new List<TrackingEventDto>
                {
                    new TrackingEventDto { EventDate = DateTime.UtcNow.AddDays(-1), Location = "Cairo, EG", Description = "Package has been picked up by the carrier." },
                    new TrackingEventDto { EventDate = DateTime.UtcNow.AddHours(-8), Location = "Cairo Sorting Center", Description = "Package is being processed at the sorting facility." }
                }
            };

            return Task.FromResult(info);
        }
    }
}