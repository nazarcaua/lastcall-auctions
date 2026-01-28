namespace LastCallMotorAuctions.API.DTOs
{
    public class BidResponseDto
    {
        public long BidId { get; set; }
        public int AuctionId { get; set; }
        public int BidderId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PlacedAt { get; set; }
    }
}
