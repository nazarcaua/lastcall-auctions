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

    }
}
