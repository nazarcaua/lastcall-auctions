using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> PreAuthorizePaymentAsync(PreAuthorizePaymentDto paymentDto, int userId);
        Task<PaymentResponseDto> ProcessFinalPaymentAsync(int auctionId, int userId);
        Task<bool> ValidatePreAuthorizationAsync(int userId, int auctionId);
    }
}
