using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Models
{
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        // Notify owner of pending edit
        public async Task NotifyOwnerPendingEdit(string placeId, int versionId)
        {
            var owner = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .FirstOrDefaultAsync(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified");

            if (owner != null)
            {
                // In a real implementation, you would:
                // 1. Create a notification record in a Notifications table
                // 2. Send email notification
                // 3. Send in-app notification
                // 4. Optionally send SMS or push notification

                // For now, we'll just log it
                // In production, implement proper notification system
            }
        }

        // Notify user that their edit was approved
        public async Task NotifyUserEditApproved(int userId, int versionId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // In a real implementation, send notification
                // For now, we'll just log it
            }
        }

        // Notify user that their edit was rejected
        public async Task NotifyUserEditRejected(int userId, int versionId, string reason)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // In a real implementation, send notification
                // For now, we'll just log it
            }
        }

        // Notify admin of pending claim
        public async Task NotifyAdminPendingClaim(int ownerId)
        {
            var admins = await _context.UserRoles
                .Include(ur => ur.User)
                .Where(ur => ur.RoleType == "Admin")
                .Select(ur => ur.User)
                .ToListAsync();

            foreach (var admin in admins)
            {
                // In a real implementation, send notification to each admin
                // For now, we'll just log it
            }
        }

        // Notify owner that their claim was approved
        public async Task NotifyOwnerClaimApproved(int ownerId)
        {
            var owner = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .FirstOrDefaultAsync(ro => ro.OwnerId == ownerId);

            if (owner != null)
            {
                // In a real implementation, send notification
                // For now, we'll just log it
            }
        }

        // Notify owner that their claim was rejected
        public async Task NotifyOwnerClaimRejected(int ownerId, string reason)
        {
            var owner = await _context.RestaurantOwners
                .Include(ro => ro.User)
                .FirstOrDefaultAsync(ro => ro.OwnerId == ownerId);

            if (owner != null)
            {
                // In a real implementation, send notification
                // For now, we'll just log it
            }
        }
    }
}

