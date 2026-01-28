using System;

namespace LastCallMotorAuctions.API.Models
{
    public class Auction
    {
        public int AuctionId { get; set; }
        public int ListingId { get; set; }
        public decimal StartPrice { get; set; }
        public decimal? ReservePrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public byte StatusId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Listing? Listing { get; set; }
        public AuctionStatus? Status { get; set; }
    }
}
