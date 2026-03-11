using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentSetupResponseDto> CreateSetupAsync(int buyerId);
        Task<PaymentPreauthResponseDto> CreatePreauthAsync(int buyerId, PaymentPreauthRequestDto request);
        Task<PaymentStatusResponseDto> GetStatusAsync(int buyerId, int auctionId);
    }
}
