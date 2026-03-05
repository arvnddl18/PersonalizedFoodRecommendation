using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Models
{
    public class OwnerVerificationService
    {
        private readonly AppDbContext _context;

        public OwnerVerificationService(AppDbContext context)
        {
            _context = context;
        }

        // Claim a restaurant (create owner request)
        public async Task<RestaurantOwner> ClaimRestaurant(
            string placeId,
            int userId,
            string restaurantName,
            string? businessName = null,
            string? businessEmail = null,
            string? businessPhone = null,
            string? businessAddress = null,
            string? businessLicensePath = null,
            string? additionalNotes = null)
        {
            // Check if restaurant is already claimed
            var existingOwner = await _context.RestaurantOwners
                .Where(ro => ro.PlaceId == placeId)
                .FirstOrDefaultAsync();

            if (existingOwner != null && existingOwner.VerificationStatus == "Verified")
            {
                throw new InvalidOperationException("This restaurant is already claimed by another owner.");
            }

            // If pending claim exists, update it instead of creating new
            if (existingOwner != null && existingOwner.VerificationStatus == "Pending")
            {
                if (existingOwner.UserId != userId)
                {
                    throw new InvalidOperationException("This restaurant already has a pending claim from another user.");
                }
                // Update existing claim
                existingOwner.RestaurantName = restaurantName;
                existingOwner.ClaimedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new owner claim
                existingOwner = new RestaurantOwner
                {
                    PlaceId = placeId,
                    UserId = userId,
                    RestaurantName = restaurantName,
                    VerificationStatus = "Pending",
                    ClaimedAt = DateTime.UtcNow
                };

                _context.RestaurantOwners.Add(existingOwner);
                await _context.SaveChangesAsync();
            }

            // Create or update verification documents
            var verification = await _context.OwnerVerifications
                .Where(ov => ov.OwnerId == existingOwner.OwnerId)
                .FirstOrDefaultAsync();

            if (verification == null)
            {
                verification = new OwnerVerification
                {
                    OwnerId = existingOwner.OwnerId,
                    BusinessLicensePath = businessLicensePath,
                    BusinessEmail = businessEmail,
                    BusinessPhone = businessPhone,
                    BusinessAddress = businessAddress,
                    BusinessName = businessName,
                    AdditionalNotes = additionalNotes,
                    SubmittedAt = DateTime.UtcNow
                };

                _context.OwnerVerifications.Add(verification);
            }
            else
            {
                // Update existing verification
                if (!string.IsNullOrEmpty(businessLicensePath))
                    verification.BusinessLicensePath = businessLicensePath;
                if (!string.IsNullOrEmpty(businessEmail))
                    verification.BusinessEmail = businessEmail;
                if (!string.IsNullOrEmpty(businessPhone))
                    verification.BusinessPhone = businessPhone;
                if (!string.IsNullOrEmpty(businessAddress))
                    verification.BusinessAddress = businessAddress;
                if (!string.IsNullOrEmpty(businessName))
                    verification.BusinessName = businessName;
                if (!string.IsNullOrEmpty(additionalNotes))
                    verification.AdditionalNotes = additionalNotes;
            }

            await _context.SaveChangesAsync();
            return existingOwner;
        }

        // Verify email (when owner clicks verification link)
        public async Task<bool> VerifyEmail(int ownerId, string email)
        {
            var verification = await _context.OwnerVerifications
                .Include(ov => ov.RestaurantOwner)
                .Where(ov => ov.OwnerId == ownerId && ov.BusinessEmail == email)
                .FirstOrDefaultAsync();

            if (verification == null)
                return false;

            verification.EmailVerified = true;
            verification.EmailVerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Verify phone (when owner receives verification code)
        public async Task<bool> VerifyPhone(int ownerId, string phone)
        {
            var verification = await _context.OwnerVerifications
                .Include(ov => ov.RestaurantOwner)
                .Where(ov => ov.OwnerId == ownerId && ov.BusinessPhone == phone)
                .FirstOrDefaultAsync();

            if (verification == null)
                return false;

            verification.PhoneVerified = true;
            verification.PhoneVerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Approve owner claim (admin action)
        public async Task<bool> ApproveOwnerClaim(int ownerId, int adminUserId)
        {
            var owner = await _context.RestaurantOwners
                .FirstOrDefaultAsync(ro => ro.OwnerId == ownerId);

            if (owner == null || owner.VerificationStatus != "Pending")
                return false;

            owner.VerificationStatus = "Verified";
            owner.VerifiedAt = DateTime.UtcNow;
            owner.VerifiedByUserId = adminUserId;

            await _context.SaveChangesAsync();
            return true;
        }

        // Reject owner claim (admin action)
        public async Task<bool> RejectOwnerClaim(int ownerId, int adminUserId, string rejectionReason)
        {
            var owner = await _context.RestaurantOwners
                .FirstOrDefaultAsync(ro => ro.OwnerId == ownerId);

            if (owner == null || owner.VerificationStatus != "Pending")
                return false;

            owner.VerificationStatus = "Rejected";
            owner.RejectionReason = rejectionReason;
            owner.VerifiedByUserId = adminUserId;

            await _context.SaveChangesAsync();
            return true;
        }

        // Get pending owner claims (for admin review)
        public async Task<List<RestaurantOwner>> GetPendingClaims()
        {
            return await _context.RestaurantOwners
                .Include(ro => ro.User)
                .Include(ro => ro.VerifiedByUser)
                .Where(ro => ro.VerificationStatus == "Pending")
                .OrderByDescending(ro => ro.ClaimedAt)
                .ToListAsync();
        }

        // Get owner's restaurants
        public async Task<List<RestaurantOwner>> GetUserRestaurants(int userId)
        {
            return await _context.RestaurantOwners
                .Where(ro => ro.UserId == userId)
                .OrderByDescending(ro => ro.ClaimedAt)
                .ToListAsync();
        }

        // Get owner by place ID
        public async Task<RestaurantOwner?> GetOwnerByPlaceId(string placeId)
        {
            return await _context.RestaurantOwners
                .Include(ro => ro.User)
                .Where(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified")
                .FirstOrDefaultAsync();
        }

        // Get verification documents for an owner
        public async Task<OwnerVerification?> GetVerification(int ownerId)
        {
            return await _context.OwnerVerifications
                .Include(ov => ov.RestaurantOwner)
                .Where(ov => ov.OwnerId == ownerId)
                .FirstOrDefaultAsync();
        }

        // Check if restaurant is claimed
        public async Task<bool> IsRestaurantClaimed(string placeId)
        {
            return await _context.RestaurantOwners
                .AnyAsync(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified");
        }

        // Check if user has pending claim
        public async Task<bool> HasPendingClaim(string placeId, int userId)
        {
            return await _context.RestaurantOwners
                .AnyAsync(ro => ro.PlaceId == placeId && ro.UserId == userId && ro.VerificationStatus == "Pending");
        }
    }
}

