using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task CreateAsync(int userId, string typeName, string title, string? message = null, int? listingId = null, int? auctionId = null)
        {
            var typeId = await _db.NotificationTypes
                .Where(t => t.Name == typeName)
                .Select(t => t.TypeId)
                .FirstOrDefaultAsync();

            if (typeId == 0)
                typeId = 7; // fallback to SellerRequestApproved if name not found

            var notification = new Notification
            {
                UserId = userId,
                TypeId = typeId,
                Title = title,
                Message = message,
                ListingId = listingId,
                AuctionId = auctionId,
                IsRead = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task<List<NotificationDto>> GetForUserAsync(int userId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Join(
                    _db.NotificationTypes,
                    n => n.TypeId,
                    t => t.TypeId,
                    (n, t) => new { n, t.Name })
                .Select(x => new NotificationDto
                {
                    NotificationId = x.n.NotificationId,
                    UserId = x.n.UserId,
                    TypeId = x.n.TypeId,
                    TypeName = x.Name,
                    Title = x.n.Title,
                    Message = x.n.Message,
                    CreatedAt = x.n.CreatedAt,
                    IsRead = x.n.IsRead,
                    ReadAt = x.n.ReadAt,
                    AuctionId = x.n.AuctionId,
                    ListingId = x.n.ListingId
                })
                .ToListAsync();
        }

        public async Task<bool> MarkReadAsync(long notificationId, int userId)
        {
            var n = await _db.Notifications
                .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);
            if (n == null) return false;
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}