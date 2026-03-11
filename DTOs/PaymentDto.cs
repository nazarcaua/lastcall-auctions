namespace LastCallMotorAuctions.API.DTOs
{
    public class PaymentSetupResponseDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    public class PaymentPreauthRequestDto
    {
        public int AuctionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }

    public class PaymentPreauthResponseDto
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class PaymentStatusResponseDto
    {
        public int AuctionId { get; set; }
        public bool Cleared { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
