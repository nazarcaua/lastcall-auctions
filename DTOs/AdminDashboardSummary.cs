namespace LastCallMotorAuctions.API.DTOs
{
    public class AdminDashboardSummaryDto
    {
        public int PendingSellerRequests { get; set; }
        public int PendingListings { get; set; }
        public int LiveAuctions { get; set; }
    }
}
