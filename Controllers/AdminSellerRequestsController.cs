using System.Runtime.InteropServices;
using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/admin/seller-requests")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSellerRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public AdminSellerRequestsController(ApplicationDbContext db, UserManager<User> userManager, INotificationService notificationService)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<List<PendingSellerRequestDto>>> GetPending()
        {
            var pending = await _db.SellerRequests
            .Where(r => !r.IsApproved && !r.IsRejected)
            .Include(r => r.User)
            .Select(r => new PendingSellerRequestDto
            {
                SellerRequestId = r.SellerRequestId,
                UserId = r.UserId,
                Email = r.User!.Email!,
                FullName = r.User.FullName,
                SubmittedAt = r.SubmittedAt,
                Notes = r.Notes
            })
            .ToListAsync();

            return Ok(pending);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ReviewSellerRequestDto? body = null)
        {
            var req = await _db.SellerRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.SellerRequestId == id);

            if (req == null) return NotFound(new { message = "Request not found." });
            if (req.IsApproved || req.IsRejected)
                return BadRequest(new { message = "Request already processed." });

            req.IsApproved = true;
            req.Notes = body?.Notes ?? req.Notes;

            // Add user to Seller roles
            if (req.User != null)
            {
                var result = await _userManager.AddToRoleAsync(req.User, "Seller");
                if (!result.Succeeded)
                    return StatusCode(500, new { message = "Failed to assign Seller role" });
            }

            await _db.SaveChangesAsync();
            if (req.User != null)
                await _notificationService.CreateAsync(req.UserId, "SellerRequestApproved", "Seller request approved", "Your request to become a seller has been approved.");

            return Ok(new { message = "Seller request approved." });
        }

            [HttpPost("{id:int}/reject")]
            public async Task<IActionResult> Reject(int id, [FromBody] ReviewSellerRequestDto? body = null)
            {
                var req = await _db.SellerRequests.FirstOrDefaultAsync(r => r.SellerRequestId == id);
                if (req == null) return NotFound(new { message = "Request not found." });
                if (req.IsApproved || req.IsRejected)
                    return BadRequest(new { message = "Request already processed." });

                req.IsRejected = true;
                req.Notes = body?.Notes ?? req.Notes;

                await _db.SaveChangesAsync();
                var reqWithUser = await _db.SellerRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.SellerRequestId == id);
                if (reqWithUser?.User != null)
                    await _notificationService.CreateAsync(reqWithUser.UserId, "SellerRequestRejected", "Seller request rejected", body?.Notes ?? "Your request to become a seller was not approved.");
            return Ok(new { message = "Seller request rejected." });
            }
        }
    }
