using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Capstone.Models;

using static Capstone.Models.NomsaurModel;

namespace Capstone.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // EVIDENCE-BASED ALGORITHM ESSENTIAL TABLES ONLY
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<FoodType> FoodTypes { get; set; }
        [Obsolete("Use UserFoodTypes instead")]
        public DbSet<FoodPreference> FoodPreferences { get; set; }
        public DbSet<UserFoodType> UserFoodTypes { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<DirectConversation> DirectConversations { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<UserBehavior> UserBehaviors { get; set; }
        public DbSet<UserPreferencePattern> UserPreferencePatterns { get; set; }
        public DbSet<DietaryRestriction> DietaryRestrictions { get; set; }
        public DbSet<HealthCondition> HealthConditions { get; set; }
        public DbSet<UserDietaryRestriction> UserDietaryRestrictions { get; set; }
        public DbSet<UserHealthCondition> UserHealthConditions { get; set; }
        public DbSet<UserFavoriteRestaurant> UserFavoriteRestaurants { get; set; }
        public DbSet<CommunityRestaurant> CommunityRestaurants { get; set; }
        public DbSet<CommunityDietaryTagVote> CommunityDietaryTagVotes { get; set; }
        
        // Restaurant Editing System
        public DbSet<RestaurantVersion> RestaurantVersions { get; set; }
        public DbSet<RestaurantOwner> RestaurantOwners { get; set; }
        public DbSet<OwnerVerification> OwnerVerifications { get; set; }
        public DbSet<RestaurantEdit> RestaurantEdits { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        
        // Restaurant Health Q&A
        public DbSet<RestaurantQuestion> RestaurantQuestions { get; set; }
        public DbSet<RestaurantAnswer> RestaurantAnswers { get; set; }
        
        // Community Posts
        public DbSet<CommunityPost> CommunityPosts { get; set; }
        public DbSet<CommunityComment> CommunityComments { get; set; }
        public DbSet<CommunityPostReaction> CommunityPostReactions { get; set; }
        
        // REMOVED: RecommendationHistories - Redundant (UserBehaviors captures this functionality)
        // REMOVED: Establishments - Using Google Maps API for restaurant data

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // REMOVED: UserProfile coordinate precision - using real-time location instead

            // Configure decimal precision for confidence scores
            modelBuilder.Entity<UserPreferencePattern>()
                .Property(p => p.Confidence)
                .HasPrecision(3, 2);

            // REMOVED: Establishment coordinate precision configuration - table removed

            // User - UserProfile relationship (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - FoodPreference relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.FoodPreferences)
                .WithOne(fp => fp.User)
                .HasForeignKey(fp => fp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - ChatSession relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.ChatSessions)
                .WithOne(cs => cs.User)
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - UserBehavior relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserBehaviors)
                .WithOne(ub => ub.User)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // REMOVED: RecommendationHistory relationship - functionality moved to UserBehavior

            // User - UserPreferencePattern relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserPreferencePatterns)
                .WithOne(upp => upp.User)
                .HasForeignKey(upp => upp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // FoodType - FoodPreference relationship (One-to-Many)
            modelBuilder.Entity<FoodType>()
                .HasMany(ft => ft.FoodPreferences)
                .WithOne(fp => fp.FoodType)
                .HasForeignKey(fp => fp.FoodTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // FoodType - UserFoodType relationship (One-to-Many)
            modelBuilder.Entity<FoodType>()
                .HasMany(ft => ft.UserFoodTypes)
                .WithOne(uft => uft.FoodType)
                .HasForeignKey(uft => uft.FoodTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - UserFoodType relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserFoodTypes)
                .WithOne(uft => uft.User)
                .HasForeignKey(uft => uft.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure numeric precision/range defaults if needed
            modelBuilder.Entity<FoodPreference>()
                .Property(p => p.PreferenceScore)
                .HasDefaultValue(5);

            modelBuilder.Entity<UserFoodType>()
                .Property(uft => uft.PreferenceScore)
                .HasDefaultValue(7);

            // ChatSession - ChatMessage relationship (One-to-Many)
            modelBuilder.Entity<ChatSession>()
                .HasMany(cs => cs.ChatMessages)
                .WithOne(cm => cm.ChatSession)
                .HasForeignKey(cm => cm.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Direct Messaging relationships
            modelBuilder.Entity<DirectConversation>()
                .HasOne(c => c.CustomerUser)
                .WithMany(u => u.DirectConversationsAsCustomer)
                .HasForeignKey(c => c.CustomerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DirectConversation>()
                .HasOne(c => c.OwnerUser)
                .WithMany(u => u.DirectConversationsAsOwner)
                .HasForeignKey(c => c.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DirectConversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.DirectConversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DirectMessage>()
                .HasOne(m => m.SenderUser)
                .WithMany(u => u.DirectMessagesSent)
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DirectConversation>()
                .HasIndex(c => new { c.PlaceId, c.CustomerUserId, c.OwnerUserId })
                .IsUnique();

            modelBuilder.Entity<DirectConversation>()
                .HasIndex(c => c.OwnerUserId);

            modelBuilder.Entity<DirectConversation>()
                .HasIndex(c => c.CustomerUserId);

            modelBuilder.Entity<DirectConversation>()
                .HasIndex(c => c.LastMessageAt);

            modelBuilder.Entity<DirectMessage>()
                .HasIndex(m => new { m.ConversationId, m.SentAt });

            // REMOVED: Establishment - FoodType relationship - Establishment table removed


            // User - UserDietaryRestriction relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserDietaryRestrictions)
                .WithOne(udr => udr.User)
                .HasForeignKey(udr => udr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - UserHealthCondition relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserHealthConditions)
                .WithOne(uhc => uhc.User)
                .HasForeignKey(uhc => uhc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // DietaryRestriction - UserDietaryRestriction relationship (One-to-Many)
            modelBuilder.Entity<DietaryRestriction>()
                .HasMany(dr => dr.UserDietaryRestrictions)
                .WithOne(udr => udr.DietaryRestriction)
                .HasForeignKey(udr => udr.DietaryRestrictionId)
                .OnDelete(DeleteBehavior.Restrict);

            // HealthCondition - UserHealthCondition relationship (One-to-Many)
            modelBuilder.Entity<HealthCondition>()
                .HasMany(hc => hc.UserHealthConditions)
                .WithOne(uhc => uhc.HealthCondition)
                .HasForeignKey(uhc => uhc.HealthConditionId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - UserFavoriteRestaurant relationship (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserFavoriteRestaurants)
                .WithOne(ufr => ufr.User)
                .HasForeignKey(ufr => ufr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for UserFavoriteRestaurant coordinates
            modelBuilder.Entity<UserFavoriteRestaurant>()
                .Property(ufr => ufr.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<UserFavoriteRestaurant>()
                .Property(ufr => ufr.Longitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<UserFavoriteRestaurant>()
                .Property(ufr => ufr.Rating)
                .HasPrecision(3, 2);

            // Community dietary tag votes
            modelBuilder.Entity<User>()
                .HasMany(u => u.CommunityDietaryTagVotes)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DietaryRestriction>()
                .HasMany(dr => dr.CommunityDietaryTagVotes)
                .WithOne(v => v.DietaryRestriction)
                .HasForeignKey(v => v.DietaryRestrictionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityRestaurant>()
                .HasMany(r => r.Votes)
                .WithOne(v => v.CommunityRestaurant)
                .HasForeignKey(v => v.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityDietaryTagVote>()
                .HasIndex(v => new { v.UserId, v.PlaceId, v.DietaryRestrictionId })
                .IsUnique();

            modelBuilder.Entity<CommunityDietaryTagVote>()
                .HasIndex(v => new { v.PlaceId, v.DietaryRestrictionId });

            modelBuilder.Entity<CommunityRestaurant>()
                .Property(r => r.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<CommunityRestaurant>()
                .Property(r => r.Longitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<CommunityRestaurant>()
                .Property(r => r.Rating)
                .HasPrecision(3, 2);

            // Restaurant Editing System relationships
            
            // User - RestaurantVersion relationships (CreatedBy, ApprovedBy)
            modelBuilder.Entity<RestaurantVersion>()
                .HasOne(rv => rv.CreatedByUser)
                .WithMany(u => u.CreatedRestaurantVersions)
                .HasForeignKey(rv => rv.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RestaurantVersion>()
                .HasOne(rv => rv.ApprovedByUser)
                .WithMany(u => u.ApprovedRestaurantVersions)
                .HasForeignKey(rv => rv.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - RestaurantOwner relationships (Owner, VerifiedBy)
            modelBuilder.Entity<RestaurantOwner>()
                .HasOne(ro => ro.User)
                .WithMany(u => u.OwnedRestaurants)
                .HasForeignKey(ro => ro.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RestaurantOwner>()
                .HasOne(ro => ro.VerifiedByUser)
                .WithMany(u => u.VerifiedRestaurantOwners)
                .HasForeignKey(ro => ro.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // RestaurantOwner - OwnerVerification relationship
            modelBuilder.Entity<OwnerVerification>()
                .HasOne(ov => ov.RestaurantOwner)
                .WithMany()
                .HasForeignKey(ov => ov.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - RestaurantEdit relationship
            modelBuilder.Entity<RestaurantEdit>()
                .HasOne(re => re.User)
                .WithMany(u => u.RestaurantEdits)
                .HasForeignKey(re => re.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // RestaurantVersion - RestaurantEdit relationship
            modelBuilder.Entity<RestaurantEdit>()
                .HasOne(re => re.RestaurantVersion)
                .WithMany()
                .HasForeignKey(re => re.VersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restaurant Health Q&A relationships
            modelBuilder.Entity<RestaurantQuestion>()
                .HasOne(q => q.User)
                .WithMany()
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RestaurantQuestion>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.RestaurantQuestion)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RestaurantAnswer>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RestaurantAnswer>()
                .HasOne(a => a.VerifiedByUser)
                .WithMany()
                .HasForeignKey(a => a.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            modelBuilder.Entity<RestaurantVersion>()
                .HasIndex(rv => new { rv.PlaceId, rv.IsCurrent });

            modelBuilder.Entity<RestaurantVersion>()
                .HasIndex(rv => rv.Status);

            modelBuilder.Entity<RestaurantQuestion>()
                .HasIndex(q => new { q.PlaceId, q.CreatedAt });

            modelBuilder.Entity<RestaurantAnswer>()
                .HasIndex(a => new { a.QuestionId, a.CreatedAt });

            modelBuilder.Entity<RestaurantOwner>()
                .HasIndex(ro => ro.PlaceId)
                .IsUnique();

            modelBuilder.Entity<RestaurantOwner>()
                .HasIndex(ro => ro.UserId);

            modelBuilder.Entity<RestaurantEdit>()
                .HasIndex(re => re.PlaceId);

            modelBuilder.Entity<RestaurantEdit>()
                .HasIndex(re => re.UserId);

            // UserRole relationships
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.GrantedByUser)
                .WithMany(u => u.GrantedRoles)
                .HasForeignKey(ur => ur.GrantedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.UserId);

            // Community Posts relationships
            modelBuilder.Entity<CommunityPost>()
                .HasOne(p => p.User)
                .WithMany(u => u.CommunityPosts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityPost>()
                .HasOne(p => p.DietaryRestriction)
                .WithMany(dr => dr.CommunityPosts)
                .HasForeignKey(p => p.DietaryRestrictionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityPost>()
                .HasMany(p => p.Comments)
                .WithOne(c => c.CommunityPost)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityPost>()
                .HasMany(p => p.Reactions)
                .WithOne(r => r.CommunityPost)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunityComment>()
                .HasOne(c => c.User)
                .WithMany(u => u.CommunityComments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommunityPostReaction>()
                .HasOne(r => r.User)
                .WithMany(u => u.CommunityPostReactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for Community Posts
            modelBuilder.Entity<CommunityPost>()
                .HasIndex(p => new { p.DietaryRestrictionId, p.CreatedAt })
                .HasFilter("[DietaryRestrictionId] IS NOT NULL");

            modelBuilder.Entity<CommunityPost>()
                .HasIndex(p => new { p.PlaceId, p.CreatedAt })
                .HasFilter("[PlaceId] IS NOT NULL");

            modelBuilder.Entity<CommunityPost>()
                .HasIndex(p => new { p.DietaryRestrictionId, p.PlaceId, p.CreatedAt })
                .HasFilter("[DietaryRestrictionId] IS NOT NULL AND [PlaceId] IS NOT NULL");

            modelBuilder.Entity<CommunityComment>()
                .HasIndex(c => new { c.PostId, c.CreatedAt });

            modelBuilder.Entity<CommunityPostReaction>()
                .HasIndex(r => new { r.PostId, r.UserId, r.ReactionType })
                .IsUnique();

            // REMOVED: UserProfile redundant relationships - these are handled through User entity
        }
    }
}
