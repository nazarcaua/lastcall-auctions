using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListingsController : ControllerBase
    {
        private readonly IListingService _listingService;
        private readonly ILogger<ListingsController> _logger;

        public ListingsController(IListingService listingService, ILogger<ListingsController> logger)
        {
            _listingService = listingService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateListing([FromBody] CreateListingDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var sellerId))
                    return Unauthorized();

            try
            {
                var result = await _listingService.CreateListingAsync(dto, sellerId);
                return CreatedAtAction(nameof(GetHashCode), new { id = result.ListingId }, result);
            }

            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating listing");
                return StatusCode(500, new {message = "An error occurred while creating the listing."});
            }
        }

        // Get a single listing by ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetListing(int id)
        {
            var listing = await _listingService.GetListingByIdAsync(id);
            if (listing == null)
                return NotFound(new { message = "Listing not found." });
            return Ok(listing);
        }

        // Get listings, optionally filtered by sellerId and/or statusId
        [HttpGet]
        public async Task<IActionResult> GetListings([FromQuery] int? sellerId, [FromQuery] byte? statusId)
        {
            var list = await _listingService.GetListingsAsync(sellerId, statusId);
            return Ok(list);
        }
        

    }
}      
