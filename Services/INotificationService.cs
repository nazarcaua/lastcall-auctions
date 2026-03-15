using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface INotificationService
    {
        Task CreateAsync(int userId, string typeName, string title, string? message = null, int? listingId = null, int? auctionId = null);
        Task<List<NotificationDto>> GetForUserAsync(int userId);
        Task<bool> MarkReadAsync(long notificationId, int userId);
    }
}