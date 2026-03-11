using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            ApplicationDbContext db,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public Task<PaymentSetupResponseDto> CreateSetupAsync(int buyerId)
        {
            // TODO: Call Stripe (or other provider) to:
            // - Create/find customer for buyerId
            // - Create a SetupIntent

            var fake = new PaymentSetupResponseDto
            {
                CustomerId = $"cus_test_{buyerId}",
                ClientSecret = "seti_test_secret"
            };

            return Task.FromResult(fake);
        }

        public async Task<PaymentPreauthResponseDto> CreatePreauthAsync(int buyerId, PaymentPreauthRequestDto request)
        {
            // TODO:
            // - Validate auction exists and amount > 0
            // - Create a PaymentIntent with amount/currency and metadata (buyerId, auctionId)
            // - Optionally store intent id + status in DB

            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            // Stubbed response for now:
            var result = new PaymentPreauthResponseDto
            {
                PaymentIntentId = $"pi_test_{buyerId}_{request.AuctionId}",
                ClientSecret = "pi_test_secret",
                Status = "requires_confirmation"
            };


            return await Task.FromResult(result);
        }

        public Task<PaymentStatusResponseDto> GetStatusAsync(int buyerId, int auctionId)
        {
            // TODO:
            // - Look up stored payment/preauth records for (buyerId, auctionId)
            // - Optionally check provider for latest status
            // For now, pretend they are cleared if a record exists.

            var status = new PaymentStatusResponseDto
            {
                AuctionId = auctionId,
                Cleared = true, // replace with real logic
                Reason = "stubbed_ok"
            };

            return Task.FromResult(status);
        }
    }
}