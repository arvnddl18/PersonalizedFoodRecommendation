# Database Cleanup Analysis for Evidence-Based Algorithm

## Tables from Database Screenshot Analysis

### ✅ KEEP - Essential for Evidence-Based Algorithm
1. **Users** - Core user management
2. **UserProfiles** - Demographics (Age, Gender, ActivityLevel) for personalized recommendations
3. **FoodTypes** - Core food data with nutritional properties (IsHealthy, IsLowCalorie, etc.)
4. **FoodPreferences** - User's explicit food preferences (30% weight in algorithm)
5. **DietaryRestrictions** - Master list of dietary restrictions
6. **UserDietaryRestrictions** - User-specific dietary constraints (25% weight - highest priority)
7. **HealthConditions** - Master list of health conditions
8. **UserHealthConditions** - User-specific health conditions (10% weight)
9. **UserPreferencePatterns** - Behavioral patterns (15% weight)
10. **ChatSessions** - For chat functionality
11. **ChatMessages** - For chat functionality
12. **UserBehaviors** - For learning user interaction patterns

### ❌ REMOVE - Redundant/Unnecessary Tables
1. **RecommendationHistories** - Redundant (UserBehaviors captures this)
2. **__EFMigrationsHistory** - System table (keep for migrations)

### 🔍 SUSPICIOUS - Need Investigation
Based on the screenshot, there appear to be additional tables that aren't in our current DbContext:
- **External Tables** - May be legacy or auto-generated
- **System Tables** - SQL Server system tables
- **Graph Tables** - Not needed for our algorithm
- **File Tables** - Not relevant

## Cleanup Strategy

### Phase 1: Remove Redundant Models
1. Remove `RecommendationHistory` model and DbSet
2. Update User model to remove RecommendationHistory navigation
3. Remove related configurations

### Phase 2: Simplify Remaining Tables
1. Keep only essential fields in each table
2. Remove any unused navigation properties
3. Optimize relationships

### Phase 3: Database Migration
1. Create migration to drop unnecessary tables
2. Clean up any orphaned data
3. Rebuild indexes for performance

## Essential Algorithm Requirements Met

✅ **Dietary Restriction Weight (25%)**: UserDietaryRestrictions + DietaryRestrictions
✅ **User Preference Weight (30%)**: FoodPreferences + FoodTypes  
✅ **Nutritional Optimization (20%)**: FoodTypes nutritional properties
✅ **Health Condition Weight (10%)**: UserHealthConditions + HealthConditions
✅ **Behavioral Patterns (15%)**: UserPreferencePatterns + UserBehaviors
✅ **User Demographics**: UserProfiles (Age, Gender, ActivityLevel)
✅ **Chat Functionality**: ChatSessions + ChatMessages
