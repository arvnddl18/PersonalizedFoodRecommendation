using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using Capstone.Models;
using static Capstone.Models.NomsaurModel;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RestaurantEditService _editService;
        private readonly ILogger<RestaurantController> _logger;

        public RestaurantController(AppDbContext context, ILogger<RestaurantController> logger)
        {
            _context = context;
            _logger = logger;
            _editService = new RestaurantEditService(context);
        }

        // GET: Restaurant/Details/{placeId}
        public async Task<IActionResult> Details(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
            {
                return NotFound();
            }

            var currentVersion = await _editService.GetCurrentVersion(placeId);
            var owner = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .FirstOrDefaultAsync(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified");

            ViewBag.PlaceId = placeId;
            ViewBag.CurrentVersion = currentVersion;
            ViewBag.Owner = owner;
            ViewBag.IsOwner = false;
            ViewBag.IsAdmin = false;

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                ViewBag.IsOwner = await _editService.IsUserOwner(placeId, userId.Value);
                ViewBag.IsAdmin = await IsAdmin(userId.Value);
            }

            return View();
        }

        // GET: Restaurant/Edit/{placeId}
        [Authorize]
        public async Task<IActionResult> Edit(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Only verified owner or admin can edit restaurant details
            var canEdit = await _editService.IsUserOwner(placeId, userId.Value) || await IsAdmin(userId.Value);
            if (!canEdit)
            {
                TempData["Error"] = "Only a verified owner or admin can edit restaurant details.";
                return RedirectToAction("Details", new { placeId });
            }

            // Check rate limiting
            if (!await _editService.CanUserEdit(userId.Value))
            {
                var remaining = await _editService.GetRemainingEdits(userId.Value);
                TempData["Error"] = $"You have reached the daily edit limit of 10 edits. Please try again tomorrow. ({remaining} remaining)";
                return RedirectToAction("Details", new { placeId });
            }

            var currentVersion = await _editService.GetCurrentVersion(placeId);
            ViewBag.PlaceId = placeId;
            ViewBag.CurrentVersion = currentVersion;
            ViewBag.RemainingEdits = await _editService.GetRemainingEdits(userId.Value);

            // Initialize model with current version data or empty values
            var model = new EditRestaurantViewModel
            {
                PlaceId = placeId,
                Name = currentVersion?.Name ?? "",
                Address = currentVersion?.Address,
                PhoneNumber = currentVersion?.PhoneNumber,
                Website = currentVersion?.Website,
                Description = currentVersion?.Description,
                CuisineType = currentVersion?.CuisineType,
                PriceRange = currentVersion?.PriceRange,
                OpeningHours = currentVersion?.OpeningHours,
                SpecialFeatures = currentVersion?.SpecialFeatures,
                FacebookUrl = currentVersion?.FacebookUrl,
                InstagramUrl = currentVersion?.InstagramUrl,
                ServiceOptions = currentVersion?.ServiceOptions,
                PopularFor = currentVersion?.PopularFor,
                MenuItemsAndIngredients = currentVersion?.MenuItemsAndIngredients
            };

            return View(model);
        }

        // POST: Restaurant/Edit/{placeId}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string placeId, EditRestaurantViewModel model)
        {
            if (string.IsNullOrEmpty(placeId) || placeId != model.PlaceId)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Only verified owner or admin can edit restaurant details
            var canEdit = await _editService.IsUserOwner(placeId, userId.Value) || await IsAdmin(userId.Value);
            if (!canEdit)
            {
                return Forbid("Only a verified owner or admin can edit restaurant details.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PlaceId = placeId;
                ViewBag.CurrentVersion = await _editService.GetCurrentVersion(placeId);
                ViewBag.RemainingEdits = await _editService.GetRemainingEdits(userId.Value);
                return View(model);
            }

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var version = await _editService.CreateVersion(
                    model.PlaceId,
                    userId.Value,
                    model.Name,
                    model.Address,
                    model.PhoneNumber,
                    model.Website,
                    model.Description,
                    model.CuisineType,
                    model.PriceRange,
                    model.OpeningHours,
                    model.SpecialFeatures,
                    model.FacebookUrl,
                    model.InstagramUrl,
                    model.ServiceOptions,
                    model.PopularFor,
                    model.MenuItemsAndIngredients,
                    ipAddress
                );

                TempData["Success"] = "Your edit has been submitted successfully. " + 
                    (version.Status == "Current" ? "It has been automatically approved." : "It is pending owner approval.");

                return RedirectToAction("Details", new { placeId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Edit", new { placeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating restaurant version");
                TempData["Error"] = "An error occurred while saving your edit. Please try again.";
                return RedirectToAction("Edit", new { placeId });
            }
        }

        // GET: Restaurant/History/{placeId}
        public async Task<IActionResult> History(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
            {
                return NotFound();
            }

            var history = await _editService.GetVersionHistory(placeId);
            var currentVersion = await _editService.GetCurrentVersion(placeId);
            var owner = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .FirstOrDefaultAsync(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified");

            ViewBag.PlaceId = placeId;
            ViewBag.History = history;
            ViewBag.CurrentVersion = currentVersion;
            ViewBag.Owner = owner;

            return View();
        }

        // GET: Restaurant/Claim/{placeId}
        [Authorize]
        public async Task<IActionResult> Claim(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Backward-compatible route: redirect to Details and open modal.
            // Also preserve the same claimed/pending checks to avoid confusion.
            var existingOwner = await _context.RestaurantOwners
                .FirstOrDefaultAsync(ro => ro.PlaceId == placeId);

            if (existingOwner != null)
            {
                if (existingOwner.VerificationStatus == "Verified")
                {
                    if (existingOwner.UserId == userId.Value)
                    {
                        TempData["Info"] = "You have already claimed this restaurant.";
                        return RedirectToAction("Details", new { placeId });
                    }

                    TempData["Error"] = "This restaurant has already been claimed by another user.";
                    return RedirectToAction("Details", new { placeId });
                }

                if (existingOwner.VerificationStatus == "Pending")
                {
                    if (existingOwner.UserId != userId.Value)
                    {
                        TempData["Error"] = "This restaurant already has a pending claim from another user.";
                        return RedirectToAction("Details", new { placeId });
                    }

                    TempData["Info"] = "You already have a pending claim. You can update the details below.";
                    return RedirectToAction("Details", new { placeId, openClaim = 1 });
                }

                TempData["Error"] = "This restaurant cannot be claimed right now.";
                return RedirectToAction("Details", new { placeId });
            }

            return RedirectToAction("Details", new { placeId, openClaim = 1 });
        }

        // POST: Restaurant/Claim/{placeId}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Claim(string placeId, ClaimRestaurantViewModel model)
        {
            if (string.IsNullOrEmpty(placeId) || placeId != model.PlaceId)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { message = "Invalid restaurant identifier." });
                }
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { message = "Please provide the required claim details and try again." });
                }
                TempData["Error"] = "Please provide the required claim details and try again.";
                return RedirectToAction("Details", new { placeId, openClaim = 1 });
            }

            try
            {
                // Handle file upload
                string? licensePath = null;
                if (model.BusinessLicense != null && model.BusinessLicense.Length > 0)
                {
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "licenses");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = $"{Guid.NewGuid()}_{model.BusinessLicense.FileName}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.BusinessLicense.CopyToAsync(stream);
                    }
                    licensePath = $"/uploads/licenses/{fileName}";
                }

                var verificationService = new OwnerVerificationService(_context);
                var owner = await verificationService.ClaimRestaurant(
                    model.PlaceId,
                    userId.Value,
                    model.RestaurantName,
                    model.BusinessName,
                    model.BusinessEmail,
                    model.BusinessPhone,
                    model.BusinessAddress,
                    licensePath,
                    model.AdditionalNotes
                );

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new
                    {
                        message = "Your claim has been submitted successfully. An admin will review it shortly.",
                        ownerId = owner.OwnerId,
                        status = owner.VerificationStatus
                    });
                }

                TempData["Success"] = "Your claim has been submitted successfully. An admin will review it shortly.";
                return RedirectToAction("Details", new { placeId });
            }
            catch (InvalidOperationException ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { message = ex.Message });
                }
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { placeId, openClaim = 1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming restaurant");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return StatusCode(500, new { message = "An error occurred while submitting your claim. Please try again." });
                }
                TempData["Error"] = "An error occurred while submitting your claim. Please try again.";
                return RedirectToAction("Details", new { placeId, openClaim = 1 });
            }
        }

        // Helper method to check if user is admin
        private async Task<bool> IsAdmin(int userId)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleType == "Admin");
        }
    }

    // View Models
    public class EditRestaurantViewModel
    {
        [Required]
        public string PlaceId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        [Url]
        public string? Website { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? CuisineType { get; set; }

        [StringLength(50)]
        public string? PriceRange { get; set; }

        [StringLength(1000)]
        public string? OpeningHours { get; set; }

        [StringLength(2000)]
        public string? SpecialFeatures { get; set; }

        [StringLength(500)]
        [Url]
        public string? FacebookUrl { get; set; }

        [StringLength(500)]
        [Url]
        public string? InstagramUrl { get; set; }

        [StringLength(1000)]
        public string? ServiceOptions { get; set; }

        [StringLength(1000)]
        public string? PopularFor { get; set; }

        [StringLength(8000)]
        public string? MenuItemsAndIngredients { get; set; }
    }

    public class ClaimRestaurantViewModel
    {
        [Required]
        public string PlaceId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string RestaurantName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? BusinessName { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? BusinessEmail { get; set; }

        [StringLength(20)]
        public string? BusinessPhone { get; set; }

        [StringLength(500)]
        public string? BusinessAddress { get; set; }

        public IFormFile? BusinessLicense { get; set; }

        [StringLength(500)]
        public string? AdditionalNotes { get; set; }
    }
}

