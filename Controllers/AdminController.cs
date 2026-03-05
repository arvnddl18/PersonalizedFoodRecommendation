using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using Capstone.Models;
using static Capstone.Models.NomsaurModel;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RestaurantEditService _editService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
            _editService = new RestaurantEditService(context);
        }

        // Check if user is admin (middleware-like check)
        private async Task<bool> IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId.Value && ur.RoleType == "Admin");
        }

        // GET: Admin/RestaurantClaims
        public async Task<IActionResult> RestaurantClaims()
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var currentUser = userId.HasValue 
                ? await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value)
                : null;

            var pendingClaims = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .Include(ro => ro.VerifiedByUser)
                .Where(ro => ro.VerificationStatus == "Pending")
                .OrderByDescending(ro => ro.ClaimedAt)
                .ToListAsync();

            var verifiedClaims = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .Include(ro => ro.VerifiedByUser)
                .Where(ro => ro.VerificationStatus == "Verified")
                .OrderByDescending(ro => ro.VerifiedAt)
                .ToListAsync();

            var rejectedClaims = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .Include(ro => ro.VerifiedByUser)
                .Where(ro => ro.VerificationStatus == "Rejected")
                .OrderByDescending(ro => ro.ClaimedAt)
                .ToListAsync();

            var allClaims = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .Include(ro => ro.VerifiedByUser)
                .OrderByDescending(ro => ro.ClaimedAt)
                .ToListAsync();

            ViewBag.PendingClaims = pendingClaims;
            ViewBag.VerifiedClaims = verifiedClaims;
            ViewBag.RejectedClaims = rejectedClaims;
            ViewBag.AllClaims = allClaims;
            ViewBag.CurrentUser = currentUser;

            return View();
        }

        // POST: Admin/ApproveClaim/{ownerId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int ownerId)
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var owner = await _context.RestaurantOwners
                    .Include(ro => ro.User)
                    .FirstOrDefaultAsync(ro => ro.OwnerId == ownerId);

                if (owner == null)
                {
                    return NotFound();
                }

                owner.VerificationStatus = "Verified";
                owner.VerifiedAt = DateTime.UtcNow;
                owner.VerifiedByUserId = userId.Value;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Restaurant claim for {owner.RestaurantName} has been approved.";
                return RedirectToAction("RestaurantClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim");
                TempData["Error"] = "An error occurred while approving the claim.";
                return RedirectToAction("RestaurantClaims");
            }
        }

        // POST: Admin/RejectClaim/{ownerId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int ownerId, string reason)
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var owner = await _context.RestaurantOwners
                    .Include(ro => ro.User)
                    .FirstOrDefaultAsync(ro => ro.OwnerId == ownerId);

                if (owner == null)
                {
                    return NotFound();
                }

                owner.VerificationStatus = "Rejected";
                owner.RejectionReason = reason;
                owner.VerifiedByUserId = userId.Value;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Restaurant claim for {owner.RestaurantName} has been rejected.";
                return RedirectToAction("RestaurantClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim");
                TempData["Error"] = "An error occurred while rejecting the claim.";
                return RedirectToAction("RestaurantClaims");
            }
        }

        // GET: Admin/PendingEdits
        public async Task<IActionResult> PendingEdits()
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            // Get pending edits for restaurants without owners
            var restaurantsWithOwners = await _context.RestaurantOwners
                .Where(ro => ro.VerificationStatus == "Verified")
                .Select(ro => ro.PlaceId)
                .ToListAsync();

            var pendingEdits = await _context.RestaurantVersions
                .Include(rv => rv.CreatedByUser)
                .Include(rv => rv.ApprovedByUser)
                .Where(rv => rv.Status == "Pending" && !restaurantsWithOwners.Contains(rv.PlaceId))
                .OrderByDescending(rv => rv.CreatedAt)
                .ToListAsync();

            ViewBag.PendingEdits = pendingEdits;

            return View();
        }

        // POST: Admin/ApproveEdit/{versionId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEdit(int versionId)
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var success = await _editService.ApproveVersion(versionId, userId.Value, isOwner: false);
                if (!success)
                {
                    TempData["Error"] = "Failed to approve edit. It may have already been processed.";
                    return RedirectToAction("PendingEdits");
                }

                TempData["Success"] = "Edit has been approved successfully.";
                return RedirectToAction("PendingEdits");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving edit");
                TempData["Error"] = "An error occurred while approving the edit.";
                return RedirectToAction("PendingEdits");
            }
        }

        // POST: Admin/RejectEdit/{versionId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectEdit(int versionId, string reason)
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var success = await _editService.RejectVersion(versionId, userId.Value, reason);
                if (!success)
                {
                    TempData["Error"] = "Failed to reject edit. It may have already been processed.";
                    return RedirectToAction("PendingEdits");
                }

                TempData["Success"] = "Edit has been rejected.";
                return RedirectToAction("PendingEdits");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting edit");
                TempData["Error"] = "An error occurred while rejecting the edit.";
                return RedirectToAction("PendingEdits");
            }
        }

        // GET: Admin/UserRoles
        public async Task<IActionResult> UserRoles()
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var admins = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.GrantedByUser)
                .Where(ur => ur.RoleType == "Admin")
                .OrderByDescending(ur => ur.GrantedAt)
                .ToListAsync();

            var moderators = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.GrantedByUser)
                .Where(ur => ur.RoleType == "Moderator")
                .OrderByDescending(ur => ur.GrantedAt)
                .ToListAsync();

            ViewBag.Admins = admins;
            ViewBag.Moderators = moderators;

            return View();
        }

        // POST: Admin/GrantRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantRole(GrantRoleViewModel model)
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction("UserRoles");
            }

            try
            {
                // Check if user exists
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.UserEmail);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("UserRoles");
                }

                // Check if role already exists
                var existingRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleType == model.RoleType);

                if (existingRole != null)
                {
                    TempData["Error"] = "User already has this role.";
                    return RedirectToAction("UserRoles");
                }

                var role = new UserRole
                {
                    UserId = user.UserId,
                    RoleType = model.RoleType,
                    GrantedByUserId = userId.Value,
                    GrantedAt = DateTime.UtcNow
                };

                _context.UserRoles.Add(role);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Role '{model.RoleType}' has been granted to {user.Name}.";
                return RedirectToAction("UserRoles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting role");
                TempData["Error"] = "An error occurred while granting the role.";
                return RedirectToAction("UserRoles");
            }
        }

        // POST: Admin/RevokeRole/{roleId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeRole(int roleId)
        {
            if (!await IsAdmin())
            {
                return Forbid();
            }

            try
            {
                var role = await _context.UserRoles
                    .Include(ur => ur.User)
                    .FirstOrDefaultAsync(ur => ur.RoleId == roleId);

                if (role == null)
                {
                    return NotFound();
                }

                _context.UserRoles.Remove(role);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Role has been revoked from {role.User.Name}.";
                return RedirectToAction("UserRoles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking role");
                TempData["Error"] = "An error occurred while revoking the role.";
                return RedirectToAction("UserRoles");
            }
        }
    }

    // View Models
    public class GrantRoleViewModel
    {
        [Required]
        [EmailAddress]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public string RoleType { get; set; } = string.Empty; // "Admin" or "Moderator"
    }
}

