using Microsoft.AspNetCore.Mvc;
using Capstone.Data;
using Capstone.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RestaurantEditService _editService;
        private readonly ILogger<MenuItemImagesController> _logger;

        // Keep reasonably small; these are per-menu-item images
        private const long MaxBytes = 5 * 1024 * 1024; // 5MB

        public MenuItemImagesController(AppDbContext context, ILogger<MenuItemImagesController> logger)
        {
            _context = context;
            _logger = logger;
            _editService = new RestaurantEditService(context);
        }

        // POST: api/MenuItemImages/upload
        [HttpPost("upload")]
        [RequestSizeLimit(MaxBytes)]
        public async Task<IActionResult> Upload([FromForm] string placeId, [FromForm] IFormFile image)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                if (string.IsNullOrWhiteSpace(placeId))
                {
                    return BadRequest(new { message = "placeId is required" });
                }

                // Permissions:
                // - If the restaurant has a verified owner: only the verified owner or an admin can upload.
                // - If there is NO verified owner yet: allow any authenticated user (community contribution),
                //   since the menu text referencing the image can still be moderated/approved via versions.
                var isAdmin = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId.Value && ur.RoleType == "Admin");
                var hasVerifiedOwner = await _context.RestaurantOwners
                    .AnyAsync(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified");
                if (hasVerifiedOwner && !isAdmin)
                {
                    var isOwner = await _editService.IsUserOwner(placeId, userId.Value);
                    if (!isOwner)
                    {
                        return Forbid("Only the verified restaurant owner or an admin can upload menu images.");
                    }
                }

                if (image == null || image.Length <= 0)
                {
                    return BadRequest(new { message = "Image file is required" });
                }

                if (image.Length > MaxBytes)
                {
                    return BadRequest(new { message = "Image is too large (max 5MB)" });
                }

                if (string.IsNullOrWhiteSpace(image.ContentType) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Only image files are allowed" });
                }

                // Sanitize placeId for folder name (keep alnum, dash, underscore)
                var safePlace = new string(placeId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
                if (string.IsNullOrWhiteSpace(safePlace)) safePlace = "place";

                // Determine extension from filename/content type
                var ext = Path.GetExtension(image.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = image.ContentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
                ext = ext.ToLowerInvariant();
                var allowed = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp", ".jfif" };
                if (!allowed.Contains(ext))
                {
                    return BadRequest(new { message = "Unsupported image type. Use JPG, PNG, or WebP." });
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "menu-items", safePlace);
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

                var url = $"/uploads/menu-items/{safePlace}/{fileName}";
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menu image upload failed");
                return StatusCode(500, new { message = "Upload failed. Please try again." });
            }
        }
    }
}

