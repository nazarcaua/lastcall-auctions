using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Services;
using Microsoft.AspNetCore.Authorization;


namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "BuyerOnly")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        private int GetBuyerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim))
            {
                throw new UnauthorizedAccessException("User id not found in token.");
            }

            return int.Parse(idClaim);
        }

        [HttpPost("setup")]
        [ProducesResponseType(typeof(PaymentSetupResponseDto), 200)]
        public async Task<IActionResult> Setup()
        {
            try
            {
                var buyerId = GetBuyerId();
                var result = await _paymentService.CreateSetupAsync(buyerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment setup intent");
                return StatusCode(500, new { message = "Error creating payment setup." });
            }
        }

        [HttpPost("preauth")]
        [ProducesResponseType(typeof(PaymentPreauthRequestDto), 200)]
        public async Task<IActionResult> Preauth([FromBody] PaymentPreauthRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var buyerId = GetBuyerId();
                var result = await _paymentService.CreatePreauthAsync(buyerId, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment preauth");
                return StatusCode(500, new { message = "Error creating payment pre-authorization." });
            }
        }

        [HttpGet("status")]
        [ProducesResponseType(typeof(PaymentStatusResponseDto), 200)]
        public async Task<IActionResult> Status([FromQuery] int auctionId)
        {
            if (auctionId <= 0)
                return BadRequest(new { message = "auctionId is required." });

            try
            {
                var buyerId = GetBuyerId();
                var status = await _paymentService.GetStatusAsync(buyerId, auctionId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for auction {AuctionId}", auctionId);
                return StatusCode(500, new { message = "Error checking payment status." });
            }
        }
    }
}
