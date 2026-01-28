using System;

namespace LastCallMotorAuctions.API.Models
{
    public class Bid
    {
        public long BidId { get; set; }
        public int AuctionId { get; set; }
        public int BidderId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

        public Auction? Auction { get; set; }
        public User? Bidder { get; set; }
    }
}
