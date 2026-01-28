using System;

namespace LastCallMotorAuctions.API.Models
{
    public class Payment
    {
        public long PaymentId { get; set; }
        public int AuctionId { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = null!;
        public int MethodId { get; set; }
        public byte StatusId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public Auction? Auction { get; set; }
    }
}
