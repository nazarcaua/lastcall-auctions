namespace LastCallMotorAuctions.API.DTOs
{
    /// <summary>
    /// Used for browse list and auction detail: auction + listing summary + current bid.
    /// </summary>
    public class AuctionBrowseDto
    {
        public int AuctionId { get; set; }
        public int ListingId { get; set; }
        public string Title { get; set; } = null!;
        public short Year { get; set; }
        public string MakeName { get; set; } = null!;
        public string ModelName { get; set; } = null!;
        public decimal StartPrice { get; set; }
        public decimal? ReservePrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public byte StatusId { get; set; }
        public string? StatusName { get; set; }
        // Current highest bid, or null if no bids yet
        public decimal? CurrentBid { get; set; }

        // Optional grouping
        public int? AuctionGroupId { get; set; }
        public string? AuctionGroupTitle { get; set; }

        // Listing photo URLs
        public List<string> PhotoUrls { get; set; } = new();

        // Vehicle details
        public string? Description { get; set; }
        public string? Vin { get; set; }
        public int? Mileage { get; set; }
        public byte ConditionGrade { get; set; }

        // Location details
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }
}
