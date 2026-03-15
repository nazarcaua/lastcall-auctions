using System.Security.Claims;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id)) throw new UnauthorizedAccessException("User id not found.");
            return int.Parse(id);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<NotificationDto>), 200)]
        public async Task<IActionResult> GetMy()
        {
            var userId = GetUserId();
            var list = await _notificationService.GetForUserAsync(userId);
            return Ok(list);
        }

        [HttpPost("{id:long}/read")]
        public async Task<IActionResult> MarkRead(long id)
        {
            var userId = GetUserId();
            var ok = await _notificationService.MarkReadAsync(id, userId);
            if (!ok) return NotFound(new { message = "Notification not found or not yours." });
            return Ok(new { message = "Marked as read." });
        }
    }
}