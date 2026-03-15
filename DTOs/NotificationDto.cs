namespace LastCallMotorAuctions.API.DTOs
{
    public class NotificationDto
    {
        public long NotificationId { get; set; }
        public int UserId { get; set; }
        public short TypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? AuctionId { get; set; }
        public int? ListingId { get; set; }
    }
}