using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.InteropServices;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/admin/seller-requests")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSellerRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public AdminSellerRequestsController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
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
                return Ok(new { message = "Seller request rejected." });
            }
        }
    }
