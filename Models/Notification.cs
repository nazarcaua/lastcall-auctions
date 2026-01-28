using System;

namespace LastCallMotorAuctions.API.Models
{
    public class Notification
    {
        public long NotificationId { get; set; }
        public int UserId { get; set; }
        public short TypeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public int? AuctionId { get; set; }
        public int? ListingId { get; set; }
        public long? BidId { get; set; }
        public long? PaymentId { get; set; }

        public User? User { get; set; }
    }
}
