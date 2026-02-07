namespace LastCallMotorAuctions.API.DTOs
{
    public class UpdateAuctionDto
    {
        public decimal? StartPrice { get; set; }
        public decimal? ReservePrice { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
