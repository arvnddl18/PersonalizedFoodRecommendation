using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using static Capstone.Models.NomsaurModel;
using System.Text.Json;

namespace Capstone.Models
{
    public class RestaurantEditService
    {
        private readonly AppDbContext _context;
        private const int MAX_EDITS_PER_DAY = 10;

        public RestaurantEditService(AppDbContext context)
        {
            _context = context;
        }

        // Check if user can edit (rate limiting)
        public async Task<bool> CanUserEdit(int userId)
        {
            var today = DateTime.UtcNow.Date;
            var editCount = await _context.RestaurantEdits
                .Where(re => re.UserId == userId && re.EditedAt.Date == today)
                .CountAsync();

            return editCount < MAX_EDITS_PER_DAY;
        }

        // Get remaining edits for today
        public async Task<int> GetRemainingEdits(int userId)
        {
            var today = DateTime.UtcNow.Date;
            var editCount = await _context.RestaurantEdits
                .Where(re => re.UserId == userId && re.EditedAt.Date == today)
                .CountAsync();

            return Math.Max(0, MAX_EDITS_PER_DAY - editCount);
        }

        // Get current version of a restaurant
        public async Task<RestaurantVersion?> GetCurrentVersion(string placeId)
        {
            return await _context.RestaurantVersions
                .Include(rv => rv.CreatedByUser)
                .Include(rv => rv.ApprovedByUser)
                .Where(rv => rv.PlaceId == placeId && rv.IsCurrent && rv.Status == "Current")
                .FirstOrDefaultAsync();
        }

