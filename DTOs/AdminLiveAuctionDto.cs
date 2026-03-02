namespace LastCallMotorAuctions.API.DTOs
{
    public class AdminLiveAuctionDto
    {
        public int AuctionId { get; set; }
        public string VehicleTitle { get; set; } = null!;
        public decimal CurrentBid { get; set; }
        public int ActiveUsers { get; set; }
        public decimal BidsPerMinute { get; set; }
    }
}
