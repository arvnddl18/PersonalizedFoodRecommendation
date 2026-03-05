using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using Capstone.Models;
using static Capstone.Models.NomsaurModel;
using System.Text.Json;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(AppDbContext context, ILogger<FavoritesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Favorites
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserFavoriteRestaurant>>> GetFavorites()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var favorites = await _context.UserFavoriteRestaurants
                    .Where(f => f.UserId == userId.Value)
                    .OrderByDescending(f => f.AddedAt)
                    .ToListAsync();

                return Ok(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorites");
                return StatusCode(500, new { message = "Error retrieving favorites" });
            }
        }

        // GET: api/Favorites/check/{placeId}
        [HttpGet("check/{placeId}")]
        public async Task<ActionResult<bool>> CheckIfFavorite(string placeId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isFavorite = await _context.UserFavoriteRestaurants
                    .AnyAsync(f => f.UserId == userId.Value && f.PlaceId == placeId);

                return Ok(new { isFavorite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorite status");
                return StatusCode(500, new { message = "Error checking favorite status" });
            }
        }

        // POST: api/Favorites/add
        [HttpPost("add")]
        public async Task<ActionResult<UserFavoriteRestaurant>> AddFavorite([FromBody] AddFavoriteRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Check if already favorited
                var existing = await _context.UserFavoriteRestaurants
                    .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.PlaceId == request.PlaceId);

                if (existing != null)
                {
                    return Ok(new { message = "Already in favorites", favorite = existing });
                }

                // Create new favorite
                var favorite = new UserFavoriteRestaurant
                {
                    UserId = userId.Value,
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
                    AddedAt = DateTime.Now
                };

                _context.UserFavoriteRestaurants.Add(favorite);
                await _context.SaveChangesAsync();

                // Track behavior
                var behavior = new UserBehavior
                {
                    UserId = userId.Value,
                    Action = "add_favorite",
                    Context = $"Added favorite: {favorite.Name}",
                    Result = "success",
                    Timestamp = DateTime.Now,
                    EstablishmentName = favorite.Name,
                    EstablishmentInfo = JsonSerializer.Serialize(new
                    {
                        placeId = favorite.PlaceId,
                        name = favorite.Name,
                        address = favorite.Address,
                        rating = favorite.Rating
                    })
                };
                _context.UserBehaviors.Add(behavior);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFavorites), new { id = favorite.FavoriteId }, favorite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding favorite");
                return StatusCode(500, new { message = "Error adding favorite" });
            }
        }

        // DELETE: api/Favorites/remove/{placeId}
        [HttpDelete("remove/{placeId}")]
        public async Task<ActionResult> RemoveFavorite(string placeId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var favorite = await _context.UserFavoriteRestaurants
                    .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.PlaceId == placeId);

                if (favorite == null)
                {
                    return NotFound(new { message = "Favorite not found" });
                }

                _context.UserFavoriteRestaurants.Remove(favorite);
                await _context.SaveChangesAsync();

                // Track behavior
                var behavior = new UserBehavior
                {
                    UserId = userId.Value,
                    Action = "remove_favorite",
                    Context = $"Removed favorite: {favorite.Name}",
                    Result = "success",
                    Timestamp = DateTime.Now,
                    EstablishmentName = favorite.Name
                };
                _context.UserBehaviors.Add(behavior);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Favorite removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite");
                return StatusCode(500, new { message = "Error removing favorite" });
            }
        }

        // POST: api/Favorites/update-viewed/{placeId}
        [HttpPost("update-viewed/{placeId}")]
        public async Task<ActionResult> UpdateLastViewed(string placeId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var favorite = await _context.UserFavoriteRestaurants
                    .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.PlaceId == placeId);

                if (favorite == null)
                {
                    return NotFound(new { message = "Favorite not found" });
                }

                favorite.LastViewed = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Last viewed updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last viewed");
                return StatusCode(500, new { message = "Error updating last viewed" });
            }
        }

        // GET: api/Favorites/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetFavoritesCount()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var count = await _context.UserFavoriteRestaurants
                    .CountAsync(f => f.UserId == userId.Value);

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites count");
                return StatusCode(500, new { message = "Error getting favorites count" });
            }
        }
    }

    // Request model for adding favorites
    public class AddFavoriteRequest
    {
        public string PlaceId { get; set; } = string.Empty;
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
}

