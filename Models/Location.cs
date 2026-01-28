namespace LastCallMotorAuctions.API.Models
{
    public class Location
    {
        public int LocationId { get; set; }
        public string City { get; set; } = null!;
        public string? Region { get; set; }
        public string Country { get; set; } = null!;
        public string? PostalCode { get; set; }
    }
}
