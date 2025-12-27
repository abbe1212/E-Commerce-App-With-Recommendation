using Stripe;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Application.DTOs.Payment;
using Ecommerce.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Services.Implementations
{
    public class StripePaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentService> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StripePaymentService(
            IConfiguration configuration,
            ILogger<StripePaymentService> logger,
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _logger = logger;
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _unitOfWork = unitOfWork;

            // Set Stripe API key
            var secretKey = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(secretKey))
            {
                StripeConfiguration.ApiKey = secretKey;
            }
        }

        public async Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto paymentRequest)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(paymentRequest.OrderID);
                if (order == null || order.TotalAmount != paymentRequest.Amount)
                {
                    return new PaymentResultDto 
                    { 
                        IsSuccess = false, 
                        Message = "Invalid order or amount mismatch." 
                    };
                }

                // Check if Stripe is configured
                if (string.IsNullOrEmpty(_configuration["Stripe:SecretKey"]))
                {
                    _logger.LogWarning("Stripe is not configured. Using mock payment.");
                    return await ProcessMockPaymentAsync(paymentRequest);
                }

                // Create Stripe Payment Intent
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(paymentRequest.Amount * 100), // Stripe uses cents
                    Currency = "usd",
                    Description = $"Order #{paymentRequest.OrderID}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", paymentRequest.OrderID.ToString() }
                    },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                // If payment intent created successfully, save payment record
                if (paymentIntent != null)
                {
                    var payment = new Core.Entities.Payment
                    {
                        OrderID = paymentRequest.OrderID,
                        PaymentDate = DateTime.UtcNow,
                        Amount = paymentRequest.Amount,
                        PaymentStatus = Core.Enums.PaymentStatus.Pending,
                        TransactionID = paymentIntent.Id
                    };

                    await _paymentRepository.AddAsync(payment);
                    await _unitOfWork.SaveChangesAsync();

                    return new PaymentResultDto
                    {
                        IsSuccess = true,
                        PaymentId = payment.PaymentID,
                        Status = Core.Enums.PaymentStatus.Pending,
                        TransactionId = paymentIntent.Id,
                        Message = "Payment intent created successfully.",
                        ClientSecret = paymentIntent.ClientSecret // For frontend confirmation
                    };
                }

                return new PaymentResultDto 
                { 
                    IsSuccess = false, 
                    Message = "Failed to create payment intent." 
                };
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Stripe payment error for order {OrderId}", paymentRequest.OrderID);
                return new PaymentResultDto
                {
                    IsSuccess = false,
                    Status = Core.Enums.PaymentStatus.Failed,
                    Message = $"Payment failed: {stripeEx.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment for order {OrderId}", paymentRequest.OrderID);
                return new PaymentResultDto
                {
                    IsSuccess = false,
                    Status = Core.Enums.PaymentStatus.Failed,
                    Message = "An error occurred while processing your payment."
                };
            }
        }

        public async Task<PaymentResultDto> ConfirmPaymentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    // Update payment record
                    var payment = await _paymentRepository.GetByTransactionIdAsync(paymentIntentId);
                    if (payment != null)
                    {
                        payment.PaymentStatus = Core.Enums.PaymentStatus.Paid;
                        await _paymentRepository.UpdateAsync(payment);

                        // Update order status
                        var order = await _orderRepository.GetByIdAsync(payment.OrderID);
                        if (order != null)
                        {
                            order.Status = Core.Enums.OrderStatus.pending;
                            await _orderRepository.UpdateAsync(order);
                        }

                        await _unitOfWork.SaveChangesAsync();

                        return new PaymentResultDto
                        {
                            IsSuccess = true,
                            PaymentId = payment.PaymentID,
                            Status = Core.Enums.PaymentStatus.Paid,
                            TransactionId = paymentIntentId,
                            Message = "Payment confirmed successfully."
                        };
                    }
                }

                return new PaymentResultDto
                {
                    IsSuccess = false,
                    Message = $"Payment status: {paymentIntent.Status}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment {PaymentIntentId}", paymentIntentId);
                return new PaymentResultDto
                {
                    IsSuccess = false,
                    Message = "Failed to confirm payment."
                };
            }
        }

        // Fallback to mock payment if Stripe is not configured
        private async Task<PaymentResultDto> ProcessMockPaymentAsync(PaymentRequestDto paymentRequest)
        {
            var transactionId = $"MOCK_PMT_{Guid.NewGuid().ToString().ToUpper()}";

            var payment = new Core.Entities.Payment
            {
                OrderID = paymentRequest.OrderID,
                PaymentDate = DateTime.UtcNow,
                Amount = paymentRequest.Amount,
                PaymentStatus = Core.Enums.PaymentStatus.Paid,
                TransactionID = transactionId
            };

            await _paymentRepository.AddAsync(payment);

            var order = await _orderRepository.GetByIdAsync(paymentRequest.OrderID);
            if (order != null)
            {
                order.Status = Core.Enums.OrderStatus.pending;
                await _orderRepository.UpdateAsync(order);
            }

            await _unitOfWork.SaveChangesAsync();

            return new PaymentResultDto
            {
                IsSuccess = true,
                PaymentId = payment.PaymentID,
                Status = Core.Enums.PaymentStatus.Paid,
                TransactionId = transactionId,
                Message = "Payment was successfully simulated (Mock Mode)."
            };
        }
    }
}
