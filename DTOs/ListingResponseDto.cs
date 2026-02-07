namespace LastCallMotorAuctions.API.DTOs
{
    public class ListingResponseDto
    {
        public int ListingId { get; set; }
        public int SellerId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public short Year { get; set; }
        public int MakeId { get; set; }
        public int ModelId { get; set; }
        public string? Vin { get; set; }
        public int? Mileage { get; set; }
        public byte ConditionGrade { get; set; }
        public int LocationId { get; set; }
        public byte StatusId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Location details for edit UI
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        // Photo URLs for this listing
        public List<string> PhotoUrls { get; set; } = new();
    }
}
