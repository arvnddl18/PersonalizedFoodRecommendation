using Microsoft.AspNetCore.Mvc;
using Capstone.Models;
using Capstone.Data;
using Microsoft.EntityFrameworkCore;
using static Capstone.Models.NomsaurModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly DialogflowService _dialogflowService;
        private readonly AppDbContext _context;
        private readonly UserBehaviorService _behaviorService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(AppDbContext context, ILogger<ChatController> logger)
        {
            _context = context;
            _logger = logger;
            // Replace "your-project-id" with your actual Dialogflow project ID
            _dialogflowService = new DialogflowService("extreme-braid-457405-r9", "session-1", context);
            _behaviorService = new UserBehaviorService(context);
        }
        
        // List chat sessions for the current user, with lightweight metadata
        [HttpGet("sessions")]        
        public async Task<IActionResult> GetSessions()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized("User is not logged in.");
            }

            var sessions = await _context.ChatSessions
                .Where(s => s.UserId == userId.Value)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new
                {
                    s.SessionId,
                    s.StartedAt,
                    s.EndedAt,
                    LastMessage = _context.ChatMessages
                        .Where(m => m.SessionId == s.SessionId)
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => m.MessageText)
                        .FirstOrDefault(),
                    MessageCount = _context.ChatMessages.Count(m => m.SessionId == s.SessionId)
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // Get messages for a specific session (owned by current user)
        [HttpGet("sessions/{sessionId}/messages")]        
        public async Task<IActionResult> GetSessionMessages(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized("User is not logged in.");
            }

            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null || session.UserId != userId.Value)
            {
                return NotFound();
            }

            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { m.MessageId, m.Sender, m.MessageText, m.Timestamp })
                .ToListAsync();

            return Ok(new { session.SessionId, session.StartedAt, session.EndedAt, messages });
        }

        // MIGRATION HELPER ENDPOINT: Populate interaction metrics for existing patterns
        [HttpPost("populate-interaction-metrics/{userId}")]
        public async Task<IActionResult> PopulateInteractionMetrics(int userId)
        {
            try
            {
                await _behaviorService.PopulateInteractionMetricsForExistingPatterns(userId);
                return Ok(new { success = true, message = $"Interaction metrics populated for user {userId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error populating interaction metrics for user {userId}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error populating interaction metrics" });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
        {
            if (message?.Text == null)
                return BadRequest("Message text cannot be null");
            try
            {
                var swTotal = Stopwatch.StartNew();
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    // Guest mode: allow generalized chat without creating sessions, logging, or personalization
                    var guestResponse = await _dialogflowService.SendMessageAsync(
                        message.Text,
                        null, // no user id for guests
                        null,
                        false // health/diet restrictions disabled for guests
                    );

                    return Ok(new
                    {
                        response = guestResponse.ResponseText,
                        parameters = guestResponse.ExtractedParameters,
                        sessionId = (int?)null
                    });
                }

                // Clean up old sessions (older than 24 hours)
                var swCleanup = Stopwatch.StartNew();
                await CleanupOldSessions(userId.Value);
                swCleanup.Stop();
                _logger.LogInformation("Chat.SendMessage CleanupOldSessions took {ms} ms", swCleanup.ElapsedMilliseconds);

                // Ensure an open chat session exists or create one
                var swSession = Stopwatch.StartNew();
                var session = await _context.ChatSessions
                    .OrderByDescending(s => s.SessionId)
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.EndedAt == null);
                if (session == null)
                {
                    session = new ChatSession
                    {
                        UserId = userId.Value,
                        StartedAt = DateTime.UtcNow
                    };
                    _context.ChatSessions.Add(session);
                    await _context.SaveChangesAsync();
                }
                swSession.Stop();
                _logger.LogInformation("Chat.SendMessage ensure/open session took {ms} ms", swSession.ElapsedMilliseconds);

                // Log user message
                var userMsg = new Capstone.Models.NomsaurModel.ChatMessage
                {
                    SessionId = session.SessionId,
                    Sender = "user",
                    MessageText = message.Text,
                    Timestamp = DateTime.UtcNow
                };
                var swLogUser = Stopwatch.StartNew();
                _context.ChatMessages.Add(userMsg);
                await _context.SaveChangesAsync();
                swLogUser.Stop();
                _logger.LogInformation("Chat.SendMessage log user message took {ms} ms", swLogUser.ElapsedMilliseconds);

                var swDF = Stopwatch.StartNew();
                var dialogflowResponse = await _dialogflowService.SendMessageAsync(message.Text, userId.Value, null, message.IncludeHealthDietRestrictions);
                swDF.Stop();
                _logger.LogInformation("Chat.SendMessage DialogflowService.SendMessageAsync took {ms} ms", swDF.ElapsedMilliseconds);

                // Log AI message
                var aiMsg = new Capstone.Models.NomsaurModel.ChatMessage
                {
                    SessionId = session.SessionId,
                    Sender = "ai",
                    MessageText = dialogflowResponse.ResponseText,
                    Timestamp = DateTime.UtcNow,
                    IntentDetected = dialogflowResponse.ExtractedParameters.IntentName,
                    ParametersJson = JsonSerializer.Serialize(dialogflowResponse.ExtractedParameters)
                };
                var swLogAi = Stopwatch.StartNew();
                _context.ChatMessages.Add(aiMsg);
                await _context.SaveChangesAsync();
                swLogAi.Stop();
                _logger.LogInformation("Chat.SendMessage log AI message took {ms} ms", swLogAi.ElapsedMilliseconds);

                // Store user behavior
                if (userId.HasValue)
                {
                    var behaviorData = new
                    {
                        Intent = dialogflowResponse.ExtractedParameters.IntentName,
                        Response = dialogflowResponse.ResponseText,
                        Timestamp = DateTime.UtcNow
                    };

                    var behaviorJson = JsonSerializer.Serialize(behaviorData);
                    
                    // Track user interaction
                    await _behaviorService.TrackUserInteraction(
                        userId.Value,
                        "conversation",
                        message.Text,
                        dialogflowResponse.ResponseText,
                        dialogflowResponse.ExtractedParameters.IntentName,
                        behaviorJson
                    );
                }

                swTotal.Stop();
                _logger.LogInformation("Chat.SendMessage total took {ms} ms", swTotal.ElapsedMilliseconds);
                return Ok(new { 
                    response = dialogflowResponse.ResponseText,
                    parameters = dialogflowResponse.ExtractedParameters,
                    sessionId = session.SessionId
                });
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                _logger.LogError(ex, "Error in Chatbot API");
                return StatusCode(500, new { error = ex.ToString() });
            }
        }
        
        // Proactive: Get top food-type suggestions for the current user (behavior-based)
        [HttpGet("proactive-foods")]
        public async Task<IActionResult> GetProactiveFoodSuggestions([FromQuery] int top = 8, [FromQuery] bool includeHealthDietRestrictions = true)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized("User is not logged in.");
                }

                // Use behavior service to compute food preference rankings with empty query (context-free)
                var rankings = await _behaviorService.GenerateFoodPreferenceRankings(
                    userId.Value,
                    string.Empty,
                    null,
                    null,
                    Math.Min(Math.Max(top, 1), 20), // clamp 1..20
                    includeHealthDietRestrictions,
                    null
                );

                var payload = rankings.Select(r => new {
                    food = r.FoodName,
                    cuisine = r.CuisineType,
                    score = r.TotalScore,
                    reason = r.Reason,
                    safe = r.IsSafeForUser
                }).ToList();

                return Ok(new { items = payload });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating proactive food suggestions");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("Chat/insights/{userId}")]
        public async Task<IActionResult> GetUserInsights(int userId)
        {
            try
            {
                var insights = await _dialogflowService.GetUserInsights(userId);
                return Ok(insights);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("recommendation/select")]
        public async Task<IActionResult> SelectRecommendation([FromBody] RecommendationSelection selection)
        {
            try
            {
                await _dialogflowService.MarkRecommendationSelected(
                    selection.UserId, 
                    selection.EstablishmentName ?? string.Empty, 
                    selection.Rating, 
                    selection.Feedback
                );
                return Ok(new { message = "Recommendation selected successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("recommendation/select-self")]
        public async Task<IActionResult> SelectRecommendationSelf([FromBody] SelfSelection selection)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized("User is not logged in.");
                }

                await _dialogflowService.MarkRecommendationSelected(
                    userId.Value,
                    selection.EstablishmentName ?? string.Empty,
                    selection.Rating,
                    selection.Feedback
                );
                return Ok(new { message = "Recommendation selected successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("recommendation/track")]
        public async Task<IActionResult> TrackRecommendation([FromBody] RecommendationTracking tracking)
        {
            try
            {
                await _dialogflowService.TrackRecommendation(
                    tracking.UserId,
                    tracking.EstablishmentName ?? string.Empty,
                    tracking.CuisineType ?? string.Empty,
                    tracking.PriceRange ?? string.Empty,
                    tracking.Reason ?? string.Empty,
                    tracking.EstablishmentInfo
                );
                return Ok(new { message = "Recommendation tracked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("recommendation/track-self")]
        public async Task<IActionResult> TrackRecommendationSelf([FromBody] SelfTracking tracking)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized("User is not logged in.");
                }

                await _dialogflowService.TrackRecommendation(
                    userId.Value,
                    tracking.EstablishmentName ?? string.Empty,
                    tracking.CuisineType ?? string.Empty,
                    tracking.PriceRange ?? string.Empty,
                    tracking.Reason ?? string.Empty,
                    tracking.EstablishmentInfo
                );
                return Ok(new { message = "Recommendation tracked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("recommendations/{userId}")]
        public async Task<IActionResult> GetPersonalizedRecommendations(int userId, [FromQuery] string query = "", 
            [FromQuery] decimal? latitude = null, [FromQuery] decimal? longitude = null)
        {
            try
            {
                // Use the new food preference ranking algorithm directly
                var rankings = await _behaviorService.GenerateFoodPreferenceRankings(
                    userId,
                    query,
                    latitude,
                    longitude,
                    10,
                    true,
                    null);

                var payload = rankings.Select(r => new {
                    food = r.FoodName,
                    cuisine = r.CuisineType,
                    score = r.TotalScore,
                    reason = r.Reason,
                    safe = r.IsSafeForUser
                }).ToList();

                return Ok(new { items = payload });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("end-session")]
        public async Task<IActionResult> EndCurrentSession()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized("User is not logged in.");
                }

                var session = await _context.ChatSessions
                    .OrderByDescending(s => s.SessionId)
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.EndedAt == null);

                if (session != null)
                {
                    session.EndedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Session ended successfully" });
                }

                return Ok(new { message = "No active session found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending chat session");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("food-favorite")]
        public async Task<IActionResult> AddFoodToFavorites([FromBody] AddFoodFavoriteRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized("User not logged in");
                }

                // Find food type by name
                var foodType = await _context.FoodTypes
                    .FirstOrDefaultAsync(ft => ft.Name.Equals(request.FoodName, StringComparison.OrdinalIgnoreCase));
                
                if (foodType == null)
                {
                    return NotFound(new { message = "Food type not found" });
                }

                // Check if already exists in UserFoodTypes
                var existing = await _context.UserFoodTypes
                    .FirstOrDefaultAsync(uft => uft.UserId == userId.Value && uft.FoodTypeId == foodType.FoodTypeId);

                if (existing == null)
                {
                    var favorite = new UserFoodType
                    {
                        UserId = userId.Value,
                        FoodTypeId = foodType.FoodTypeId,
                        PreferenceLevel = "Preferred",
                        PreferenceScore = 9, // High score for favorites
                        AddedAt = DateTime.Now,
                        LastSelected = DateTime.Now
                    };
                    _context.UserFoodTypes.Add(favorite);
                }
                else
                {
                    // Update existing to favorite
                    existing.PreferenceLevel = "Preferred";
                    existing.PreferenceScore = 9;
                    existing.LastSelected = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"{request.FoodName} added to favorites!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding food to favorites");
                return StatusCode(500, "Internal server error");
            }
        }

        // Guest-friendly: randomized proactive foods without prescriptive personalization
        [HttpGet("proactive-foods-guest")]
        public async Task<IActionResult> GetProactiveFoodSuggestionsGuest([FromQuery] int top = 8)
        {
            try
            {
                // Pull randomized food types from catalog; use in-memory shuffle for provider compatibility
                var max = Math.Min(Math.Max(top, 1), 20);
                var all = await _context.FoodTypes
                    .Select(ft => new { ft.Name, ft.CuisineType })
                    .ToListAsync();

                var items = all
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(max)
                    .Select(ft => new
                    {
                        food = ft.Name,
                        cuisine = ft.CuisineType,
                        score = (decimal?)null,
                        reason = "Popular pick",
                        safe = (bool?)null
                    })
                    .ToList();

                return Ok(new { items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating guest proactive food suggestions");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("food-favorite/remove")]
        public async Task<IActionResult> RemoveFoodFromFavorites([FromBody] AddFoodFavoriteRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized("User not logged in");
                }

                // Find food type by name
                var foodType = await _context.FoodTypes
                    .FirstOrDefaultAsync(ft => ft.Name.Equals(request.FoodName, StringComparison.OrdinalIgnoreCase));
                
                if (foodType == null)
                {
                    return NotFound(new { message = "Food type not found" });
                }

                var favorite = await _context.UserFoodTypes
                    .FirstOrDefaultAsync(uft => uft.UserId == userId.Value && uft.FoodTypeId == foodType.FoodTypeId);

                if (favorite != null)
                {
                    _context.UserFoodTypes.Remove(favorite);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = $"{request.FoodName} removed from favorites!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing food from favorites");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task CleanupOldSessions(int userId)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-24);
                var oldSessions = await _context.ChatSessions
                    .Where(s => s.UserId == userId && s.EndedAt == null && s.StartedAt < cutoffTime)
                    .ToListAsync();

                foreach (var session in oldSessions)
                {
                    session.EndedAt = DateTime.UtcNow;
                }

                if (oldSessions.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Cleaned up {oldSessions.Count} old sessions for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cleaning up old sessions for user {userId}");
            }
        }






    }

    public class ChatMessage
    {
        public string? Text { get; set; }
        public bool IncludeHealthDietRestrictions { get; set; } = true; // Default to true for backwards compatibility
        public int? SessionId { get; set; } // Optional: for future routing to a specific open session
    }

    public class RecommendationSelection
    {
        public int UserId { get; set; }
        public string? EstablishmentName { get; set; }
        public int? Rating { get; set; }
        public string? Feedback { get; set; }
    }

    public class RecommendationTracking
    {
        public int UserId { get; set; }
        public string? EstablishmentName { get; set; }
        public string? CuisineType { get; set; }
        public string? PriceRange { get; set; }
        public string? Reason { get; set; }
        public string? EstablishmentInfo { get; set; }
    }

    public class SelfSelection
    {
        public string? EstablishmentName { get; set; }
        public int? Rating { get; set; }
        public string? Feedback { get; set; }
    }

    public class SelfTracking
    {
        public string? EstablishmentName { get; set; }
        public string? CuisineType { get; set; }
        public string? PriceRange { get; set; }
        public string? Reason { get; set; }
        public string? EstablishmentInfo { get; set; }
    }

    public class AddFoodFavoriteRequest
    {
        public string FoodName { get; set; } = string.Empty;
    }



    public class DialogflowAdminController : Controller
    {
        [HttpGet("/setup-dialogflow")]
        public async Task<IActionResult> SetupDialogflow()
        {
            var setup = new DialogflowSetup("extreme-braid-457405-r9");
            await setup.SetupAllIntentsAsync();
            return Content("Dialogflow setup complete! Both FoodRecommendation and PersonalizedFoodRecommendation intents have been created.");
        }

        [HttpGet("/setup-personalized-intent")]
        public async Task<IActionResult> SetupPersonalizedIntent()
        {
            var setup = new DialogflowSetup("extreme-braid-457405-r9");
            await setup.SetupPersonalizedFoodRecommendationAsync();
            return Content("Personalized Food Recommendation intent created successfully!");
        }
    }
} 