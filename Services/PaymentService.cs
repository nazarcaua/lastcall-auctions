using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public class PaymentService : IPaymentService
    {
        public Task<PaymentResponseDto> PreAuthorizePaymentAsync(PreAuthorizePaymentDto paymentDto, int userId)
        {
            // TODO: Implement payment pre-authorization
            throw new NotImplementedException();
        }

        public Task<PaymentResponseDto> ProcessFinalPaymentAsync(int auctionId, int userId)
        {
            // TODO: Implement final payment processing
            throw new NotImplementedException();
        }

        public Task<bool> ValidatePreAuthorizationAsync(int userId, int auctionId)
        {
            // TODO: Implement pre-authorization validation
            throw new NotImplementedException();
        }
    }
}
