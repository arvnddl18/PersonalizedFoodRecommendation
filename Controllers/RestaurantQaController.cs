using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantQaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RestaurantQaController> _logger;

        public RestaurantQaController(AppDbContext context, ILogger<RestaurantQaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{placeId}")]
        public async Task<ActionResult> GetQa(string placeId, [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(placeId))
                    return BadRequest(new { message = "placeId is required" });

                limit = Math.Clamp(limit, 1, 50);

                var questions = await _context.RestaurantQuestions
                    .AsNoTracking()
                    .Where(q => q.PlaceId == placeId)
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(limit)
                    .Select(q => new
                    {
                        q.QuestionId,
                        q.PlaceId,
                        q.QuestionText,
                        q.CreatedAt,
                        AskedBy = new { q.UserId, q.User.Name },
                        Answers = _context.RestaurantAnswers
                            .AsNoTracking()
                            .Where(a => a.QuestionId == q.QuestionId)
                            .OrderBy(a => a.CreatedAt)
                            .Select(a => new
                            {
                                a.AnswerId,
                                a.AnswerText,
                                a.CreatedAt,
                                a.IsVerified,
                                a.VerifiedByRole,
                                AnsweredBy = new { a.UserId, a.User.Name }
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(new { placeId, questions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving restaurant Q&A");
                return StatusCode(500, new { message = "Error retrieving restaurant Q&A" });
            }
        }

        [HttpPost("question")]
        public async Task<ActionResult> AskQuestion([FromBody] AskQuestionRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated" });

                if (string.IsNullOrWhiteSpace(request.PlaceId))
                    return BadRequest(new { message = "PlaceId is required" });

                var text = (request.QuestionText ?? string.Empty).Trim();
                if (text.Length < 5)
                    return BadRequest(new { message = "Question is too short." });
                if (text.Length > 800)
                    return BadRequest(new { message = "Question is too long." });

                var q = new RestaurantQuestion
                {
                    PlaceId = request.PlaceId.Trim(),
                    UserId = userId.Value,
                    QuestionText = text,
                    CreatedAt = DateTime.UtcNow
                };

                _context.RestaurantQuestions.Add(q);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    q.QuestionId,
                    q.PlaceId,
                    q.QuestionText,
                    q.CreatedAt,
                    AskedBy = new { UserId = userId.Value }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating restaurant question");
                return StatusCode(500, new { message = "Error creating question" });
            }
        }

        [HttpPost("answer")]
        public async Task<ActionResult> PostAnswer([FromBody] PostAnswerRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated" });

                if (request.QuestionId <= 0)
                    return BadRequest(new { message = "QuestionId is required" });

                var question = await _context.RestaurantQuestions
                    .FirstOrDefaultAsync(q => q.QuestionId == request.QuestionId);

                if (question == null)
                    return NotFound(new { message = "Question not found" });

                var text = (request.AnswerText ?? string.Empty).Trim();
                if (text.Length < 2)
                    return BadRequest(new { message = "Answer is too short." });
                if (text.Length > 1200)
                    return BadRequest(new { message = "Answer is too long." });

                // Determine if this user is Verified Owner for this place or Admin
                var isOwner = await _context.RestaurantOwners
                    .AnyAsync(ro => ro.PlaceId == question.PlaceId &&
                                    ro.UserId == userId.Value &&
                                    ro.VerificationStatus == "Verified");

                var isAdmin = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == userId.Value && ur.RoleType == "Admin");

                var verifiedRole = isOwner ? "Owner" : (isAdmin ? "Admin" : null);
                var isVerified = verifiedRole != null;

                var answer = new RestaurantAnswer
                {
                    QuestionId = question.QuestionId,
                    UserId = userId.Value,
                    AnswerText = text,
                    CreatedAt = DateTime.UtcNow,
                    IsVerified = isVerified,
                    VerifiedByRole = verifiedRole,
                    VerifiedAt = isVerified ? DateTime.UtcNow : null,
                    VerifiedByUserId = isVerified ? userId.Value : null
                };

                _context.RestaurantAnswers.Add(answer);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    answer.AnswerId,
                    answer.QuestionId,
                    answer.AnswerText,
                    answer.CreatedAt,
                    answer.IsVerified,
                    answer.VerifiedByRole,
                    AnsweredBy = new { UserId = userId.Value }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting restaurant answer");
                return StatusCode(500, new { message = "Error posting answer" });
            }
        }

        public class AskQuestionRequest
        {
            public string PlaceId { get; set; } = string.Empty;
            public string? QuestionText { get; set; }
        }

        public class PostAnswerRequest
        {
            public int QuestionId { get; set; }
            public string? AnswerText { get; set; }
        }
    }
}

