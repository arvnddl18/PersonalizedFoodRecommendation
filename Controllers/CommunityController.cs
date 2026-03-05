using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Capstone.Data;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommunityController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CommunityController> _logger;
        private readonly IConfiguration _configuration;

        // Rate limiting: track last post/comment time per user (in-memory, simple approach)
        private static readonly Dictionary<int, DateTime> _lastPostTime = new();
        private static readonly Dictionary<int, DateTime> _lastCommentTime = new();
        private const int POST_COOLDOWN_SECONDS = 30;
        private const int COMMENT_COOLDOWN_SECONDS = 10;

        // Image upload constraints for community posts
        private const long MaxPostImageBytes = 5 * 1024 * 1024; // 5MB
        private static readonly HashSet<string> AllowedImageExtensions = new(new[]
        {
            ".jpg", ".jpeg", ".png", ".webp", ".jfif"
        }, StringComparer.OrdinalIgnoreCase);

        public CommunityController(AppDbContext context, ILogger<CommunityController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private int GetThreshold(int dietaryRestrictionId)
        {
            var defaultThreshold = _configuration.GetValue("DietaryTagVotes:DefaultThreshold", 3);
            var overrides = _configuration
                .GetSection("DietaryTagVotes:Overrides")
                .Get<Dictionary<string, int>>() ?? new Dictionary<string, int>();

            return overrides.TryGetValue(dietaryRestrictionId.ToString(), out var overrideValue)
                ? overrideValue
                : defaultThreshold;
        }

        private async Task<bool> IsVerifiedOwnerAsync(int userId, string? placeId)
        {
            if (string.IsNullOrWhiteSpace(placeId))
                return false;

            return await _context.RestaurantOwners
                .AnyAsync(ro => ro.PlaceId == placeId &&
                                ro.UserId == userId &&
                                ro.VerificationStatus == "Verified");
        }

        private async Task<bool> IsAdminAsync(int userId)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleType == "Admin");
        }

        // GET: api/Community/feed?dietaryRestrictionId=1&placeId=...&type=...
        [HttpGet("feed")]
        public async Task<ActionResult> GetFeed(
            [FromQuery] int? dietaryRestrictionId,
            [FromQuery] string? placeId,
            [FromQuery] string? type,
            [FromQuery] int limit = 20)
        {
            try
            {
                limit = Math.Clamp(limit, 1, 50);
                var userId = HttpContext.Session.GetInt32("UserId");

                var query = _context.CommunityPosts
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted);

                if (dietaryRestrictionId.HasValue)
                    query = query.Where(p => p.DietaryRestrictionId == dietaryRestrictionId.Value);

                if (!string.IsNullOrWhiteSpace(placeId))
                    query = query.Where(p => p.PlaceId == placeId);

                if (!string.IsNullOrWhiteSpace(type))
                    query = query.Where(p => p.PostType == type);

                var posts = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .Select(p => new
                    {
                        p.PostId,
                        p.DietaryRestrictionId,
                        p.PlaceId,
                        p.PostType,
                        p.Title,
                        p.Body,
                        p.ImageUrl,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.IsVerifiedPoster,
                        p.VerifiedByRole,
                        Author = new { p.UserId, p.User.Name },
                        CommentCount = p.Comments.Count(c => !c.IsDeleted),
                        Reactions = p.Reactions
                            .GroupBy(r => r.ReactionType)
                            .Select(g => new { Type = g.Key, Count = g.Count() })
                            .ToList(),
                        UserReactions = userId.HasValue
                            ? p.Reactions
                                .Where(r => r.UserId == userId.Value)
                                .Select(r => r.ReactionType)
                                .ToList()
                            : new List<string>()
                    })
                    .ToListAsync();

                return Ok(new { posts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving community feed");
                return StatusCode(500, new { message = "Error retrieving feed" });
            }
        }

        private async Task<(bool Success, string? Url, string? ErrorMessage)> SaveCommunityPostImageAsync(int userId, IFormFile image)
        {
            if (image == null || image.Length <= 0)
                return (false, null, "Image file is required");

            if (image.Length > MaxPostImageBytes)
                return (false, null, "Image is too large (max 5MB)");

            if (string.IsNullOrWhiteSpace(image.ContentType) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return (false, null, "Only image files are allowed");

            var ext = Path.GetExtension(image.FileName);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = image.ContentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
            }

            ext = ext.ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
                return (false, null, "Unsupported image type. Use JPG, PNG, or WebP.");

            // Store images under wwwroot/uploads/community-posts/user-{userId}
            var safeUserSegment = $"user-{userId}";
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "community-posts", safeUserSegment);
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var url = $"/uploads/community-posts/{safeUserSegment}/{fileName}";
            return (true, url, null);
        }

        // GET: api/Community/trending?dietaryRestrictionId=1&placeId=...&type=...
        [HttpGet("trending")]
        public async Task<ActionResult> GetTrending(
            [FromQuery] int? dietaryRestrictionId,
            [FromQuery] string? placeId,
            [FromQuery] string? type,
            [FromQuery] int limit = 20)
        {
            try
            {
                limit = Math.Clamp(limit, 1, 50);
                var userId = HttpContext.Session.GetInt32("UserId");

                var query = _context.CommunityPosts
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted);

                if (dietaryRestrictionId.HasValue)
                    query = query.Where(p => p.DietaryRestrictionId == dietaryRestrictionId.Value);

                if (!string.IsNullOrWhiteSpace(placeId))
                    query = query.Where(p => p.PlaceId == placeId);

                if (!string.IsNullOrWhiteSpace(type))
                    query = query.Where(p => p.PostType == type);

                var since = DateTime.UtcNow.AddDays(-7);

                // Rank by: recent activity (comments in last 7 days) + reaction count + supporter count
                // IMPORTANT: keep this as a single EF query so navigation props are projected safely (no null refs).
                var ranked = await query
                    .Select(p => new
                    {
                        p.PostId,
                        p.DietaryRestrictionId,
                        p.PlaceId,
                        p.PostType,
                        p.Title,
                        p.Body,
                        p.ImageUrl,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.IsVerifiedPoster,
                        p.VerifiedByRole,
                        p.UserId,
                        UserName = p.User.Name,
                        ReactionCount = p.Reactions.Count,
                        RecentCommentCount = p.Comments.Count(c => !c.IsDeleted && c.CreatedAt >= since),
                        SupporterCount = (!string.IsNullOrWhiteSpace(p.PlaceId) && p.DietaryRestrictionId.HasValue)
                            ? _context.CommunityDietaryTagVotes.Count(v =>
                                v.PlaceId == p.PlaceId &&
                                v.DietaryRestrictionId == p.DietaryRestrictionId.Value)
                            : 0,
                        CommentCount = p.Comments.Count(c => !c.IsDeleted),
                        Reactions = p.Reactions
                            .GroupBy(r => r.ReactionType)
                            .Select(g => new { Type = g.Key, Count = g.Count() })
                            .ToList(),
                        UserReactions = userId.HasValue
                            ? p.Reactions
                                .Where(r => r.UserId == userId.Value)
                                .Select(r => r.ReactionType)
                                .ToList()
                            : new List<string>()
                    })
                    .OrderByDescending(x => x.RecentCommentCount * 2 + x.ReactionCount + x.SupporterCount)
                    .ThenByDescending(x => x.CreatedAt)
                    .Take(limit)
                    .Select(x => new
                    {
                        x.PostId,
                        x.DietaryRestrictionId,
                        x.PlaceId,
                        x.PostType,
                        x.Title,
                        x.Body,
                        x.CreatedAt,
                        x.UpdatedAt,
                        x.IsVerifiedPoster,
                        x.VerifiedByRole,
                        Author = new { x.UserId, Name = x.UserName },
                        x.CommentCount,
                        x.Reactions,
                        x.UserReactions,
                        x.SupporterCount
                    })
                    .ToListAsync();

                return Ok(new { posts = ranked });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trending posts");
                return StatusCode(500, new { message = "Error retrieving trending posts" });
            }
        }

        // POST: api/Community/post
        [HttpPost("post")]
        public async Task<ActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            return await CreatePostInternal(request, null);
        }

        // POST: api/Community/post-with-image
        // Accepts the same fields as CreatePost plus an optional image file.
        [HttpPost("post-with-image")]
        [RequestSizeLimit(MaxPostImageBytes)]
        public async Task<ActionResult> CreatePostWithImage([FromForm] CreatePostWithImageRequest request)
        {
            var basicRequest = new CreatePostRequest
            {
                DietaryRestrictionId = request.DietaryRestrictionId,
                PlaceId = request.PlaceId,
                PostType = request.PostType,
                Title = request.Title,
                Body = request.Body
            };

            return await CreatePostInternal(basicRequest, request.Image);
        }

        private async Task<ActionResult> CreatePostInternal(CreatePostRequest request, IFormFile? image)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated" });

                // Rate limiting
                if (_lastPostTime.TryGetValue(userId.Value, out var lastPost) &&
                    DateTime.UtcNow - lastPost < TimeSpan.FromSeconds(POST_COOLDOWN_SECONDS))
                {
                    var remaining = POST_COOLDOWN_SECONDS - (int)(DateTime.UtcNow - lastPost).TotalSeconds;
                    return BadRequest(new { message = $"Please wait {remaining} seconds before posting again." });
                }

                // Validation
                if (!request.DietaryRestrictionId.HasValue && string.IsNullOrWhiteSpace(request.PlaceId))
                    return BadRequest(new { message = "Either dietaryRestrictionId or placeId is required" });

                if (string.IsNullOrWhiteSpace(request.PostType))
                    return BadRequest(new { message = "PostType is required" });

                var validTypes = new[] { "Discussion", "Tip", "Question", "Find", "Meetup", "OwnerUpdate" };
                if (!validTypes.Contains(request.PostType))
                    return BadRequest(new { message = "Invalid PostType" });

                var body = (request.Body ?? string.Empty).Trim();
                if (body.Length < 10)
                    return BadRequest(new { message = "Post body is too short (minimum 10 characters)" });
                if (body.Length > 2000)
                    return BadRequest(new { message = "Post body is too long (maximum 2000 characters)" });

                // OwnerUpdate requires verified owner
                if (request.PostType == "OwnerUpdate")
                {
                    if (string.IsNullOrWhiteSpace(request.PlaceId))
                        return BadRequest(new { message = "PlaceId is required for OwnerUpdate posts" });

                    var isOwner = await IsVerifiedOwnerAsync(userId.Value, request.PlaceId);
                    if (!isOwner)
                    {
                        var isAdmin = await IsAdminAsync(userId.Value);
                        if (!isAdmin)
                            return Forbid("Only verified restaurant owners can create OwnerUpdate posts");
                    }
                }

                // If placeId is provided, verify it's community-tagged (meets threshold)
                if (!string.IsNullOrWhiteSpace(request.PlaceId) && request.DietaryRestrictionId.HasValue)
                {
                    var voteCount = await _context.CommunityDietaryTagVotes
                        .CountAsync(v => v.PlaceId == request.PlaceId &&
                                         v.DietaryRestrictionId == request.DietaryRestrictionId.Value);

                    var threshold = GetThreshold(request.DietaryRestrictionId.Value);
                    if (voteCount < threshold)
                        return BadRequest(new { message = "Restaurant must be community-tagged (meet vote threshold) before posting" });
                }

                var isVerifiedPoster = false;
                string? verifiedByRole = null;

                if (request.PostType == "OwnerUpdate" && !string.IsNullOrWhiteSpace(request.PlaceId))
                {
                    var isOwner = await IsVerifiedOwnerAsync(userId.Value, request.PlaceId);
                    var isAdmin = await IsAdminAsync(userId.Value);
                    isVerifiedPoster = isOwner || isAdmin;
                    verifiedByRole = isOwner ? "Owner" : (isAdmin ? "Admin" : null);
                }

                var post = new CommunityPost
                {
                    UserId = userId.Value,
                    DietaryRestrictionId = request.DietaryRestrictionId,
                    PlaceId = string.IsNullOrWhiteSpace(request.PlaceId) ? null : request.PlaceId.Trim(),
                    PostType = request.PostType,
                    Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
                    Body = body,
                    CreatedAt = DateTime.UtcNow,
                    IsVerifiedPoster = isVerifiedPoster,
                    VerifiedByRole = verifiedByRole,
                    VerifiedAt = isVerifiedPoster ? DateTime.UtcNow : null
                };

                // Handle optional image upload
                if (image != null && image.Length > 0)
                {
                    var imageResult = await SaveCommunityPostImageAsync(userId.Value, image);
                    if (!imageResult.Success)
                    {
                        return BadRequest(new { message = imageResult.ErrorMessage ?? "Invalid image file" });
                    }

                    post.ImageUrl = imageResult.Url;
                }

                _context.CommunityPosts.Add(post);
                await _context.SaveChangesAsync();

                _lastPostTime[userId.Value] = DateTime.UtcNow;

                return Ok(new
                {
                    post.PostId,
                    post.DietaryRestrictionId,
                    post.PlaceId,
                    post.PostType,
                    post.Title,
                    post.Body,
                    post.ImageUrl,
                    post.CreatedAt,
                    post.IsVerifiedPoster,
                    post.VerifiedByRole,
                    Author = new { UserId = userId.Value }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating community post");
                return StatusCode(500, new { message = "Error creating post" });
            }
        }

        // POST: api/Community/comment
        [HttpPost("comment")]
        public async Task<ActionResult> CreateComment([FromBody] CreateCommentRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated" });

                // Rate limiting
                if (_lastCommentTime.TryGetValue(userId.Value, out var lastComment) &&
                    DateTime.UtcNow - lastComment < TimeSpan.FromSeconds(COMMENT_COOLDOWN_SECONDS))
                {
                    var remaining = COMMENT_COOLDOWN_SECONDS - (int)(DateTime.UtcNow - lastComment).TotalSeconds;
                    return BadRequest(new { message = $"Please wait {remaining} seconds before commenting again." });
                }

                if (request.PostId <= 0)
                    return BadRequest(new { message = "PostId is required" });

                var post = await _context.CommunityPosts
                    .FirstOrDefaultAsync(p => p.PostId == request.PostId && !p.IsDeleted);

                if (post == null)
                    return NotFound(new { message = "Post not found" });

                var body = (request.Body ?? string.Empty).Trim();
                if (body.Length < 2)
                    return BadRequest(new { message = "Comment is too short (minimum 2 characters)" });
                if (body.Length > 1200)
                    return BadRequest(new { message = "Comment is too long (maximum 1200 characters)" });

                var comment = new CommunityComment
                {
                    PostId = request.PostId,
                    UserId = userId.Value,
                    Body = body,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CommunityComments.Add(comment);
                await _context.SaveChangesAsync();

                // Reload to get user name
                await _context.Entry(comment).Reference(c => c.User).LoadAsync();
                var userName = comment.User.Name;

                _lastCommentTime[userId.Value] = DateTime.UtcNow;

                return Ok(new
                {
                    comment.CommentId,
                    comment.PostId,
                    comment.Body,
                    comment.CreatedAt,
                    Author = new { UserId = userId.Value, Name = userName }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, new { message = "Error creating comment" });
            }
        }

        // POST: api/Community/react
        [HttpPost("react")]
        public async Task<ActionResult> ReactToPost([FromBody] ReactToPostRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized(new { message = "User not authenticated" });

                if (request.PostId <= 0)
                    return BadRequest(new { message = "PostId is required" });

                if (string.IsNullOrWhiteSpace(request.ReactionType))
                    return BadRequest(new { message = "ReactionType is required" });

                var validTypes = new[] { "Helpful", "Interested", "Tried", "Saved" };
                if (!validTypes.Contains(request.ReactionType))
                    return BadRequest(new { message = "Invalid ReactionType" });

                var post = await _context.CommunityPosts
                    .FirstOrDefaultAsync(p => p.PostId == request.PostId && !p.IsDeleted);

                if (post == null)
                    return NotFound(new { message = "Post not found" });

                // Check if reaction already exists
                var existing = await _context.CommunityPostReactions
                    .FirstOrDefaultAsync(r => r.PostId == request.PostId &&
                                               r.UserId == userId.Value &&
                                               r.ReactionType == request.ReactionType);

                if (existing != null)
                {
                    // Toggle: remove reaction
                    _context.CommunityPostReactions.Remove(existing);
                    await _context.SaveChangesAsync();
                    return Ok(new { removed = true, reactionType = request.ReactionType });
                }

                // Add new reaction
                var reaction = new CommunityPostReaction
                {
                    PostId = request.PostId,
                    UserId = userId.Value,
                    ReactionType = request.ReactionType,
                    ReactedAt = DateTime.UtcNow
                };

                _context.CommunityPostReactions.Add(reaction);
                await _context.SaveChangesAsync();

                return Ok(new { added = true, reactionType = request.ReactionType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to post");
                return StatusCode(500, new { message = "Error reacting to post" });
            }
        }

        // GET: api/Community/comments?postId=1
        [HttpGet("comments")]
        public async Task<ActionResult> GetComments([FromQuery] int postId, [FromQuery] int limit = 50)
        {
            try
            {
                limit = Math.Clamp(limit, 1, 100);

                var comments = await _context.CommunityComments
                    .AsNoTracking()
                    .Where(c => c.PostId == postId && !c.IsDeleted)
                    .OrderBy(c => c.CreatedAt)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.CommentId,
                        c.PostId,
                        c.Body,
                        c.CreatedAt,
                        c.UpdatedAt,
                        Author = new { c.UserId, c.User.Name }
                    })
                    .ToListAsync();

                return Ok(new { postId, comments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comments");
                return StatusCode(500, new { message = "Error retrieving comments" });
            }
        }

        public class CreatePostRequest
        {
            public int? DietaryRestrictionId { get; set; }
            public string? PlaceId { get; set; }
            public string PostType { get; set; } = string.Empty;
            public string? Title { get; set; }
            public string? Body { get; set; }
        }

        public class CreatePostWithImageRequest
        {
            public int? DietaryRestrictionId { get; set; }
            public string? PlaceId { get; set; }
            public string PostType { get; set; } = string.Empty;
            public string? Title { get; set; }
            public string? Body { get; set; }

            // Image file sent as multipart/form-data
            public IFormFile? Image { get; set; }
        }

        public class CreateCommentRequest
        {
            public int PostId { get; set; }
            public string? Body { get; set; }
        }

        public class ReactToPostRequest
        {
            public int PostId { get; set; }
            public string ReactionType { get; set; } = string.Empty;
        }
    }
}
