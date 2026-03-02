namespace LastCallMotorAuctions.API.Models
{
    public class SellerRequest
    {
        public int SellerRequestId { get; set; }
        public int UserId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? Notes { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public User? User { get; set; }
    }
}
