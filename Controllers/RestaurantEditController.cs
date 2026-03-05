using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using Capstone.Models;
using static Capstone.Models.NomsaurModel;
using System.Text.RegularExpressions;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantEditController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RestaurantEditService _editService;
        private readonly ILogger<RestaurantEditController> _logger;

        public RestaurantEditController(AppDbContext context, ILogger<RestaurantEditController> logger)
        {
            _context = context;
            _logger = logger;
            _editService = new RestaurantEditService(context);
        }

        // GET: api/RestaurantEdit/current/{placeId}
        [HttpGet("current/{placeId}")]
        public async Task<ActionResult<RestaurantVersion>> GetCurrentVersion(string placeId)
        {
            try
            {
                var version = await _editService.GetCurrentVersion(placeId);
                if (version == null)
                {
                    return NotFound(new { message = "No version found for this restaurant" });
                }

                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current version");
                return StatusCode(500, new { message = "Error retrieving current version" });
            }
        }

        // GET: api/RestaurantEdit/history/{placeId}
        [HttpGet("history/{placeId}")]
        public async Task<ActionResult<List<RestaurantVersion>>> GetVersionHistory(string placeId)
        {
            try
            {
                var history = await _editService.GetVersionHistory(placeId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version history");
                return StatusCode(500, new { message = "Error retrieving version history" });
            }
        }

        // GET: api/RestaurantEdit/edits/{placeId}
        [HttpGet("edits/{placeId}")]
        public async Task<ActionResult<List<RestaurantEdit>>> GetEditHistory(string placeId, [FromQuery] int limit = 50)
        {
            try
            {
                var edits = await _editService.GetEditHistory(placeId, limit);
                return Ok(edits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving edit history");
                return StatusCode(500, new { message = "Error retrieving edit history" });
            }
        }

        // GET: api/RestaurantEdit/pending/{placeId}
        [HttpGet("pending/{placeId}")]
        public async Task<ActionResult<List<RestaurantVersion>>> GetPendingVersions(string placeId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Check if user is owner
                var isOwner = await _editService.IsUserOwner(placeId, userId.Value);
                if (!isOwner)
                {
                    return Forbid("Only the restaurant owner can view pending versions");
                }

                var pending = await _editService.GetPendingVersions(placeId);
                return Ok(pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending versions");
                return StatusCode(500, new { message = "Error retrieving pending versions" });
            }
        }

        // GET: api/RestaurantEdit/remaining
        [HttpGet("remaining")]
        public async Task<ActionResult<int>> GetRemainingEdits()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var remaining = await _editService.GetRemainingEdits(userId.Value);
                return Ok(new { remaining });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving remaining edits");
                return StatusCode(500, new { message = "Error retrieving remaining edits" });
            }
        }

        // GET: api/RestaurantEdit/permissions/{placeId}
        // Used by the details panel to decide whether to show/enable the edit button
        [HttpGet("permissions/{placeId}")]
        public async Task<ActionResult> GetEditPermissions(string placeId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Ok(new { isAuthenticated = false, isOwner = false, isAdmin = false, canEdit = false });
                }

                var isOwner = await _editService.IsUserOwner(placeId, userId.Value);
                var isAdmin = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId.Value && ur.RoleType == "Admin");

                return Ok(new
                {
                    isAuthenticated = true,
                    isOwner,
                    isAdmin,
                    canEdit = isOwner || isAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving edit permissions");
                return StatusCode(500, new { message = "Error retrieving edit permissions" });
            }
        }

        // POST: api/RestaurantEdit/create
        [HttpPost("create")]
        public async Task<ActionResult<RestaurantVersion>> CreateVersion([FromBody] CreateVersionRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Only verified owner or admin can edit via the details panel
                var isOwner = await _editService.IsUserOwner(request.PlaceId, userId.Value);
                var isAdmin = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId.Value && ur.RoleType == "Admin");
                if (!isOwner && !isAdmin)
                {
                    return Forbid("Only a verified owner or admin can edit restaurant details.");
                }

                // Check rate limiting
                if (!await _editService.CanUserEdit(userId.Value))
                {
                    var remaining = await _editService.GetRemainingEdits(userId.Value);
                    return BadRequest(new { message = $"You have reached the daily edit limit. Please try again tomorrow.", remaining });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var version = await _editService.CreateVersion(
                    request.PlaceId,
                    userId.Value,
                    request.Name,
                    request.Address,
                    request.PhoneNumber,
                    request.Website,
                    request.Description,
                    request.CuisineType,
                    request.PriceRange,
                    request.OpeningHours,
                    request.SpecialFeatures,
                    request.FacebookUrl,
                    request.InstagramUrl,
                    request.ServiceOptions,
                    request.PopularFor,
                    request.MenuItemsAndIngredients,
                    ipAddress
                );

                return Ok(version);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating version");
                return StatusCode(500, new { message = "Error creating version" });
            }
        }

        // POST: api/RestaurantEdit/approve/{versionId}
        [HttpPost("approve/{versionId}")]
        public async Task<ActionResult> ApproveVersion(int versionId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Check if user is owner
                var version = await _context.RestaurantVersions
                    .FirstOrDefaultAsync(rv => rv.VersionId == versionId);

                if (version == null)
                {
                    return NotFound(new { message = "Version not found" });
                }

                var isOwner = await _editService.IsUserOwner(version.PlaceId, userId.Value);
                if (!isOwner)
                {
                    return Forbid("Only the restaurant owner can approve versions");
                }

                var success = await _editService.ApproveVersion(versionId, userId.Value, isOwner: true);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to approve version" });
                }

                return Ok(new { message = "Version approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving version");
                return StatusCode(500, new { message = "Error approving version" });
            }
        }

        // POST: api/RestaurantEdit/reject/{versionId}
        [HttpPost("reject/{versionId}")]
        public async Task<ActionResult> RejectVersion(int versionId, [FromBody] RejectVersionRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var version = await _context.RestaurantVersions
                    .FirstOrDefaultAsync(rv => rv.VersionId == versionId);

                if (version == null)
                {
                    return NotFound(new { message = "Version not found" });
                }

                var isOwner = await _editService.IsUserOwner(version.PlaceId, userId.Value);
                if (!isOwner)
                {
                    return Forbid("Only the restaurant owner can reject versions");
                }

                var success = await _editService.RejectVersion(versionId, userId.Value, request.Reason);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to reject version" });
                }

                return Ok(new { message = "Version rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting version");
                return StatusCode(500, new { message = "Error rejecting version" });
            }
        }

        // GET: api/RestaurantEdit/menu-matches?term=pasta&dietary=vegetarian
        // Public endpoint used by the map/chat search to include owner-provided menu matches.
        [HttpGet("menu-matches")]
        public async Task<ActionResult> GetMenuMatches([FromQuery] string? term = null, [FromQuery] string? dietary = null, [FromQuery] int limit = 25)
        {
            try
            {
                term = (term ?? string.Empty).Trim();
                dietary = (dietary ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(term) && string.IsNullOrWhiteSpace(dietary))
                {
                    return Ok(new { placeIds = Array.Empty<string>() });
                }

                // Pre-filter candidates in SQL to reduce memory scanning.
                // Note: case sensitivity depends on DB collation; we still do a case-insensitive parse below.
                IQueryable<RestaurantVersion> q = _context.RestaurantVersions
                    .AsNoTracking()
                    .Where(rv => rv.IsCurrent && rv.Status == "Current" && rv.MenuItemsAndIngredients != null && rv.MenuItemsAndIngredients != "");

                var termLower = term.ToLowerInvariant();
                var dietaryLower = dietary.ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    // Use LOWER() so this works even on case-sensitive collations
                    q = q.Where(rv => rv.MenuItemsAndIngredients!.ToLower().Contains(termLower));
                }
                if (!string.IsNullOrWhiteSpace(dietary))
                {
                    // quick prefilter for likely tagged entries
                    q = q.Where(rv =>
                        rv.MenuItemsAndIngredients!.ToLower().Contains(dietaryLower) ||
                        rv.MenuItemsAndIngredients!.ToLower().Contains("tags:") ||
                        rv.MenuItemsAndIngredients!.ToLower().Contains("dietary tag"));
                }

                var candidates = await q
                    .Select(rv => new { rv.PlaceId, rv.MenuItemsAndIngredients })
                    .ToListAsync();

                bool Matches(string line)
                {
                    var parsed = ParseMenuLine(line);
                    var lineLower = line.ToLowerInvariant();

                    var termOk =
                        string.IsNullOrWhiteSpace(termLower) ||
                        (!string.IsNullOrWhiteSpace(parsed.Name) && parsed.Name.ToLowerInvariant().Contains(termLower)) ||
                        (!string.IsNullOrWhiteSpace(parsed.Body) && parsed.Body.ToLowerInvariant().Contains(termLower)) ||
                        lineLower.Contains(termLower);

                    var dietaryOk =
                        string.IsNullOrWhiteSpace(dietaryLower) ||
                        (parsed.Tags.Any(t => t.Equals(dietaryLower, StringComparison.OrdinalIgnoreCase)));

                    return termOk && dietaryOk;
                }

                var placeIds = new List<string>();
                foreach (var c in candidates)
                {
                    if (placeIds.Count >= Math.Max(1, limit)) break;
                    if (string.IsNullOrWhiteSpace(c.PlaceId) || string.IsNullOrWhiteSpace(c.MenuItemsAndIngredients)) continue;

                    var lines = c.MenuItemsAndIngredients
                        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s));

                    if (lines.Any(Matches))
                    {
                        placeIds.Add(c.PlaceId);
                    }
                }

                return Ok(new { placeIds = placeIds.Distinct().Take(Math.Max(1, limit)).ToArray() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu matches");
                return StatusCode(500, new { message = "Error retrieving menu matches" });
            }
        }

        private sealed record ParsedMenuLine(string Name, string Body, List<string> Tags);

        // Server-side equivalent of the client parse (supports "[tags: ...]" / "(dietary tags: ...)" suffixes).
        private static ParsedMenuLine ParseMenuLine(string line)
        {
            var original = (line ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(original))
                return new ParsedMenuLine(string.Empty, string.Empty, new List<string>());

            var working = original;
            var tags = new List<string>();

            // peel off up to a few bracket/paren suffixes from the end
            for (var i = 0; i < 6; i++)
            {
                var m = Regex.Match(working, @"\s*(?:\(|\[)\s*([a-zA-Z ]+?)\s*:\s*([^\)\]]+?)\s*(?:\)|\])\s*$");
                if (!m.Success) break;

                var key = (m.Groups[1].Value ?? string.Empty).Trim().ToLowerInvariant();
                var val = (m.Groups[2].Value ?? string.Empty).Trim();

                if (key is "tags" or "dietary tag" or "dietary tags")
                {
                    var parsedTags = val
                        .Split(new[] { ',', '|'}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.ToLowerInvariant());
                    tags.AddRange(parsedTags);
                }

                working = working[..m.Index].Trim();
            }

            // de-dupe tags
            tags = tags
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .Take(20)
                .ToList();

            var dash = working.IndexOf(" - ", StringComparison.Ordinal);
            string name;
            string body;
            if (dash > 0)
            {
                name = working[..dash].Trim();
                body = working[(dash + 3)..].Trim();
            }
            else
            {
                name = string.Empty;
                body = working.Trim();
            }

            return new ParsedMenuLine(name, body, tags);
        }

        // Request models
        public class CreateVersionRequest
        {
            public string PlaceId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Address { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Website { get; set; }
            public string? Description { get; set; }
            public string? CuisineType { get; set; }
            public string? PriceRange { get; set; }
            public string? OpeningHours { get; set; }
            public string? SpecialFeatures { get; set; }
            public string? FacebookUrl { get; set; }
            public string? InstagramUrl { get; set; }
            public string? ServiceOptions { get; set; }
            public string? PopularFor { get; set; }
            public string? MenuItemsAndIngredients { get; set; }
        }

        public class RejectVersionRequest
        {
            public string Reason { get; set; } = string.Empty;
        }
    }
}

