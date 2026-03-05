using System.Text.Json;
using Capstone.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DietaryTagVotesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DietaryTagVotesController> _logger;
        private readonly IConfiguration _configuration;

        public DietaryTagVotesController(AppDbContext context, ILogger<DietaryTagVotesController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private int GetThreshold(int dietaryRestrictionId)
        {
            var defaultThreshold = _configuration.GetValue("DietaryTagVotes:DefaultThreshold", 3);

            // Optional override map: "DietaryTagVotes:Overrides": { "1": 5, "2": 3 }
            var overrides = _configuration
                .GetSection("DietaryTagVotes:Overrides")
                .Get<Dictionary<string, int>>() ?? new Dictionary<string, int>();

            return overrides.TryGetValue(dietaryRestrictionId.ToString(), out var overrideValue)
                ? overrideValue
                : defaultThreshold;
        }

        private async Task<(int voteCount, bool isTagActive)> GetVoteCountAndActiveAsync(string placeId, int dietaryRestrictionId)
        {
            var voteCount = await _context.CommunityDietaryTagVotes.CountAsync(v =>
                v.PlaceId == placeId && v.DietaryRestrictionId == dietaryRestrictionId);

            var isTagActive = voteCount >= GetThreshold(dietaryRestrictionId);
            return (voteCount, isTagActive);
        }

        // POST: api/DietaryTagVotes/vote
        [HttpPost("vote")]
        public async Task<IActionResult> Vote([FromBody] VoteDietaryTagRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                if (string.IsNullOrWhiteSpace(request.PlaceId))
                {
                    return BadRequest(new { message = "placeId is required" });
                }

                // Upsert cached place details
                var existingRestaurant = await _context.CommunityRestaurants
                    .FirstOrDefaultAsync(r => r.PlaceId == request.PlaceId);

                if (existingRestaurant == null)
                {
                    existingRestaurant = new CommunityRestaurant
                    {
                        PlaceId = request.PlaceId,
                        Name = request.Name ?? string.Empty,
                        Address = request.Address,
                        Rating = request.Rating,
                        PriceLevel = request.PriceLevel,
                        PhotoReference = request.PhotoReference,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        PhoneNumber = request.PhoneNumber,
                        Website = request.Website,
                        Types = request.Types != null ? JsonSerializer.Serialize(request.Types) : null,
                        LastUpdated = DateTime.UtcNow
                    };

                    _context.CommunityRestaurants.Add(existingRestaurant);
                }
                else
                {
                    // Update cache (only overwrite when provided)
                    if (!string.IsNullOrWhiteSpace(request.Name)) existingRestaurant.Name = request.Name;
                    if (!string.IsNullOrWhiteSpace(request.Address)) existingRestaurant.Address = request.Address;
                    if (request.Rating != null) existingRestaurant.Rating = request.Rating;
                    if (request.PriceLevel != null) existingRestaurant.PriceLevel = request.PriceLevel;
                    if (!string.IsNullOrWhiteSpace(request.PhotoReference)) existingRestaurant.PhotoReference = request.PhotoReference;
                    if (request.Latitude != null) existingRestaurant.Latitude = request.Latitude;
                    if (request.Longitude != null) existingRestaurant.Longitude = request.Longitude;
                    if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) existingRestaurant.PhoneNumber = request.PhoneNumber;
                    if (!string.IsNullOrWhiteSpace(request.Website)) existingRestaurant.Website = request.Website;
                    if (request.Types != null) existingRestaurant.Types = JsonSerializer.Serialize(request.Types);
                    existingRestaurant.LastUpdated = DateTime.UtcNow;
                }

                // Insert vote if not exists
                var existingVote = await _context.CommunityDietaryTagVotes.FirstOrDefaultAsync(v =>
                    v.UserId == userId.Value &&
                    v.PlaceId == request.PlaceId &&
                    v.DietaryRestrictionId == request.DietaryRestrictionId);

                if (existingVote == null)
                {
                    var vote = new CommunityDietaryTagVote
                    {
                        UserId = userId.Value,
                        PlaceId = request.PlaceId,
                        DietaryRestrictionId = request.DietaryRestrictionId,
                        VotedAt = DateTime.UtcNow
                    };

                    _context.CommunityDietaryTagVotes.Add(vote);
                }

                await _context.SaveChangesAsync();

                var (voteCount, isTagActive) = await GetVoteCountAndActiveAsync(request.PlaceId, request.DietaryRestrictionId);
                return Ok(new
                {
                    voteCount,
                    userHasVoted = true,
                    isTagActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting dietary tag");
                return StatusCode(500, new { message = "Error voting dietary tag" });
            }
        }

        // DELETE: api/DietaryTagVotes/unvote/{placeId}/{dietaryRestrictionId}
        [HttpDelete("unvote/{placeId}/{dietaryRestrictionId:int}")]
        public async Task<IActionResult> Unvote(string placeId, int dietaryRestrictionId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var existingVote = await _context.CommunityDietaryTagVotes.FirstOrDefaultAsync(v =>
                    v.UserId == userId.Value &&
                    v.PlaceId == placeId &&
                    v.DietaryRestrictionId == dietaryRestrictionId);

                if (existingVote != null)
                {
                    _context.CommunityDietaryTagVotes.Remove(existingVote);
                    await _context.SaveChangesAsync();
                }

                var (voteCount, isTagActive) = await GetVoteCountAndActiveAsync(placeId, dietaryRestrictionId);
                return Ok(new
                {
                    voteCount,
                    userHasVoted = false,
                    isTagActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unvoting dietary tag");
                return StatusCode(500, new { message = "Error unvoting dietary tag" });
            }
        }

        // POST: api/DietaryTagVotes/status
        // Body: { placeIds: [...], dietaryRestrictionIds: [...] }
        [HttpPost("status")]
        public async Task<IActionResult> GetStatus([FromBody] DietaryTagVoteStatusRequest request)
        {
            try
            {
                var placeIds = request.PlaceIds?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList() ?? new List<string>();
                var dietaryIds = request.DietaryRestrictionIds?.Distinct().ToList() ?? new List<int>();

                if (placeIds.Count == 0 || dietaryIds.Count == 0)
                {
                    return Ok(Array.Empty<object>());
                }

                var voteCounts = await _context.CommunityDietaryTagVotes
                    .Where(v => placeIds.Contains(v.PlaceId) && dietaryIds.Contains(v.DietaryRestrictionId))
                    .GroupBy(v => new { v.PlaceId, v.DietaryRestrictionId })
                    .Select(g => new { g.Key.PlaceId, g.Key.DietaryRestrictionId, VoteCount = g.Count() })
                    .ToListAsync();

                var countsMap = voteCounts.ToDictionary(
                    x => (x.PlaceId, x.DietaryRestrictionId),
                    x => x.VoteCount);

                var userId = HttpContext.Session.GetInt32("UserId");
                HashSet<(string placeId, int dietaryRestrictionId)> userVotes = new();
                if (userId != null)
                {
                    var voted = await _context.CommunityDietaryTagVotes
                        .Where(v => v.UserId == userId.Value && placeIds.Contains(v.PlaceId) && dietaryIds.Contains(v.DietaryRestrictionId))
                        .Select(v => new { v.PlaceId, v.DietaryRestrictionId })
                        .ToListAsync();

                    userVotes = voted.Select(v => (v.PlaceId, v.DietaryRestrictionId)).ToHashSet();
                }

                var result = new List<object>(placeIds.Count * dietaryIds.Count);
                foreach (var placeId in placeIds)
                {
                    foreach (var dietaryId in dietaryIds)
                    {
                        var voteCount = countsMap.TryGetValue((placeId, dietaryId), out var c) ? c : 0;
                        result.Add(new
                        {
                            placeId,
                            dietaryRestrictionId = dietaryId,
                            voteCount,
                            userHasVoted = userVotes.Contains((placeId, dietaryId)),
                            isTagActive = voteCount >= GetThreshold(dietaryId)
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dietary tag vote status");
                return StatusCode(500, new { message = "Error getting dietary tag vote status" });
            }
        }

        // GET: api/DietaryTagVotes/shops?dietaryRestrictionId=...
        [HttpGet("shops")]
        public async Task<IActionResult> GetShopsByTag([FromQuery] int dietaryRestrictionId)
        {
            try
            {
                if (dietaryRestrictionId <= 0)
                {
                    return BadRequest(new { message = "dietaryRestrictionId is required" });
                }

                var threshold = GetThreshold(dietaryRestrictionId);

                var activeVoteCounts = _context.CommunityDietaryTagVotes
                    .Where(v => v.DietaryRestrictionId == dietaryRestrictionId)
                    .GroupBy(v => v.PlaceId)
                    .Select(g => new { PlaceId = g.Key, VoteCount = g.Count() })
                    .Where(x => x.VoteCount >= threshold);

                var results = await (from vc in activeVoteCounts
                                     join r in _context.CommunityRestaurants on vc.PlaceId equals r.PlaceId
                                     orderby vc.VoteCount descending, r.Rating descending
                                     select new
                                     {
                                         placeId = r.PlaceId,
                                         name = r.Name,
                                         address = r.Address,
                                         rating = r.Rating,
                                         priceLevel = r.PriceLevel,
                                         photoReference = r.PhotoReference,
                                         latitude = r.Latitude,
                                         longitude = r.Longitude,
                                         phoneNumber = r.PhoneNumber,
                                         website = r.Website,
                                         types = r.Types,
                                         voteCount = vc.VoteCount
                                     }).ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shops by dietary tag");
                return StatusCode(500, new { message = "Error getting shops by dietary tag" });
            }
        }
    }

    public class VoteDietaryTagRequest
    {
        public string PlaceId { get; set; } = string.Empty;
        public int DietaryRestrictionId { get; set; }

        // Cached restaurant fields (mirrors favorites shape)
        public string? Name { get; set; }
        public string? Address { get; set; }
        public decimal? Rating { get; set; }
        public int? PriceLevel { get; set; }
        public string? PhotoReference { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public List<string>? Types { get; set; }
    }

    public class DietaryTagVoteStatusRequest
    {
        public List<string>? PlaceIds { get; set; }
        public List<int>? DietaryRestrictionIds { get; set; }
    }
}