        // Create a new version from restaurant data (from Google Maps or user input)
        public async Task<RestaurantVersion> CreateVersion(
            string placeId,
            int userId,
            string name,
            string? address = null,
            string? phoneNumber = null,
            string? website = null,
            string? description = null,
            string? cuisineType = null,
            string? priceRange = null,
            string? openingHours = null,
            string? specialFeatures = null,
            string? facebookUrl = null,
            string? instagramUrl = null,
            string? serviceOptions = null,
            string? popularFor = null,
            string? menuItemsAndIngredients = null,
            string? ipAddress = null)
        {
            // Check rate limiting
            if (!await CanUserEdit(userId))
            {
                throw new InvalidOperationException($"You have reached the daily edit limit of {MAX_EDITS_PER_DAY} edits.");
            }

            // Get next version number
            var maxVersion = await _context.RestaurantVersions
                .Where(rv => rv.PlaceId == placeId)
                .OrderByDescending(rv => rv.VersionNumber)
                .Select(rv => rv.VersionNumber)
                .FirstOrDefaultAsync();

            var versionNumber = maxVersion + 1;

            // Get current version for comparison
            var currentVersion = await GetCurrentVersion(placeId);

            // Check if restaurant has an owner
            var owner = await _context.RestaurantOwners
                .Where(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified")
                .FirstOrDefaultAsync();

            // Decide whether this edit should be applied immediately.
            // Rules:
            // - If there is NO verified owner yet: auto-apply (community/admin/any allowed editor)
            // - If there IS a verified owner: only the verified owner or an admin can auto-apply;
            //   other users (if allowed elsewhere) would require approval.
            var isVerifiedOwnerEditor = await IsUserOwner(placeId, userId);
            var isAdminEditor = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleType == "Admin");
            var shouldAutoApply = owner == null || isVerifiedOwnerEditor || isAdminEditor;

            var status = shouldAutoApply ? "Current" : "Pending";

            // Only mark previous version as historical if this new version becomes current immediately.
            if (shouldAutoApply && currentVersion != null)
            {
                currentVersion.IsCurrent = false;
                currentVersion.Status = "Historical";
            }

            // Create new version
            var newVersion = new RestaurantVersion
            {
                PlaceId = placeId,
                VersionNumber = versionNumber,
                Name = name,
                Address = address,
                PhoneNumber = phoneNumber,
                Website = website,
                Description = description,
                CuisineType = cuisineType,
                PriceRange = priceRange,
                OpeningHours = openingHours,
                SpecialFeatures = specialFeatures,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                ServiceOptions = serviceOptions,
                PopularFor = popularFor,
                MenuItemsAndIngredients = menuItemsAndIngredients,
                Status = status,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsCurrent = shouldAutoApply
            };

            // Auto-approve when applied immediately
            if (shouldAutoApply)
            {
                newVersion.ApprovedByUserId = userId;
                newVersion.ApprovedAt = DateTime.UtcNow;
            }

            _context.RestaurantVersions.Add(newVersion);
            await _context.SaveChangesAsync();

            // Create edit history entry
            var fieldChanges = GenerateFieldChanges(currentVersion, newVersion);
            var edit = new RestaurantEdit
            {
                PlaceId = placeId,
                UserId = userId,
                VersionId = newVersion.VersionId,
                Action = currentVersion == null ? "Created" : "Updated",
                FieldChanges = fieldChanges,
                EditedAt = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            _context.RestaurantEdits.Add(edit);
            await _context.SaveChangesAsync();

            return newVersion;
        }

        // Generate field changes JSON for edit history
        private string GenerateFieldChanges(RestaurantVersion? oldVersion, RestaurantVersion newVersion)
        {
            var changes = new Dictionary<string, string>();

            if (oldVersion == null)
            {
                // New restaurant - all fields are new
                changes["name"] = $"→ {newVersion.Name}";
                if (!string.IsNullOrEmpty(newVersion.Address))
                    changes["address"] = $"→ {newVersion.Address}";
                if (!string.IsNullOrEmpty(newVersion.PhoneNumber))
                    changes["phoneNumber"] = $"→ {newVersion.PhoneNumber}";
                if (!string.IsNullOrEmpty(newVersion.Website))
                    changes["website"] = $"→ {newVersion.Website}";
            }
            else
            {
                // Compare fields
                if (oldVersion.Name != newVersion.Name)
                    changes["name"] = $"{oldVersion.Name} → {newVersion.Name}";
                if (oldVersion.Address != newVersion.Address)
                    changes["address"] = $"{(oldVersion.Address ?? "null")} → {(newVersion.Address ?? "null")}";
                if (oldVersion.PhoneNumber != newVersion.PhoneNumber)
                    changes["phoneNumber"] = $"{(oldVersion.PhoneNumber ?? "null")} → {(newVersion.PhoneNumber ?? "null")}";
                if (oldVersion.Website != newVersion.Website)
                    changes["website"] = $"{(oldVersion.Website ?? "null")} → {(newVersion.Website ?? "null")}";
                if (oldVersion.Description != newVersion.Description)
                    changes["description"] = $"{(oldVersion.Description ?? "null")} → {(newVersion.Description ?? "null")}";
                if (oldVersion.CuisineType != newVersion.CuisineType)
                    changes["cuisineType"] = $"{(oldVersion.CuisineType ?? "null")} → {(newVersion.CuisineType ?? "null")}";
                if (oldVersion.PriceRange != newVersion.PriceRange)
                    changes["priceRange"] = $"{(oldVersion.PriceRange ?? "null")} → {(newVersion.PriceRange ?? "null")}";
                if (oldVersion.FacebookUrl != newVersion.FacebookUrl)
                    changes["facebookUrl"] = $"{(oldVersion.FacebookUrl ?? "null")} → {(newVersion.FacebookUrl ?? "null")}";
                if (oldVersion.InstagramUrl != newVersion.InstagramUrl)
                    changes["instagramUrl"] = $"{(oldVersion.InstagramUrl ?? "null")} → {(newVersion.InstagramUrl ?? "null")}";
                if (oldVersion.ServiceOptions != newVersion.ServiceOptions)
                    changes["serviceOptions"] = $"{(oldVersion.ServiceOptions ?? "null")} → {(newVersion.ServiceOptions ?? "null")}";
                if (oldVersion.PopularFor != newVersion.PopularFor)
                    changes["popularFor"] = $"{(oldVersion.PopularFor ?? "null")} → {(newVersion.PopularFor ?? "null")}";
                if (oldVersion.MenuItemsAndIngredients != newVersion.MenuItemsAndIngredients)
                    changes["menuItemsAndIngredients"] = $"{(oldVersion.MenuItemsAndIngredients ?? "null")} → {(newVersion.MenuItemsAndIngredients ?? "null")}";
            }

            return JsonSerializer.Serialize(changes);
        }

        // Approve a version (by owner or admin)
        public async Task<bool> ApproveVersion(int versionId, int approverUserId, bool isOwner = false)
        {
            var version = await _context.RestaurantVersions
                .Include(rv => rv.CreatedByUser)
                .FirstOrDefaultAsync(rv => rv.VersionId == versionId);

            if (version == null)
                return false;

            // Check if approver is the owner (if owner approval required)
            if (version.Status == "Pending")
            {
                if (isOwner)
                {
                    var owner = await _context.RestaurantOwners
                        .Where(ro => ro.PlaceId == version.PlaceId && ro.UserId == approverUserId && ro.VerificationStatus == "Verified")
                        .FirstOrDefaultAsync();

                    if (owner == null)
                        return false; // User is not the owner
                }
                else
                {
                    // Check if approver is admin
                    var isAdmin = await _context.UserRoles
                        .AnyAsync(ur => ur.UserId == approverUserId && ur.RoleType == "Admin");

                    if (!isAdmin)
                        return false; // User is not admin
                }

                // Mark previous version as historical
                var previousCurrent = await GetCurrentVersion(version.PlaceId);
                if (previousCurrent != null)
                {
                    previousCurrent.IsCurrent = false;
                    previousCurrent.Status = "Historical";
                }

                // Approve new version
                version.Status = "Current";
                version.IsCurrent = true;
                version.ApprovedByUserId = approverUserId;
                version.ApprovedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        // Reject a version
        public async Task<bool> RejectVersion(int versionId, int rejectorUserId, string rejectionReason)
        {
            var version = await _context.RestaurantVersions
                .FirstOrDefaultAsync(rv => rv.VersionId == versionId);

            if (version == null || version.Status != "Pending")
                return false;

            version.Status = "Rejected";
            version.RejectionReason = rejectionReason;

            await _context.SaveChangesAsync();
            return true;
        }

        // Get version history for a restaurant
        public async Task<List<RestaurantVersion>> GetVersionHistory(string placeId)
        {
            return await _context.RestaurantVersions
                .Include(rv => rv.CreatedByUser)
                .Include(rv => rv.ApprovedByUser)
                .Where(rv => rv.PlaceId == placeId)
                .OrderByDescending(rv => rv.VersionNumber)
                .ToListAsync();
        }

        // Get pending versions for a restaurant (for owner review)
        public async Task<List<RestaurantVersion>> GetPendingVersions(string placeId)
        {
            return await _context.RestaurantVersions
                .Include(rv => rv.CreatedByUser)
                .Where(rv => rv.PlaceId == placeId && rv.Status == "Pending")
                .OrderByDescending(rv => rv.CreatedAt)
                .ToListAsync();
        }

        // Get edit history for a restaurant
        public async Task<List<RestaurantEdit>> GetEditHistory(string placeId, int limit = 50)
        {
            return await _context.RestaurantEdits
                .Include(re => re.User)
                .Include(re => re.RestaurantVersion)
                .Where(re => re.PlaceId == placeId)
                .OrderByDescending(re => re.EditedAt)
                .Take(limit)
                .ToListAsync();
        }

        // Check if user is owner of restaurant
        public async Task<bool> IsUserOwner(string placeId, int userId)
        {
            return await _context.RestaurantOwners
                .AnyAsync(ro => ro.PlaceId == placeId && ro.UserId == userId && ro.VerificationStatus == "Verified");
        }

        // Get restaurant data (current version or Google Maps default)
        public async Task<RestaurantVersion?> GetRestaurantData(string placeId)
        {
            var currentVersion = await GetCurrentVersion(placeId);
            return currentVersion;
        }
    }
}

