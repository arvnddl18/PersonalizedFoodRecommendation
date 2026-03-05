using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using Capstone.Models;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantOwnerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly OwnerVerificationService _verificationService;
        private readonly ILogger<RestaurantOwnerController> _logger;

        public RestaurantOwnerController(AppDbContext context, ILogger<RestaurantOwnerController> logger)
        {
            _context = context;
            _logger = logger;
            _verificationService = new OwnerVerificationService(context);
        }

        // GET: api/RestaurantOwner/my-restaurants
        [HttpGet("my-restaurants")]
        public async Task<ActionResult<List<RestaurantOwner>>> GetMyRestaurants()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var restaurants = await _verificationService.GetUserRestaurants(userId.Value);
                return Ok(restaurants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user restaurants");
                return StatusCode(500, new { message = "Error retrieving user restaurants" });
            }
        }

        // GET: api/RestaurantOwner/owner/{placeId}
        [HttpGet("owner/{placeId}")]
        public async Task<ActionResult<RestaurantOwner>> GetOwner(string placeId)
        {
            try
            {
                var owner = await _verificationService.GetOwnerByPlaceId(placeId);
                if (owner == null)
                {
                    return NotFound(new { message = "No owner found for this restaurant" });
                }

                return Ok(owner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving owner");
                return StatusCode(500, new { message = "Error retrieving owner" });
            }
        }

        // GET: api/RestaurantOwner/check/{placeId}
        [HttpGet("check/{placeId}")]
        public async Task<ActionResult<bool>> CheckIfClaimed(string placeId)
        {
            try
            {
                var isClaimed = await _verificationService.IsRestaurantClaimed(placeId);
                return Ok(new { isClaimed });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking claim status");
                return StatusCode(500, new { message = "Error checking claim status" });
            }
        }

        // POST: api/RestaurantOwner/claim
        [HttpPost("claim")]
        public async Task<ActionResult<RestaurantOwner>> ClaimRestaurant([FromBody] ClaimRestaurantRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var owner = await _verificationService.ClaimRestaurant(
                    request.PlaceId,
                    userId.Value,
                    request.RestaurantName,
                    request.BusinessName,
                    request.BusinessEmail,
                    request.BusinessPhone,
                    request.BusinessAddress,
                    request.BusinessLicensePath,
                    request.AdditionalNotes
                );

                return Ok(owner);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming restaurant");
                return StatusCode(500, new { message = "Error claiming restaurant" });
            }
        }

        // GET: api/RestaurantOwner/verification/{ownerId}
        [HttpGet("verification/{ownerId}")]
        public async Task<ActionResult<OwnerVerification>> GetVerification(int ownerId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Check if user owns this verification
                var owner = await _context.RestaurantOwners
                    .FirstOrDefaultAsync(ro => ro.OwnerId == ownerId && ro.UserId == userId.Value);

                if (owner == null)
                {
                    return Forbid("You don't have access to this verification");
                }

                var verification = await _verificationService.GetVerification(ownerId);
                if (verification == null)
                {
                    return NotFound(new { message = "Verification not found" });
                }

                return Ok(verification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verification");
                return StatusCode(500, new { message = "Error retrieving verification" });
            }
        }

        // POST: api/RestaurantOwner/verify-email
        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                var success = await _verificationService.VerifyEmail(request.OwnerId, request.Email);
                if (!success)
                {
                    return BadRequest(new { message = "Email verification failed" });
                }

                return Ok(new { message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(500, new { message = "Error verifying email" });
            }
        }

        // POST: api/RestaurantOwner/verify-phone
        [HttpPost("verify-phone")]
        public async Task<ActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
        {
            try
            {
                var success = await _verificationService.VerifyPhone(request.OwnerId, request.Phone);
                if (!success)
                {
                    return BadRequest(new { message = "Phone verification failed" });
                }

                return Ok(new { message = "Phone verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying phone");
                return StatusCode(500, new { message = "Error verifying phone" });
            }
        }

        // Request models
        public class ClaimRestaurantRequest
        {
            public string PlaceId { get; set; } = string.Empty;
            public string RestaurantName { get; set; } = string.Empty;
            public string? BusinessName { get; set; }
            public string? BusinessEmail { get; set; }
            public string? BusinessPhone { get; set; }
            public string? BusinessAddress { get; set; }
            public string? BusinessLicensePath { get; set; }
            public string? AdditionalNotes { get; set; }
        }

        public class VerifyEmailRequest
        {
            public int OwnerId { get; set; }
            public string Email { get; set; } = string.Empty;
        }

        public class VerifyPhoneRequest
        {
            public int OwnerId { get; set; }
            public string Phone { get; set; } = string.Empty;
        }
    }
}

