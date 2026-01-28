using System;

namespace LastCallMotorAuctions.API.Models
{
    public class AuctionResult
    {
        public int AuctionId { get; set; }
        public long WinningBidId { get; set; }
        public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

        public Auction? Auction { get; set; }
        public Bid? WinningBid { get; set; }
    }
}
