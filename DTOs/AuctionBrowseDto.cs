namespace LastCallMotorAuctions.API.DTOs
{
    /// <summary>
    /// Used for browse list and auction detail: auction + listing summary + current bid.
    /// </summary>
    public class AuctionBrowseDto
    {
        public int AuctionId { get; set; }
        public int ListingId { get; set; }
        public string Title { get; set; }
        public short Year { get; set; }
        public string MakeName { get; set; }
        public string ModelName { get; set; }
        public decimal StartPrice { get; set; }
        public decimal? ReservePrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public byte StatusId { get; set; }
        public string? StatusName { get; set; }
        // Current highest bid, or null if no bids yet
        public decimal? CurrentBid { get; set; }
    }
}
