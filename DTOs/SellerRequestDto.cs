namespace LastCallMotorAuctions.API.DTOs
{
    public class PendingSellerRequestDto
    {
        public int SellerRequestId { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ReviewSellerRequestDto
    {
        public string? Notes { get; set; }
    }
}
