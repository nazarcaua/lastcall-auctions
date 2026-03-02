using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/admin/listings")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminListingsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminListingsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<List<ListingResponseDto>>> GetPendingListings()
        {
            // Draft as pending
            var draftStatusId = await _db.ListingStatuses
                .Where(s => s.Name == "Draft")
                .Select(s => s.StatusId)
                .SingleAsync();

            var pending = await _db.Listings
                .Where(l => l.StatusId == draftStatusId)
                .Include(l => l.Location)
                .Select(l => new ListingResponseDto
                {
                    ListingId = l.ListingId,
                    SellerId = l.SellerId,
                    Title = l.Title,
                    Description = l.Description,
                    Year = l.Year,
                    MakeId = l.MakeId,
                    ModelId = l.ModelId,
                    Vin = l.Vin,
                    Mileage = l.Mileage,
                    ConditionGrade = l.ConditionGrade,
                    LocationId = l.LocationId,
                    StatusId = l.StatusId,
                    CreatedAt = l.CreatedAt,
                    City = l.Location!.City,
                    Region = l.Location!.Region,
                    Country = l.Location!.Country,
                    PostalCode = l.Location!.PostalCode,
                    PhotoUrls = new List<string>()
                })
                .ToListAsync();

            return Ok (pending);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approvallisting(int id)
        {
            var draftId = await _db.ListingStatuses
                .Where(s => s.Name == "Draft")
                .Select(s => s.StatusId)
                .SingleAsync();

            var activeId = await _db.ListingStatuses
                .Where(s => s.Name == "Active")
                .Select(s => s.StatusId)
                .SingleAsync();

            var listing = await _db.Listings.FindAsync(id);
            if (listing == null) return NotFound(new { message = "Listing not found." });
            if (listing.StatusId != draftId) return BadRequest(new { message = "Listing is not pending approval." });

            listing.StatusId = activeId;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Listing approved." });
        }

        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> RejectListing(int id, [FromBody] RejectListingRequest? body = null)
        {
            var draftId = await _db.ListingStatuses
                .Where(s => s.Name == "Draft")
                .Select(s => s.StatusId)
                .SingleAsync();

            var archivedId = await _db.ListingStatuses
                .Where(s => s.Name == "Archived")
                .Select(s => s.StatusId)
                .SingleAsync();

            var listing = await _db.Listings.FindAsync(id);
            if (listing == null) return NotFound(new { message = "Listing not found." });
            if (listing.StatusId != draftId) return BadRequest(new { message = "Listing is not pending approval." });

            listing.StatusId = archivedId;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Listing rejected." });
        }

    }
}
