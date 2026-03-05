using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Models
{
    public class NomsaurModel
    {
        public class User
        {
            [Key]
            public int UserId { get; set; }
            
            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;
            
            [Required]
            [EmailAddress]
            [StringLength(100)]
            public string Email { get; set; } = string.Empty;
            
            [StringLength(255)]
            public string? PasswordHash { get; set; } // Nullable for OAuth users
            
            // OAuth Provider Information
            [StringLength(50)]
            public string? Provider { get; set; } // "local", "google", "facebook"
            
            [StringLength(100)]
            public string? ProviderId { get; set; } // OAuth provider's user ID
            
            [StringLength(2000)]
            public string? ProviderData { get; set; } // JSON data from OAuth provider
            
            [NotMapped]
            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
            public string Password { get; set; } = string.Empty;
            
            [NotMapped]
            [Compare("Password", ErrorMessage = "Passwords do not match")]
            public string ConfirmPassword { get; set; } = string.Empty;
            
            [Required]
            public DateTime CreatedAt { get; set; }
            
            public DateTime? LastLogin { get; set; }
            
            public UserProfile? UserProfile { get; set; }
            
            [Obsolete("Use UserFoodTypes instead")]
            public ICollection<FoodPreference> FoodPreferences { get; set; } = new List<FoodPreference>();
            public ICollection<UserFoodType> UserFoodTypes { get; set; } = new List<UserFoodType>();
            public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
            public ICollection<UserBehavior> UserBehaviors { get; set; } = new List<UserBehavior>();
            public ICollection<UserPreferencePattern> UserPreferencePatterns { get; set; } = new List<UserPreferencePattern>();
            public ICollection<UserDietaryRestriction> UserDietaryRestrictions { get; set; } = new List<UserDietaryRestriction>();
            public ICollection<UserHealthCondition> UserHealthConditions { get; set; } = new List<UserHealthCondition>();
            public ICollection<UserFavoriteRestaurant> UserFavoriteRestaurants { get; set; } = new List<UserFavoriteRestaurant>();
            public ICollection<CommunityDietaryTagVote> CommunityDietaryTagVotes { get; set; } = new List<CommunityDietaryTagVote>();

            // Community Posts
            public ICollection<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();
            public ICollection<CommunityComment> CommunityComments { get; set; } = new List<CommunityComment>();
            public ICollection<CommunityPostReaction> CommunityPostReactions { get; set; } = new List<CommunityPostReaction>();

            // Direct Messaging (User <-> Verified Restaurant Owner)
            public ICollection<DirectConversation> DirectConversationsAsCustomer { get; set; } = new List<DirectConversation>();
            public ICollection<DirectConversation> DirectConversationsAsOwner { get; set; } = new List<DirectConversation>();
            public ICollection<DirectMessage> DirectMessagesSent { get; set; } = new List<DirectMessage>();
            
            // Restaurant Editing System relationships
            public ICollection<RestaurantVersion> CreatedRestaurantVersions { get; set; } = new List<RestaurantVersion>();
            public ICollection<RestaurantVersion> ApprovedRestaurantVersions { get; set; } = new List<RestaurantVersion>();
            public ICollection<RestaurantOwner> OwnedRestaurants { get; set; } = new List<RestaurantOwner>();
            public ICollection<RestaurantOwner> VerifiedRestaurantOwners { get; set; } = new List<RestaurantOwner>();
            public ICollection<RestaurantEdit> RestaurantEdits { get; set; } = new List<RestaurantEdit>();
            public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
            public ICollection<UserRole> GrantedRoles { get; set; } = new List<UserRole>();
        }

        public class UserProfile
        {
            [Key]
            public int ProfileId { get; set; }
            
            [Required]
            public int UserId { get; set; }
            
            // REMOVED: DietaryPreference (replaced by UserDietaryRestriction table)
            // REMOVED: FavoriteFood (replaced by behavioral analysis and food preference ranking)
            
            // EVIDENCE-BASED DEMOGRAPHIC DATA (Required for Age-Specific Recommendations)
            [Range(13, 120)]
            public int? Age { get; set; }
            
            [StringLength(20)]
            public string? Gender { get; set; } // 'Male', 'Female', 'Other'
            
            [StringLength(20)]
            public string? ActivityLevel { get; set; } // 'Sedentary', 'Moderate', 'Active'
            
            // LOCATION & PREFERENCES (Essential for Recommendations)
            [StringLength(200)]
            public string? PreferredLocation { get; set; }
            
            [StringLength(20)]
            public string? PriceRange { get; set; } // 'Budget', 'Moderate', 'Premium'
            
            // REMOVED: LastKnownLatitude/Longitude - Using real-time location from session instead
            
            [Required]
            public DateTime LastUpdated { get; set; }
            
            [ForeignKey("UserId")]
            public User User { get; set; }
            
            // REMOVED: Navigation properties moved to User entity to avoid duplication
        }

        public class FoodType
        {
            [Key]
            public int FoodTypeId { get; set; }
            
            [Required]
            [StringLength(50)]
            public string Name { get; set; }
            
            [StringLength(200)]
            public string Description { get; set; }
            
            // EVIDENCE-BASED NUTRITIONAL PROPERTIES (Required for Algorithm)
            [StringLength(50)]
            public string CuisineType { get; set; } = string.Empty;
            
            // General Nutritional Quality (S_quality calculation)
            public bool IsHealthy { get; set; } = false;
            public bool IsLowCalorie { get; set; } = false;
            public bool IsHighProtein { get; set; } = false;
            
            // Micronutrient Density (S_micronutrient calculation)
            public bool IsNutrientDense { get; set; } = false;
            public bool IsVitaminRich { get; set; } = false;
            
            // Health-Specific Properties (S_health-specific calculation)
            public bool IsLowGlycemic { get; set; } = false; // Diabetes support
            public bool IsLowSodium { get; set; } = false; // Hypertension support
            public bool IsHeartHealthy { get; set; } = false; // Heart disease support
            public bool IsIronRich { get; set; } = false; // Anemia support
            
            // Dietary Restriction Compliance
            public bool ContainsGluten { get; set; } = false;
            public bool ContainsDairy { get; set; } = false;
            public bool ContainsNuts { get; set; } = false;
            public bool ContainsMeat { get; set; } = false;
            public bool IsVegetarian { get; set; } = false;
            public bool IsVegan { get; set; } = false;
            
            // MEAL TYPE CLASSIFICATION (For Personalized Meal Recommendations)
            public bool IsBreakfast { get; set; } = false;
            public bool IsLunch { get; set; } = false;
            public bool IsDinner { get; set; } = false;
            public bool IsSnacks { get; set; } = false;
            public bool IsDessert { get; set; } = false;
            public bool IsBeverage { get; set; } = false;
            
            [Obsolete("Use UserFoodTypes instead")]
            public ICollection<FoodPreference> FoodPreferences { get; set; }
            public ICollection<UserFoodType> UserFoodTypes { get; set; }
        }

        // REMOVED: Establishment table - We use Google Maps API for restaurant data instead of local storage

        [Obsolete("This class is deprecated. Use UserFoodType instead for all food preference functionality.")]
        public class FoodPreference
        {
            [Key]
            public int PreferenceId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [ForeignKey("FoodType")]
            public int FoodTypeId { get; set; }
            
            [Required]
            [StringLength(20)]
            public string PreferenceLevel { get; set; }
            
            // NEW: Numeric preference score (1-10). Higher means stronger preference.
            // Kept alongside PreferenceLevel for backward compatibility.
            [Range(1, 10)]
            public int PreferenceScore { get; set; } = 5;
            
            public DateTime? LastSelected { get; set; }
            
            public User User { get; set; }
            public FoodType FoodType { get; set; }
        }

        // NEW: UserFoodTypes - Stores user's onboarding food preferences
        public class UserFoodType
        {
            [Key]
            public int UserFoodTypeId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [ForeignKey("FoodType")]
            public int FoodTypeId { get; set; }
            
            [Required]
            [StringLength(20)]
            public string PreferenceLevel { get; set; } = "Preferred"; // "Preferred", "Avoided", "Neutral"
            
            [Range(1, 10)]
            public int PreferenceScore { get; set; } = 7; // Higher default for onboarding selections
            
            public DateTime AddedAt { get; set; } = DateTime.Now;
            
            public DateTime? LastSelected { get; set; }
            
            public User User { get; set; }
            public FoodType FoodType { get; set; }
        }

        public class ChatSession
        {
            [Key]
            public int SessionId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            public DateTime StartedAt { get; set; }
            
            public DateTime? EndedAt { get; set; }
            
            public User User { get; set; }
            public ICollection<ChatMessage> ChatMessages { get; set; }
        }

        public class ChatMessage
        {
            [Key]
            public int MessageId { get; set; }
            
            [Required]
            [ForeignKey("ChatSession")]
            public int SessionId { get; set; }
            
            [Required]
            [StringLength(20)]
            public string Sender { get; set; }
            
            [Required]
            [StringLength(1000)]
            public string MessageText { get; set; }
            
            [Required]
            public DateTime Timestamp { get; set; }
            
            [Required]
            [StringLength(50)]
            public string IntentDetected { get; set; } = string.Empty;
            
            [Required]
            [StringLength(500)]
            public string ParametersJson { get; set; } = string.Empty;
            
            public ChatSession ChatSession { get; set; }
        }

        // ============================================
        // DIRECT MESSAGING (User <-> Verified Owner)
        // ============================================

        public class DirectConversation
        {
            [Key]
            public int ConversationId { get; set; }

            // Google Place ID (restaurant)
            [Required]
            [StringLength(255)]
            public string PlaceId { get; set; } = string.Empty;

            // Customer user
            [Required]
            [ForeignKey("CustomerUser")]
            public int CustomerUserId { get; set; }

            // Verified owner user
            [Required]
            [ForeignKey("OwnerUser")]
            public int OwnerUserId { get; set; }

            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? LastMessageAt { get; set; }

            public User CustomerUser { get; set; } = null!;
            public User OwnerUser { get; set; } = null!;

            public ICollection<DirectMessage> Messages { get; set; } = new List<DirectMessage>();
        }

        public class DirectMessage
        {
            [Key]
            public int MessageId { get; set; }

            [Required]
            [ForeignKey("DirectConversation")]
            public int ConversationId { get; set; }

            [Required]
            [ForeignKey("SenderUser")]
            public int SenderUserId { get; set; }

            [Required]
            [StringLength(2000)]
            public string Body { get; set; } = string.Empty;

            [Required]
            public DateTime SentAt { get; set; } = DateTime.UtcNow;

            public DateTime? ReadAt { get; set; }

            public DirectConversation DirectConversation { get; set; } = null!;
            public User SenderUser { get; set; } = null!;
        }

        // ENHANCED: UserBehavior now captures ALL user interactions including recommendations
        public class UserBehavior
        {
            [Key]
            public int BehaviorId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [StringLength(50)]
            public string Action { get; set; } = string.Empty; // "search", "select", "rate", "reject", "recommendation"
            
            [StringLength(4000)]
            public string? Context { get; set; } // time, location, mood, query, food preferences
            
            [StringLength(2000)]
            public string? Result { get; set; } // success/failure, selected item, recommendation details
            
            [Required]
            public DateTime Timestamp { get; set; }
            
            [Range(1, 5)]
            public int? Satisfaction { get; set; } // 1-5 rating (replaces UserRating from RecommendationHistory)
            
            [StringLength(100)]
            public string? IntentDetected { get; set; }
            
            [StringLength(1000)]
            public string ParametersJson { get; set; } = string.Empty;
            
            // RECOMMENDATION-SPECIFIC FIELDS (from removed RecommendationHistory)
            [StringLength(200)]
            public string? ReasonForRecommendation { get; set; } // Why this was recommended
            
            [StringLength(1000)]
            public string? EstablishmentInfo { get; set; } // JSON with establishment details
            
            [StringLength(100)]
            public string? EstablishmentName { get; set; } // Quick access to establishment name
            
            [StringLength(50)]
            public string? CuisineType { get; set; } // Food cuisine type
            
            public DateTime? SelectedAt { get; set; } // When user selected the recommendation
            
            public User User { get; set; } = null!;
        }

        // REMOVED: RecommendationHistory class - Functionality moved to UserBehavior table
        // UserBehavior now captures all recommendation tracking with Action="recommendation" and Result fields

        public class UserPreferencePattern
        {
            [Key]
            public int PatternId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [StringLength(50)]
            public string PatternType { get; set; } = string.Empty; // "time", "location", "cuisine", "price"
            
            [Required]
            [StringLength(200)]
            public string PatternValue { get; set; } = string.Empty;
            
            [Required]
            public decimal Confidence { get; set; } // 0.0 to 1.0
            
            [Required]
            public DateTime LastObserved { get; set; }
            
            [Required]
            public int ObservationCount { get; set; }
            
            // EVIDENCE-BASED INTERACTION METRICS (Required for Confidence Calculation)
            [Required]
            public int TotalInteractions { get; set; } = 0; // Total interactions for this pattern type
            
            [Required]
            public int SuccessfulInteractions { get; set; } = 0; // Successful interactions (user satisfaction >= 4)
            
            public User User { get; set; } = null!;
        }

        public class DietaryRestriction
        {
            [Key]
            public int DietaryRestrictionId { get; set; }
            
            [Required]
            [StringLength(50)]
            public string Name { get; set; } = string.Empty; // e.g., "Vegan", "Gluten-Free", "Dairy-Free"
            
            [StringLength(200)]
            public string Description { get; set; } = string.Empty;
            
            public ICollection<UserDietaryRestriction> UserDietaryRestrictions { get; set; }
            public ICollection<CommunityDietaryTagVote> CommunityDietaryTagVotes { get; set; } = new List<CommunityDietaryTagVote>();
            public ICollection<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();
        }

        public class HealthCondition
        {
            [Key]
            public int HealthConditionId { get; set; }
            
            [Required]
            [StringLength(50)]
            public string Name { get; set; } = string.Empty; // e.g., "Diabetes", "Hypertension", "Heart Disease"
            
            [StringLength(200)]
            public string Description { get; set; } = string.Empty;
            
            [StringLength(500)]
            public string RecommendedDiets { get; set; } = string.Empty; // JSON array of recommended dietary patterns
            
            public ICollection<UserHealthCondition> UserHealthConditions { get; set; }
        }

        public class UserDietaryRestriction
        {
            [Key]
            public int UserDietaryRestrictionId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [ForeignKey("DietaryRestriction")]
            public int DietaryRestrictionId { get; set; }
            
            [Required]
            [Range(1, 10)]
            public int ImportanceLevel { get; set; } = 1; // 1-10 scale, affects weight in algorithm
            
            [Required]
            public DateTime AddedAt { get; set; }
            
            public User User { get; set; } = null!;
            public DietaryRestriction DietaryRestriction { get; set; } = null!;
        }

        public class UserHealthCondition
        {
            [Key]
            public int UserHealthConditionId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [ForeignKey("HealthCondition")]
            public int HealthConditionId { get; set; }
            
            [Required]
            [Range(1, 10)]
            public int SeverityLevel { get; set; } = 1; // 1-10 scale, affects weight in algorithm
            
            [Required]
            public DateTime DiagnosedAt { get; set; }
            
            public User User { get; set; } = null!;
            public HealthCondition HealthCondition { get; set; } = null!;
        }

        public class FoodPreferenceScore
        {
            public string FoodName { get; set; } = string.Empty;
            public string CuisineType { get; set; } = string.Empty;
            public decimal TotalScore { get; set; }
            public string Reason { get; set; } = string.Empty;
            public List<string> MatchingRestrictions { get; set; } = new List<string>();
            public List<string> MatchingHealthConditions { get; set; } = new List<string>();
            public bool IsSafeForUser { get; set; } = true;
        }

        // User Favorite Restaurant - Stores user's favorite places from Google Maps
        public class UserFavoriteRestaurant
        {
            [Key]
            public int FavoriteId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            // Google Place ID (unique identifier from Google Maps API)
            [Required]
            [StringLength(255)]
            public string PlaceId { get; set; } = string.Empty;
            
            // Restaurant details (cached from Google Maps)
            [Required]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;
            
            [StringLength(500)]
            public string? Address { get; set; }
            
            public decimal? Rating { get; set; }
            
            public int? PriceLevel { get; set; }
            
            [StringLength(2000)]
            public string? PhotoReference { get; set; } // Google photo reference
            
            public decimal? Latitude { get; set; }
            
            public decimal? Longitude { get; set; }
            
            [StringLength(100)]
            public string? PhoneNumber { get; set; }
            
            [StringLength(500)]
            public string? Website { get; set; }
            
            [StringLength(1000)]
            public string? Types { get; set; } // JSON array of place types
            
            [Required]
            public DateTime AddedAt { get; set; } = DateTime.Now;
            
            public DateTime? LastViewed { get; set; }
            
            [StringLength(500)]
            public string? UserNotes { get; set; }
            
            public User User { get; set; } = null!;
        }

        // ============================================
        // COMMUNITY DIETARY TAG VOTES (Crowdsourced)
        // ============================================

        // CommunityRestaurant - Global cache of Google place details for community features
        public class CommunityRestaurant
        {
            [Key]
            [StringLength(255)]
            public string PlaceId { get; set; } = string.Empty;

            [Required]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;

            [StringLength(500)]
            public string? Address { get; set; }

            public decimal? Rating { get; set; }

            public int? PriceLevel { get; set; }

            [StringLength(2000)]
            public string? PhotoReference { get; set; }

            public decimal? Latitude { get; set; }

            public decimal? Longitude { get; set; }

            [StringLength(100)]
            public string? PhoneNumber { get; set; }

            [StringLength(500)]
            public string? Website { get; set; }

            [StringLength(1000)]
            public string? Types { get; set; } // JSON array of place types

            [Required]
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

            public ICollection<CommunityDietaryTagVote> Votes { get; set; } = new List<CommunityDietaryTagVote>();
        }

        // CommunityDietaryTagVote - One vote per user per tag per place
        public class CommunityDietaryTagVote
        {
            [Key]
            public int VoteId { get; set; }

            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }

            [Required]
            [StringLength(255)]
            [ForeignKey("CommunityRestaurant")]
            public string PlaceId { get; set; } = string.Empty;

            [Required]
            [ForeignKey("DietaryRestriction")]
            public int DietaryRestrictionId { get; set; }

            [Required]
            public DateTime VotedAt { get; set; } = DateTime.UtcNow;

            public User User { get; set; } = null!;
            public CommunityRestaurant CommunityRestaurant { get; set; } = null!;
            public DietaryRestriction DietaryRestriction { get; set; } = null!;
        }

        // ============================================
        // RESTAURANT EDITING SYSTEM (Wikipedia-like)
        // ============================================

        // Restaurant Version - Stores different versions of restaurant data
        public class RestaurantVersion
        {
            [Key]
            public int VersionId { get; set; }
            
            // Google Place ID (unique identifier from Google Maps API)
            [Required]
            [StringLength(255)]
            public string PlaceId { get; set; } = string.Empty;
            
            // Version number (1, 2, 3, etc.)
            [Required]
            public int VersionNumber { get; set; }
            
            // Restaurant details (editable fields)
            [Required]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;
            
            [StringLength(500)]
            public string? Address { get; set; }
            
            [StringLength(100)]
            public string? PhoneNumber { get; set; }
            
            [StringLength(500)]
            public string? Website { get; set; }
            
            [StringLength(2000)]
            public string? Description { get; set; }
            
            [StringLength(500)]
            public string? CuisineType { get; set; }
            
            [StringLength(50)]
            public string? PriceRange { get; set; }
            
            [StringLength(1000)]
            public string? OpeningHours { get; set; } // JSON array of opening hours
            
            [StringLength(2000)]
            public string? SpecialFeatures { get; set; } // Dietary options, amenities, etc.
            
            // Community-managed fields (not provided by Places API)
            [StringLength(500)]
            public string? FacebookUrl { get; set; }

            [StringLength(500)]
            public string? InstagramUrl { get; set; }

            // Free-text or comma/newline-separated list (e.g. "Dine-in, Takeout")
            [StringLength(1000)]
            public string? ServiceOptions { get; set; }

            // Free-text or comma/newline-separated list (e.g. "Breakfast, Dinner")
            [StringLength(1000)]
            public string? PopularFor { get; set; }

            // Owner/Admin curated menu items with ingredient details (newline-separated entries like: "Bulgogi - ...")
            [StringLength(8000)]
            public string? MenuItemsAndIngredients { get; set; }
            
            // Status: "Current", "Pending", "Rejected", "Historical"
            [Required]
            [StringLength(20)]
            public string Status { get; set; } = "Pending"; // Current, Pending, Rejected, Historical
            
            // Who created this version
            [ForeignKey("CreatedByUser")]
            public int CreatedByUserId { get; set; }
            
            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            
            // Approval information (if applicable)
            [ForeignKey("ApprovedByUser")]
            public int? ApprovedByUserId { get; set; }
            
            public DateTime? ApprovedAt { get; set; }
            
            [StringLength(500)]
            public string? RejectionReason { get; set; }
            
            // Is this the current active version?
            public bool IsCurrent { get; set; } = false;
            
            // Navigation properties
            public User CreatedByUser { get; set; } = null!;
            public User? ApprovedByUser { get; set; }
        }

        // Restaurant Owner - Links users to restaurants they own
        public class RestaurantOwner
        {
            [Key]
            public int OwnerId { get; set; }
            
            // Google Place ID (unique identifier from Google Maps API)
            [Required]
            [StringLength(255)]
            public string PlaceId { get; set; } = string.Empty;
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            // Restaurant name (cached for display)
            [Required]
            [StringLength(200)]
            public string RestaurantName { get; set; } = string.Empty;
            
            // Verification status
            [Required]
            [StringLength(20)]
            public string VerificationStatus { get; set; } = "Pending"; // Pending, Verified, Rejected
            
            [Required]
            public DateTime ClaimedAt { get; set; } = DateTime.Now;
            
            public DateTime? VerifiedAt { get; set; }
            
            [ForeignKey("VerifiedByUser")]
            public int? VerifiedByUserId { get; set; } // Admin who verified
            
            [StringLength(500)]
            public string? RejectionReason { get; set; }
            
            // Navigation properties
            public User User { get; set; } = null!;
            public User? VerifiedByUser { get; set; }
        }

        // Owner Verification - Stores verification documents
        public class OwnerVerification
        {
            [Key]
            public int VerificationId { get; set; }
            
            [Required]
            [ForeignKey("RestaurantOwner")]
            public int OwnerId { get; set; }
            
            // Verification documents
            [StringLength(500)]
            public string? BusinessLicensePath { get; set; } // Path to uploaded file
            
            [StringLength(100)]
            public string? BusinessEmail { get; set; }
            
            [StringLength(20)]
            public string? BusinessPhone { get; set; }
            
            [StringLength(500)]
            public string? BusinessAddress { get; set; }
            
            [StringLength(200)]
            public string? BusinessName { get; set; }
            
            [StringLength(500)]
            public string? AdditionalNotes { get; set; }
            
            // Email verification
            public bool EmailVerified { get; set; } = false;
            
            public DateTime? EmailVerifiedAt { get; set; }
            
            // Phone verification
            public bool PhoneVerified { get; set; } = false;
            
            public DateTime? PhoneVerifiedAt { get; set; }
            
            [Required]
            public DateTime SubmittedAt { get; set; } = DateTime.Now;
            
            // Navigation properties
            public RestaurantOwner RestaurantOwner { get; set; } = null!;
        }

        // Restaurant Edit - Tracks all edit history
        public class RestaurantEdit
        {
            [Key]
            public int EditId { get; set; }
            
            // Google Place ID
            [Required]
            [StringLength(255)]
            public string PlaceId { get; set; } = string.Empty;
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [ForeignKey("RestaurantVersion")]
            public int VersionId { get; set; }
            
            // Edit action: "Created", "Updated", "Deleted"
            [Required]
            [StringLength(20)]
            public string Action { get; set; } = "Updated"; // Created, Updated, Deleted
            
            // Field changes (JSON format: {"field": "old_value -> new_value"})
            [StringLength(2000)]
            public string? FieldChanges { get; set; }
            
            [Required]
            public DateTime EditedAt { get; set; } = DateTime.Now;
            
            // IP Address for security tracking
            [StringLength(50)]
            public string? IpAddress { get; set; }
            
            // Navigation properties
            public User User { get; set; } = null!;
            public RestaurantVersion RestaurantVersion { get; set; } = null!;
        }

        // ============================================
        // RESTAURANT HEALTH Q&A (Community + Verified Replies)
        // ============================================

        public class RestaurantQuestion
        {
            [Key]
            public int QuestionId { get; set; }

            [Required]
            [StringLength(255)]
            public string PlaceId { get; set; } = string.Empty;

            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }

            [Required]
            [StringLength(800)]
            public string QuestionText { get; set; } = string.Empty;

            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }

            // Navigation
            public User User { get; set; } = null!;
            public ICollection<RestaurantAnswer> Answers { get; set; } = new List<RestaurantAnswer>();
        }

        public class RestaurantAnswer
        {
            [Key]
            public int AnswerId { get; set; }

            [Required]
            [ForeignKey("RestaurantQuestion")]
            public int QuestionId { get; set; }

            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }

            [Required]
            [StringLength(1200)]
            public string AnswerText { get; set; } = string.Empty;

            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // Verified answers (owner/admin)
            public bool IsVerified { get; set; } = false;

            [StringLength(20)]
            public string? VerifiedByRole { get; set; } // "Owner", "Admin"

            public DateTime? VerifiedAt { get; set; }

            [ForeignKey("VerifiedByUser")]
            public int? VerifiedByUserId { get; set; }

            // Navigation
            public RestaurantQuestion RestaurantQuestion { get; set; } = null!;
            public User User { get; set; } = null!;
            public User? VerifiedByUser { get; set; }
        }

        // UserRole - Admin and Moderator role system
        public class UserRole
        {
            [Key]
            public int RoleId { get; set; }
            
            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }
            
            [Required]
            [StringLength(20)]
            public string RoleType { get; set; } = string.Empty; // "Admin", "Moderator"
            
            [ForeignKey("GrantedByUser")]
            public int? GrantedByUserId { get; set; }
            
            [Required]
            public DateTime GrantedAt { get; set; } = DateTime.Now;
            
            // Navigation properties
            public User User { get; set; } = null!;
            public User? GrantedByUser { get; set; }
        }

        // ============================================
        // COMMUNITY POSTS (Dietary Tag Communities)
        // ============================================

        public class CommunityPost
        {
            [Key]
            public int PostId { get; set; }

            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }

            // Scope: Tag Lounge (dietary tag only) or Restaurant Corner (tag + place)
            [ForeignKey("DietaryRestriction")]
            public int? DietaryRestrictionId { get; set; }

            [StringLength(255)]
            public string? PlaceId { get; set; } // Nullable for Tag Lounge posts

            [Required]
            [StringLength(20)]
            public string PostType { get; set; } = "Discussion"; // Discussion, Tip, Question, Find, Meetup, OwnerUpdate

            [StringLength(200)]
            public string? Title { get; set; }

            [Required]
            [StringLength(2000)]
            public string Body { get; set; } = string.Empty;

            // Optional image attached to the post (stored as a web-accessible URL/path)
            [StringLength(500)]
            public string? ImageUrl { get; set; }

            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }

            // Verified poster (owner/admin)
            public bool IsVerifiedPoster { get; set; } = false;

            [StringLength(20)]
            public string? VerifiedByRole { get; set; } // "Owner", "Admin"

            public DateTime? VerifiedAt { get; set; }

            // Soft delete
            public bool IsDeleted { get; set; } = false;

            // Navigation
            public User User { get; set; } = null!;
            public DietaryRestriction? DietaryRestriction { get; set; }
            public ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
            public ICollection<CommunityPostReaction> Reactions { get; set; } = new List<CommunityPostReaction>();
        }

        public class CommunityComment
        {
            [Key]
            public int CommentId { get; set; }

            [Required]
            [ForeignKey("CommunityPost")]
            public int PostId { get; set; }

            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }

            [Required]
            [StringLength(1200)]
            public string Body { get; set; } = string.Empty;

            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }

            // Soft delete
            public bool IsDeleted { get; set; } = false;

            // Navigation
            public CommunityPost CommunityPost { get; set; } = null!;
            public User User { get; set; } = null!;
        }

        public class CommunityPostReaction
        {
            [Key]
            public int ReactionId { get; set; }

            [Required]
            [ForeignKey("CommunityPost")]
            public int PostId { get; set; }

            [Required]
            [ForeignKey("User")]
            public int UserId { get; set; }

            [Required]
            [StringLength(20)]
            public string ReactionType { get; set; } = "Helpful"; // Helpful, Interested, Tried, Saved

            [Required]
            public DateTime ReactedAt { get; set; } = DateTime.UtcNow;

            // Navigation
            public CommunityPost CommunityPost { get; set; } = null!;
            public User User { get; set; } = null!;
        }
    }
}
