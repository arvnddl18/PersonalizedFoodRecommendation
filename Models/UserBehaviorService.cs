using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Capstone.Data;
using static Capstone.Models.NomsaurModel;
using System.Text.RegularExpressions;

namespace Capstone.Models
{
    public class UserBehaviorService
    {
        private readonly AppDbContext _context;

        public UserBehaviorService(AppDbContext context)
        {
            _context = context;
        }

        // Track user interactions
        public async Task TrackUserInteraction(int userId, string action, string context, string result, 
            string? intentDetected = null, string? parametersJson = null, int? satisfaction = null)
        {
            var behavior = new UserBehavior
            {
                UserId = userId,
                Action = action,
                Context = context ?? string.Empty,
                Result = result,
                Timestamp = DateTime.UtcNow,
                IntentDetected = intentDetected,
                ParametersJson = parametersJson ?? string.Empty,
                Satisfaction = satisfaction
            };

            _context.UserBehaviors.Add(behavior);
            await _context.SaveChangesAsync();

            // Trigger pattern analysis after tracking
            await AnalyzeAndUpdatePatterns(userId);
        }

        // Track recommendation using UserBehavior table (replaces RecommendationHistory)
        public async Task TrackRecommendation(int userId, string establishmentName, string cuisineType, 
            string priceRange, string reason, string? establishmentInfo = null)
        {
            var behavior = new UserBehavior
            {
                UserId = userId,
                Action = "recommendation",
                EstablishmentName = establishmentName,
                CuisineType = cuisineType,
                ReasonForRecommendation = reason,
                EstablishmentInfo = establishmentInfo,
                Context = $"PriceRange: {priceRange}",
                Timestamp = DateTime.UtcNow
            };

            _context.UserBehaviors.Add(behavior);
            await _context.SaveChangesAsync();
        }

        // Mark recommendation as selected using UserBehavior table
        public async Task MarkRecommendationSelected(int userId, string establishmentName, int? rating = null, string? feedback = null)
        {
            var behavior = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.EstablishmentName == establishmentName && ub.Action == "recommendation")
                .OrderByDescending(ub => ub.Timestamp)
                .FirstOrDefaultAsync();

            if (behavior != null)
            {
                behavior.SelectedAt = DateTime.UtcNow;
                behavior.Satisfaction = rating;
                behavior.Result = feedback ?? "selected";

                // UPDATE PATTERNS WITH INTERACTION SUCCESS DATA
                if (rating.HasValue)
                {
                    await UpdatePatternInteractions(userId, behavior, rating.Value >= 4);
                }

                // Also update user's UserFoodType.LastSelected if we can match cuisine -> FoodType
                if (!string.IsNullOrWhiteSpace(behavior.CuisineType))
                {
                    var matchedFoodType = await _context.FoodTypes
                        .FirstOrDefaultAsync(ft => ft.CuisineType.ToLower() == behavior.CuisineType.ToLower());
                    if (matchedFoodType != null)
                    {
                        var uft = await _context.UserFoodTypes
                            .FirstOrDefaultAsync(x => x.UserId == userId && x.FoodTypeId == matchedFoodType.FoodTypeId);
                        if (uft != null)
                        {
                            uft.LastSelected = DateTime.UtcNow;
                        }
                        else
                        {
                            // Create a preference if it doesn't exist yet
                            _context.UserFoodTypes.Add(new UserFoodType
                            {
                                UserId = userId,
                                FoodTypeId = matchedFoodType.FoodTypeId,
                                PreferenceLevel = "Preferred",
                                PreferenceScore = 6, // Medium score for learned preference
                                AddedAt = DateTime.UtcNow,
                                LastSelected = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        // Analyze user patterns and update preference patterns
        private async Task AnalyzeAndUpdatePatterns(int userId)
        {
            // Analyze time patterns
            await AnalyzeTimePatterns(userId);
            
            // Analyze location patterns
            await AnalyzeLocationPatterns(userId);
            
            // Analyze cuisine preferences
            await AnalyzeCuisinePatterns(userId);
            
            // Analyze price patterns
            await AnalyzePricePatterns(userId);
        }

        private async Task AnalyzeTimePatterns(int userId)
        {
            var recentBehaviors = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.Action == "search")
                .OrderByDescending(ub => ub.Timestamp)
                .Take(50)
                .ToListAsync();

            if (recentBehaviors.Count < 5) return;

            var timeGroups = recentBehaviors
                .GroupBy(ub => ub.Timestamp.Hour)
                .OrderByDescending(g => g.Count())
                .Take(3);

            foreach (var group in timeGroups)
            {
                var confidence = Math.Min((decimal)group.Count() / recentBehaviors.Count, 1.0m);
                var timeRange = GetTimeRange(group.Key);

                // Calculate success rate for this time pattern
                var successfulInteractions = group.Count(b => b.Satisfaction.HasValue && b.Satisfaction >= 4);
                var isSuccessful = successfulInteractions > (group.Count() * 0.6); // 60% success threshold

                await UpdatePattern(userId, "time", timeRange, confidence, isSuccessful);
            }
        }

        private async Task AnalyzeLocationPatterns(int userId)
        {
            var recentBehaviors = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.Context != null && ub.Context.Contains("location"))
                .OrderByDescending(ub => ub.Timestamp)
                .Take(30)
                .ToListAsync();

            if (recentBehaviors.Count < 3) return;

            // Extract location preferences from context
            var locationPreferences = recentBehaviors
                .Where(ub => ub.Context != null && (ub.Context.Contains("downtown") || ub.Context.Contains("nearby") || 
                           ub.Context.Contains("mall") || ub.Context.Contains("university")))
                .GroupBy(ub => ExtractLocationFromContext(ub.Context!))
                .OrderByDescending(g => g.Count())
                .Take(2);

            foreach (var group in locationPreferences)
            {
                var confidence = Math.Min((decimal)group.Count() / recentBehaviors.Count, 1.0m);
                
                // Calculate success rate for this location pattern
                var successfulInteractions = group.Count(b => b.Satisfaction.HasValue && b.Satisfaction >= 4);
                var isSuccessful = successfulInteractions > (group.Count() * 0.6); // 60% success threshold
                
                await UpdatePattern(userId, "location", group.Key ?? "general", confidence, isSuccessful);
            }
        }

        private async Task AnalyzeCuisinePatterns(int userId)
        {
            var recentBehaviors = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.Action == "search")
                .OrderByDescending(ub => ub.Timestamp)
                .Take(40)
                .ToListAsync();

            if (recentBehaviors.Count < 5) return;

            // Extract cuisine preferences from parameters
            var cuisinePreferences = new Dictionary<string, int>();
            
            foreach (var behavior in recentBehaviors)
            {
                if (!string.IsNullOrEmpty(behavior.ParametersJson))
                {
                    try
                    {
                        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(behavior.ParametersJson ?? string.Empty) ?? new Dictionary<string, object>();
                        if (parameters.ContainsKey("food"))
                        {
                            var food = parameters["food"]?.ToString()?.ToLower() ?? string.Empty;
                            var cuisine = MapFoodToCuisine(food);
                            if (!string.IsNullOrEmpty(cuisine))
                            {
                                cuisinePreferences[cuisine] = cuisinePreferences.GetValueOrDefault(cuisine, 0) + 1;
                            }
                            // NEW: per-food preference score boosting
                            var matchedFoodType = await _context.FoodTypes.FirstOrDefaultAsync(ft => ft.Name.ToLower() == food);
                            if (matchedFoodType != null)
                            {
                                var uft = await _context.UserFoodTypes.FirstOrDefaultAsync(x => x.UserId == userId && x.FoodTypeId == matchedFoodType.FoodTypeId);
                                if (uft == null)
                                {
                                    _context.UserFoodTypes.Add(new UserFoodType
                                    {
                                        UserId = userId,
                                        FoodTypeId = matchedFoodType.FoodTypeId,
                                        PreferenceLevel = "Preferred",
                                        PreferenceScore = 6,
                                        AddedAt = DateTime.UtcNow,
                                        LastSelected = DateTime.UtcNow
                                    });
                                }
                                else
                                {
                                    uft.PreferenceScore = Math.Min(10, uft.PreferenceScore + 1);
                                    uft.LastSelected = DateTime.UtcNow;
                                }
                            }
                        }
                    }
                    catch { /* Ignore parsing errors */ }
                }
            }

            foreach (var cuisine in cuisinePreferences.OrderByDescending(kvp => kvp.Value).Take(3))
            {
                var confidence = Math.Min((decimal)cuisine.Value / recentBehaviors.Count, 1.0m);
                
                // Calculate success rate for this cuisine pattern
                var cuisineBehaviors = recentBehaviors.Where(b => 
                    b.ParametersJson != null && b.ParametersJson.Contains(cuisine.Key ?? "", StringComparison.OrdinalIgnoreCase));
                var successfulInteractions = cuisineBehaviors.Count(b => b.Satisfaction.HasValue && b.Satisfaction >= 4);
                var isSuccessful = cuisineBehaviors.Any() && successfulInteractions > (cuisineBehaviors.Count() * 0.6);
                
                await UpdatePattern(userId, "cuisine", cuisine.Key ?? "general", confidence, isSuccessful);
            }
        }

        private async Task AnalyzePricePatterns(int userId)
        {
            var recentBehaviors = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.Action == "search")
                .OrderByDescending(ub => ub.Timestamp)
                .Take(30)
                .ToListAsync();

            if (recentBehaviors.Count < 3) return;

            var pricePreferences = recentBehaviors
                .Where(ub => !string.IsNullOrEmpty(ub.ParametersJson))
                .Select(ub => ExtractPriceFromParameters(ub.ParametersJson!))
                .Where(price => !string.IsNullOrEmpty(price))
                .GroupBy(price => price)
                .OrderByDescending(g => g.Count())
                .Take(2);

            foreach (var group in pricePreferences)
            {
                var confidence = Math.Min((decimal)group.Count() / recentBehaviors.Count, 1.0m);
                
                // Calculate success rate for this price pattern
                var priceBehaviors = group.AsEnumerable();
                var successfulInteractions = recentBehaviors
                    .Where(b => ExtractPriceFromParameters(b.ParametersJson ?? "") == group.Key)
                    .Count(b => b.Satisfaction.HasValue && b.Satisfaction >= 4);
                var isSuccessful = successfulInteractions > (group.Count() * 0.6); // 60% success threshold
                
                await UpdatePattern(userId, "price", group.Key!, confidence, isSuccessful);
            }
        }

        // ENHANCED: UpdatePattern with TotalInteractions and SuccessfulInteractions tracking
        private async Task UpdatePattern(int userId, string patternType, string patternValue, decimal confidence, bool isSuccessful = false)
        {
            var existingPattern = await _context.UserPreferencePatterns
                .FirstOrDefaultAsync(upp => upp.UserId == userId && 
                                          upp.PatternType == patternType && 
                                          upp.PatternValue == patternValue);

            if (existingPattern != null)
            {
                // Update existing pattern with enhanced interaction tracking
                var totalObservations = existingPattern.ObservationCount + 1;
                var newConfidence = (existingPattern.Confidence * existingPattern.ObservationCount + confidence) / totalObservations;
                
                existingPattern.Confidence = Math.Min(newConfidence, 1.0m);
                existingPattern.LastObserved = DateTime.UtcNow;
                existingPattern.ObservationCount = totalObservations;
                
                // EVIDENCE-BASED INTERACTION METRICS
                existingPattern.TotalInteractions += 1;
                if (isSuccessful)
                {
                    existingPattern.SuccessfulInteractions += 1;
                }
                
                // UPDATE CONFIDENCE BASED ON SUCCESS RATE (Enhanced Formula)
                if (existingPattern.TotalInteractions > 0)
                {
                    var successRate = (decimal)existingPattern.SuccessfulInteractions / existingPattern.TotalInteractions;
                    var baseConfidence = Math.Min((decimal)existingPattern.ObservationCount / 50, 1.0m); // Cap at 50 observations
                    
                    // DECAY: If pattern not observed recently, decay confidence (soft decay)
                    var days = (DateTime.UtcNow - existingPattern.LastObserved).TotalDays;
                    var decay = (decimal)Math.Min(0.3, days / 30.0); // up to -0.3 over 30+ days
                    baseConfidence = Math.Max(0.0m, baseConfidence - decay);
                    
                    // Blend pattern frequency with success rate (70% frequency, 30% success rate)
                    existingPattern.Confidence = Math.Min(1.0m, (baseConfidence * 0.7m) + (successRate * 0.3m));
                }
            }
            else
            {
                // Create new pattern with initial interaction metrics
                var newPattern = new UserPreferencePattern
                {
                    UserId = userId,
                    PatternType = patternType,
                    PatternValue = patternValue,
                    Confidence = confidence,
                    LastObserved = DateTime.UtcNow,
                    ObservationCount = 1,
                    TotalInteractions = 1,
                    SuccessfulInteractions = isSuccessful ? 1 : 0
                };

                _context.UserPreferencePatterns.Add(newPattern);
            }

            await _context.SaveChangesAsync();
        }
        
        // NEW: Update pattern interactions based on user feedback
        private async Task UpdatePatternInteractions(int userId, UserBehavior behavior, bool isSuccessful)
        {
            // Update cuisine pattern interactions
            if (!string.IsNullOrEmpty(behavior.CuisineType))
            {
                await UpdatePatternInteractionMetrics(userId, "cuisine", behavior.CuisineType, isSuccessful);
            }
            
            // Update time pattern interactions
            var timeRange = GetTimeRange(behavior.Timestamp.Hour);
            await UpdatePatternInteractionMetrics(userId, "time", timeRange, isSuccessful);
            
            // Update location pattern interactions (if available in context)
            if (!string.IsNullOrEmpty(behavior.Context))
            {
                var location = ExtractLocationFromContext(behavior.Context);
                if (!string.IsNullOrEmpty(location))
                {
                    await UpdatePatternInteractionMetrics(userId, "location", location, isSuccessful);
                }
            }
            
            // Update price pattern interactions (if available in context)
            if (!string.IsNullOrEmpty(behavior.Context) && behavior.Context.Contains("PriceRange"))
            {
                var priceMatch = System.Text.RegularExpressions.Regex.Match(behavior.Context, @"PriceRange:\s*(\w+)");
                if (priceMatch.Success)
                {
                    await UpdatePatternInteractionMetrics(userId, "price", priceMatch.Groups[1].Value.ToLower(), isSuccessful);
                }
            }
        }
        
        // Helper method to update interaction metrics for existing patterns
        private async Task UpdatePatternInteractionMetrics(int userId, string patternType, string patternValue, bool isSuccessful)
        {
            var pattern = await _context.UserPreferencePatterns
                .FirstOrDefaultAsync(upp => upp.UserId == userId && 
                                          upp.PatternType == patternType && 
                                          upp.PatternValue.Equals(patternValue, StringComparison.OrdinalIgnoreCase));
            
            if (pattern != null)
            {
                // Update interaction metrics
                pattern.TotalInteractions += 1;
                if (isSuccessful)
                {
                    pattern.SuccessfulInteractions += 1;
                }
                
                // Recalculate confidence based on success rate
                if (pattern.TotalInteractions > 0)
                {
                    var successRate = (decimal)pattern.SuccessfulInteractions / pattern.TotalInteractions;
                    var baseConfidence = Math.Min((decimal)pattern.ObservationCount / 50, 1.0m);
                    
                    // Enhanced confidence formula: 70% frequency + 30% success rate
                    pattern.Confidence = Math.Min(1.0m, (baseConfidence * 0.7m) + (successRate * 0.3m));
                }
                
                pattern.LastObserved = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        
        // MIGRATION HELPER: Populate TotalInteractions and SuccessfulInteractions for existing patterns
        public async Task PopulateInteractionMetricsForExistingPatterns(int userId)
        {
            var patterns = await _context.UserPreferencePatterns
                .Where(upp => upp.UserId == userId)
                .ToListAsync();
                
            foreach (var pattern in patterns)
            {
                // Get relevant behaviors for this pattern
                var relevantBehaviors = await GetRelevantBehaviorsForPattern(userId, pattern);
                
                // Calculate interaction metrics
                var totalInteractions = relevantBehaviors.Count;
                var successfulInteractions = relevantBehaviors.Count(b => b.Satisfaction.HasValue && b.Satisfaction >= 4);
                
                // Update pattern with calculated metrics
                pattern.TotalInteractions = totalInteractions;
                pattern.SuccessfulInteractions = successfulInteractions;
                
                // Recalculate confidence with success rate
                if (totalInteractions > 0)
                {
                    var successRate = (decimal)successfulInteractions / totalInteractions;
                    var baseConfidence = Math.Min((decimal)pattern.ObservationCount / 50, 1.0m);
                    
                    // Enhanced confidence: 70% frequency + 30% success rate
                    pattern.Confidence = Math.Min(1.0m, (baseConfidence * 0.7m) + (successRate * 0.3m));
                }
            }
            
            await _context.SaveChangesAsync();
        }
        
        // Helper method to get behaviors relevant to a specific pattern
        private async Task<List<UserBehavior>> GetRelevantBehaviorsForPattern(int userId, UserPreferencePattern pattern)
        {
            var allBehaviors = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.Satisfaction.HasValue)
                .ToListAsync();
            
            return pattern.PatternType.ToLower() switch
            {
                "time" => allBehaviors.Where(b => GetTimeRange(b.Timestamp.Hour) == pattern.PatternValue).ToList(),
                "cuisine" => allBehaviors.Where(b => !string.IsNullOrEmpty(b.CuisineType) && 
                    b.CuisineType.Equals(pattern.PatternValue, StringComparison.OrdinalIgnoreCase)).ToList(),
                "location" => allBehaviors.Where(b => !string.IsNullOrEmpty(b.Context) && 
                    ExtractLocationFromContext(b.Context) == pattern.PatternValue).ToList(),
                "price" => allBehaviors.Where(b => !string.IsNullOrEmpty(b.Context) && 
                    b.Context.Contains("PriceRange") && b.Context.Contains(pattern.PatternValue, StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => new List<UserBehavior>()
            };
        }

        // DEPRECATED: Use GenerateFoodPreferenceRankings() instead
        [Obsolete("This method returns generic recommendations. Use GenerateFoodPreferenceRankings() for the new food preference ranking algorithm.")]
        public async Task<Dictionary<string, object>> GeneratePersonalizedRecommendations(int userId, string query, 
            decimal? latitude = null, decimal? longitude = null)
        {
            var userPatterns = await _context.UserPreferencePatterns
                .Where(upp => upp.UserId == userId && upp.Confidence >= 0.3m)
                .OrderByDescending(upp => upp.Confidence)
                .ToListAsync();

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == userId);

            var recommendations = new Dictionary<string, object>();

            // Add user dietary restrictions (replacing old single DietaryPreference)
            var userDietaryRestrictions = await _context.UserDietaryRestrictions
                .Include(udr => udr.DietaryRestriction)
                .Where(udr => udr.UserId == userId)
                .OrderByDescending(udr => udr.ImportanceLevel)
                .ToListAsync();
            
            if (userDietaryRestrictions.Any())
            {
                recommendations["dietaryRestrictions"] = userDietaryRestrictions
                    .Select(udr => udr.DietaryRestriction.Name)
                    .ToList();
            }

            // Add learned patterns
            var cuisinePattern = userPatterns
                .Where(p => p.PatternType == "cuisine")
                .OrderByDescending(p => p.Confidence)
                .FirstOrDefault();
            if (cuisinePattern != null)
            {
                recommendations["preferredCuisine"] = cuisinePattern.PatternValue;
                recommendations["cuisineConfidence"] = cuisinePattern.Confidence;
            }

            var pricePattern = userPatterns.FirstOrDefault(p => p.PatternType == "price");
            if (pricePattern != null)
            {
                recommendations["preferredPriceRange"] = pricePattern.PatternValue;
                recommendations["priceConfidence"] = pricePattern.Confidence;
            }

            var timePattern = userPatterns
                .Where(p => p.PatternType == "time")
                .OrderByDescending(p => p.Confidence)
                .FirstOrDefault();
            if (timePattern != null)
            {
                recommendations["preferredTime"] = timePattern.PatternValue;
                recommendations["timeConfidence"] = timePattern.Confidence;
            }

            var locationPattern = userPatterns
                .Where(p => p.PatternType == "location")
                .OrderByDescending(p => p.Confidence)
                .FirstOrDefault();
            if (locationPattern != null)
            {
                recommendations["preferredLocation"] = locationPattern.PatternValue;
                recommendations["locationConfidence"] = locationPattern.Confidence;
            }

            // Add location info if provided
            if (latitude.HasValue && longitude.HasValue)
            {
                recommendations["latitude"] = latitude.Value;
                recommendations["longitude"] = longitude.Value;
            }

            return recommendations;
        }

        // Prescriptive recommendation result DTO
        public class PrescriptiveRecommendationResult
        {
            public string CandidateName { get; set; } = string.Empty;
            public decimal Score { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        // NEW: Generate food preference rankings instead of establishment rankings
        public async Task<List<FoodPreferenceScore>> GenerateFoodPreferenceRankings(
            int userId,
            string query,
            decimal? latitude = null,
            decimal? longitude = null,
            int topN = 10,
            bool includeHealthDietRestrictions = true,
            string? requestedMealType = null)
        {
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId);

            var userPatterns = await _context.UserPreferencePatterns
                .Where(upp => upp.UserId == userId)
                .OrderByDescending(p => p.LastObserved)
                .ThenByDescending(p => p.Confidence)
                .ToListAsync();

            // Get user food preferences (UserFoodTypes) - Used for both User Preferences and Behavioral Pattern scores
            var userFoodTypes = await _context.UserFoodTypes
                .Where(uft => uft.UserId == userId)
                .ToListAsync();
            var preferredFoodTypeIds = userFoodTypes
                .Where(uft => uft.PreferenceLevel == "Preferred" || uft.PreferenceScore >= 7)
                .Select(uft => uft.FoodTypeId)
                .ToList();

            var foodTypes = await _context.FoodTypes.ToListAsync();
            // REMOVED: Establishments table - using Google Maps API instead
            // All establishment data now comes from Google Maps, not local database

            // Parse query context
            var queryLower = (query ?? string.Empty).ToLowerInvariant();
            
            // Apply meal type filtering if requested
            if (!string.IsNullOrEmpty(requestedMealType))
            {
                var mealType = requestedMealType.ToLowerInvariant();
                foodTypes = mealType switch
                {
                    "breakfast" => foodTypes.Where(ft => ft.IsBreakfast).ToList(),
                    "lunch" => foodTypes.Where(ft => ft.IsLunch).ToList(),
                    "dinner" => foodTypes.Where(ft => ft.IsDinner).ToList(),
                    "snacks" or "snack" => foodTypes.Where(ft => ft.IsSnacks).ToList(),
                    "dessert" => foodTypes.Where(ft => ft.IsDessert).ToList(),
                    "beverage" or "beverages" or "drink" or "drinks" => foodTypes.Where(ft => ft.IsBeverage).ToList(),
                    _ => foodTypes
                };
            }
            
            // Also detect meal type from query if not explicitly provided
            if (string.IsNullOrEmpty(requestedMealType))
            {
                var detectedMealType = DetectMealTypeFromQuery(queryLower);
                if (!string.IsNullOrEmpty(detectedMealType))
                {
                    foodTypes = detectedMealType switch
                    {
                        "breakfast" => foodTypes.Where(ft => ft.IsBreakfast).ToList(),
                        "lunch" => foodTypes.Where(ft => ft.IsLunch).ToList(),
                        "dinner" => foodTypes.Where(ft => ft.IsDinner).ToList(),
                        "snacks" => foodTypes.Where(ft => ft.IsSnacks).ToList(),
                        "dessert" => foodTypes.Where(ft => ft.IsDessert).ToList(),
                        "beverage" => foodTypes.Where(ft => ft.IsBeverage).ToList(),
                        _ => foodTypes
                    };
                }
            }
            bool wantsHealthy = Regex.IsMatch(queryLower, "\\b(healthy|low\\s*cal|low\\s*fat|low\\s*carb|diet)\\b");
            
            // Get user's dietary restrictions and health conditions from User entity (only if enabled)
            var dietaryRestrictions = includeHealthDietRestrictions ? 
                await _context.UserDietaryRestrictions
                .Include(udr => udr.DietaryRestriction)
                .Where(udr => udr.UserId == userId)
                    .ToListAsync() : 
                new List<UserDietaryRestriction>();
                
            var healthConditions = includeHealthDietRestrictions ? 
                await _context.UserHealthConditions
                .Include(uhc => uhc.HealthCondition)
                .Where(uhc => uhc.UserId == userId)
                    .ToListAsync() : 
                new List<UserHealthCondition>();

            // Create food preference scores
            var foodScores = new Dictionary<string, FoodPreferenceScore>();

            // Score each food type based on comprehensive criteria
            foreach (var foodType in foodTypes)
            {
                // Extract current location context from query or user profile
                string? currentLocation = ExtractCurrentLocationFromQuery(queryLower) ?? userProfile?.PreferredLocation;
                
                var score = CalculateFoodPreferenceScore(
                    foodType, 
                    userFoodTypes,
                    preferredFoodTypeIds, 
                    userPatterns, 
                    dietaryRestrictions, 
                    healthConditions, 
                    wantsHealthy,
                    queryLower,
                    null, // cuisineType will be mapped internally
                    currentLocation,
                    includeHealthDietRestrictions);

                if (score.TotalScore > 0.3m) // Only include foods with reasonable scores
                {
                    foodScores[foodType.Name] = score;
                }
            }

            return foodScores.Values
                .OrderByDescending(fs => fs.TotalScore)
                .Take(topN)
                .ToList();
        }

        private FoodPreferenceScore CalculateFoodPreferenceScore(
            FoodType foodType,
            List<UserFoodType> userFoodTypes,
            List<int> preferredFoodTypeIds,
            List<UserPreferencePattern> userPatterns,
            List<UserDietaryRestriction> dietaryRestrictions,
            List<UserHealthCondition> healthConditions,
            bool wantsHealthy,
            string queryLower,
            string? cuisineType = null,
            string? currentLocation = null,
            bool includeHealthDietRestrictions = true)
        {
            // Map the food name to its cuisine type
            var mappedCuisine = MapFoodToCuisine(foodType.Name);
            
            var score = new FoodPreferenceScore
            {
                FoodName = foodType.Name,
                CuisineType = cuisineType ?? mappedCuisine
            };

            decimal totalWeight = 0m;

            // EVIDENCE-BASED WEIGHT ALLOCATION (Based on Academic Research)
            // Research Sources: Nutrition Informatics, Health-Aware Recommendation Systems
            
            // Dynamic weight allocation based on health/diet toggle
            decimal restrictionMultiplier, preferenceMultiplier, nutritionalMultiplier, healthMultiplier, behavioralMultiplier;
            
            if (includeHealthDietRestrictions)
            {
                // Standard weights (health/diet mode ON)
                restrictionMultiplier = 0.25m;  // 25% - Dietary restrictions
                preferenceMultiplier = 0.30m;   // 30% - User preferences  
                nutritionalMultiplier = 0.20m;  // 20% - Nutritional optimization
                healthMultiplier = 0.10m;       // 10% - Health conditions
                behavioralMultiplier = 0.15m;   // 15% - Behavioral patterns
            }
            else
            {
                // Redistributed weights (general mode - health/diet mode OFF)
                // Redistribute the 35% from dietary restrictions (25%) and health conditions (10%)
                restrictionMultiplier = 0.0m;   // 0% - No dietary restrictions
                preferenceMultiplier = 0.50m;   // 50% - Increased user preferences (+20%)
                nutritionalMultiplier = 0.25m;  // 25% - Increased nutritional (+5%)
                healthMultiplier = 0.0m;        // 0% - No health conditions
                behavioralMultiplier = 0.25m;   // 25% - Increased behavioral patterns (+10%)
            }
            
            // 1. DIETARY RESTRICTIONS - Only if enabled
            decimal restrictionWeight = 0.0m;
            if (includeHealthDietRestrictions)
            {
                restrictionWeight = CalculateDietaryRestrictionCompliance(foodType, dietaryRestrictions);
            
            // Populate matching restrictions for reasoning
            foreach (var restriction in dietaryRestrictions)
            {
                if (GetDietaryCompliance(foodType, restriction.DietaryRestriction.Name) > 0.5m)
                {
                    score.MatchingRestrictions.Add(restriction.DietaryRestriction.Name);
                }
            }
            }
            totalWeight += restrictionWeight * restrictionMultiplier;

            // 2. USER PREFERENCES - Uses UserFoodTypes (onboarding data) for User Preferences score
            decimal preferenceWeight;
            var userFoodType = userFoodTypes.FirstOrDefault(uft => uft.FoodTypeId == foodType.FoodTypeId);
            if (userFoodType != null)
            {
                // map 1-10 -> 0.3-0.95 for smoother gradation
                var mapped = 0.25m + (userFoodType.PreferenceScore / 10.0m) * 0.7m; // 0.25..0.95
                preferenceWeight = Math.Min(0.95m, Math.Max(0.3m, mapped));
            }
            else
            {
                preferenceWeight = preferredFoodTypeIds.Contains(foodType.FoodTypeId) ? 0.9m : 0.4m;
            }
            totalWeight += preferenceWeight * preferenceMultiplier;

            // 3. NUTRITIONAL OPTIMIZATION - Slightly higher weight when health/diet mode is off
            decimal nutritionalWeight = CalculateNutritionalOptimization(foodType, healthConditions, wantsHealthy);
            totalWeight += nutritionalWeight * nutritionalMultiplier;

            // 4. HEALTH CONDITIONS - Only if enabled
            decimal healthWeight = 0.0m;
            if (includeHealthDietRestrictions)
            {
                healthWeight = CalculateHealthConditionAlignment(foodType, healthConditions);
            
            // Populate matching health conditions for reasoning
            foreach (var condition in healthConditions)
            {
                if (GetHealthAlignment(foodType, condition.HealthCondition.Name) > 0.5m)
                {
                    score.MatchingHealthConditions.Add(condition.HealthCondition.Name);
                }
            }
            }
            totalWeight += healthWeight * healthMultiplier;

            // 5. BEHAVIORAL PATTERNS - Higher weight when health/diet mode is off
            decimal behavioralWeight = CalculateBehavioralPatternWeight(foodType, userPatterns, queryLower, cuisineType, currentLocation, userFoodTypes);
            totalWeight += behavioralWeight * behavioralMultiplier;

            score.TotalScore = Math.Round(totalWeight, 2);
            
            // Set safety flag based on dietary restrictions compliance (or always safe if health/diet mode is off)
            score.IsSafeForUser = includeHealthDietRestrictions ? (restrictionWeight >= 0.8m) : true;
            
            // Build comprehensive reason
            var reasonParts = new List<string>();
            
            if (preferredFoodTypeIds.Contains(foodType.FoodTypeId))
                reasonParts.Add("matches your saved preferences");
            
            if (includeHealthDietRestrictions && score.MatchingRestrictions.Any())
                reasonParts.Add($"complies with your {string.Join(", ", score.MatchingRestrictions)} restrictions");
            
            if (includeHealthDietRestrictions && score.MatchingHealthConditions.Any())
                reasonParts.Add($"supports your {string.Join(", ", score.MatchingHealthConditions)} health needs");
            
            if (wantsHealthy)
                reasonParts.Add("aligns with healthy eating goals");
                
            if (!includeHealthDietRestrictions)
                reasonParts.Add("based on general preferences");

            score.Reason = reasonParts.Any() ? string.Join(", ", reasonParts) : "neutral recommendation";

            return score;
        }

        // DEPRECATED: Use GenerateFoodPreferenceRankings() instead
        [Obsolete("This method ranks establishments instead of food preferences. Use GenerateFoodPreferenceRankings() for the new algorithm.")]
        public async Task<List<PrescriptiveRecommendationResult>> GeneratePrescriptiveRecommendations(
            int userId,
            string query,
            decimal? latitude = null,
            decimal? longitude = null,
            int topN = 5)
        {
            // DEPRECATED: Establishments table removed, returning empty list
            return new List<PrescriptiveRecommendationResult>();
        }

        private string GetTimeRange(int hour)
        {
            if (hour >= 6 && hour < 11) return "morning";
            if (hour >= 11 && hour < 15) return "afternoon";
            if (hour >= 15 && hour < 21) return "evening";
            return "late-night";
        }

        private string? ExtractCuisineFromParameters(string parametersJson)
        {
            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);
                if (parameters != null && parameters.ContainsKey("cuisine"))
                {
                    return parameters["cuisine"]?.ToString();
                }
            }
            catch { /* Ignore parsing errors */ }
            
            return null;
        }

        private string? ExtractPriceFromParameters(string parametersJson)
        {
            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);
                if (parameters != null && parameters.ContainsKey("price"))
                {
                    return parameters["price"]?.ToString();
                }
            }
            catch { /* Ignore parsing errors */ }
            
            return null;
        }

        public async Task<Dictionary<string, object>> GetUserInsights(int userId)
        {
            var behaviors = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId)
                .OrderByDescending(ub => ub.Timestamp)
                .Take(100)
                .ToListAsync();

            var patterns = await _context.UserPreferencePatterns
                .Where(upp => upp.UserId == userId)
                .OrderByDescending(upp => upp.Confidence)
                .ToListAsync();

            var recommendations = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.Action == "recommendation")
                .OrderByDescending(ub => ub.Timestamp)
                .Take(50)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["totalInteractions"] = behaviors.Count,
                ["averageSatisfaction"] = behaviors.Where(b => b.Satisfaction.HasValue && b.Satisfaction.Value > 0).DefaultIfEmpty().Average(b => (double?)(b?.Satisfaction ?? 0)) ?? 0,
                ["topPatterns"] = patterns.Take(5).ToList(),
                ["recommendationAcceptanceRate"] = recommendations.Count == 0 ? 0 : recommendations.Count(r => r.SelectedAt.HasValue) / (double)recommendations.Count,
                ["mostActiveTime"] = GetMostActiveTime(behaviors),
                ["preferredCuisines"] = patterns.Where(p => p.PatternType == "cuisine").Take(3).ToList()
            };
        }

        // NEW: Detect meal type from user query
        private string? DetectMealTypeFromQuery(string queryLower)
        {
            // Breakfast keywords
            if (Regex.IsMatch(queryLower, @"\b(breakfast|morning meal|am|early|first meal|morning food)\b"))
                return "breakfast";
            
            // Lunch keywords
            if (Regex.IsMatch(queryLower, @"\b(lunch|midday|noon|afternoon meal|lunchtime)\b"))
                return "lunch";
            
            // Dinner keywords
            if (Regex.IsMatch(queryLower, @"\b(dinner|evening meal|pm|night|supper|dinnertime)\b"))
                return "dinner";
            
            // Snacks keywords
            if (Regex.IsMatch(queryLower, @"\b(snack|snacks|light bite|quick bite|meryenda|merienda|appetizer)\b"))
                return "snacks";
            
            // Dessert keywords
            if (Regex.IsMatch(queryLower, @"\b(dessert|sweet|ice cream|cake|pastry|after meal)\b"))
                return "dessert";
            
            // Beverage keywords
            if (Regex.IsMatch(queryLower, @"\b(drink|beverage|juice|coffee|tea|soda|water|alcohol)\b"))
                return "beverage";
            
            return null;
        }

        private string GetMostActiveTime(List<UserBehavior> behaviors)
        {
            var timeGroups = behaviors
                .GroupBy(b => b.Timestamp.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return timeGroups != null ? GetTimeRange(timeGroups.Key) : "unknown";
        }

        // Missing helper methods that were removed during cleanup
        private string ExtractLocationFromContext(string context)
        {
            // Extract location information from context string
            try
            {
                if (string.IsNullOrEmpty(context)) return string.Empty;
                
                // Simple location extraction - look for location keywords
                var locationKeywords = new[] { "davao", "manila", "cebu", "downtown", "mall", "university" };
                foreach (var keyword in locationKeywords)
                {
                    if (context.ToLowerInvariant().Contains(keyword))
                        return keyword;
                }
            }
            catch { /* Ignore parsing errors */ }
            
            return string.Empty;
        }

        private string MapFoodToCuisine(string food)
        {
            if (string.IsNullOrEmpty(food)) return string.Empty;
            
            var foodLower = food.ToLowerInvariant();
            
            // Comprehensive mapping of specific foods to cuisines
            // Italian Foods
            if (foodLower.Contains("pizza") || foodLower.Contains("pasta") || 
                foodLower.Contains("lasagna") || foodLower.Contains("risotto")) return "Italian";
            
            // Japanese Foods
            if (foodLower.Contains("sushi") || foodLower.Contains("ramen") || 
                foodLower.Contains("tempura") || foodLower.Contains("teriyaki")) return "Japanese";
            
            // American Foods
            if (foodLower.Contains("burger") || foodLower.Contains("bbq") || 
                foodLower.Contains("mac and cheese") || foodLower.Contains("fried chicken")) return "American";
            
            // Mexican Foods
            if (foodLower.Contains("taco") || foodLower.Contains("burrito") || 
                foodLower.Contains("quesadilla") || foodLower.Contains("nachos")) return "Mexican";
            
            // Indian Foods
            if (foodLower.Contains("curry") || foodLower.Contains("biryani") || 
                foodLower.Contains("tandoori") || foodLower.Contains("naan") || 
                foodLower.Contains("tikka masala")) return "Indian";
            
            // Chinese Foods
            if (foodLower.Contains("fried rice") || foodLower.Contains("sweet and sour") || 
                foodLower.Contains("dumplings") || foodLower.Contains("chow mein")) return "Chinese";
            
            // Thai Foods
            if (foodLower.Contains("pad thai")) return "Thai";
            
            // Vietnamese Foods
            if (foodLower.Contains("pho")) return "Vietnamese";
            
            return "General";
        }

                private decimal CalculateDietaryRestrictionCompliance(FoodType foodType, List<UserDietaryRestriction> dietaryRestrictions)
        {
            if (!dietaryRestrictions.Any()) return 1.0m; // No restrictions = full compliance
            
            decimal totalCompliance = 0m;
            decimal totalWeight = 0m;
            
            foreach (var restriction in dietaryRestrictions)
            {
                // Use DATABASE FIELDS for precise dietary compliance checking
                decimal compliance = GetDietaryComplianceFromDatabase(foodType, restriction.DietaryRestriction.Name);
                decimal weight = restriction.ImportanceLevel / 10.0m; // Convert 1-10 to 0.1-1.0
                
                totalCompliance += compliance * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? totalCompliance / totalWeight : 1.0m;
        }

        // DEPRECATED: Old keyword-based method - kept for backward compatibility
        private decimal GetDietaryCompliance(FoodType foodType, string restrictionName)
        {
            var foodName = foodType.Name.ToLowerInvariant();
            var description = (foodType.Description ?? "").ToLowerInvariant();
            var restrictionLower = restrictionName.ToLowerInvariant();
            
            switch (restrictionLower)
            {
                case "vegan":
                    return ContainsAnimalProducts(foodName, description) ? 0m : 1m;
                case "vegetarian":
                    return ContainsMeat(foodName, description) ? 0m : 1m;
                case "gluten-free":
                    return ContainsGluten(foodName, description) ? 0m : 1m;
                case "dairy-free":
                    return ContainsDairy(foodName, description) ? 0m : 1m;
                case "nut-free":
                    return ContainsNuts(foodName, description) ? 0m : 1m;
                case "low-sodium":
                    return IsLowSodium(foodName, description) ? 1m : 0.5m;
                default:
                    return 1m; // Unknown restriction = neutral
            }
        }

        // NEW: Database-based dietary compliance (Evidence-Based)
        // Academic Justification: Precise compliance checking using structured database fields
        private decimal GetDietaryComplianceFromDatabase(FoodType foodType, string restrictionName)
        {
            var restrictionLower = restrictionName.ToLowerInvariant();
            
            return restrictionLower switch
            {
                // Vegan compliance: Must be explicitly marked as vegan
                "vegan" => foodType.IsVegan ? 1.0m : 0.0m,
                
                // Vegetarian compliance: Must not contain meat
                "vegetarian" => foodType.ContainsMeat ? 0.0m : 1.0m,
                
                // Gluten-free compliance: Must not contain gluten
                "gluten-free" => foodType.ContainsGluten ? 0.0m : 1.0m,
                
                // Dairy-free compliance: Must not contain dairy
                "dairy-free" => foodType.ContainsDairy ? 0.0m : 1.0m,
                
                // Nut-free compliance: Must not contain nuts
                "nut-free" => foodType.ContainsNuts ? 0.0m : 1.0m,
                
                // Low-sodium compliance: Food must be marked as low sodium
                "low-sodium" => foodType.IsLowSodium ? 1.0m : 0.3m,
                
                // Health-supportive restrictions
                "low-calorie" => foodType.IsLowCalorie ? 1.0m : 0.5m,
                "high-protein" => foodType.IsHighProtein ? 1.0m : 0.5m,
                
                // Default: If restriction not specifically handled, use conservative compliance
                _ => foodType.IsVegetarian ? 0.8m : 0.6m
            };
        }

                private decimal CalculateHealthConditionAlignment(FoodType foodType, List<UserHealthCondition> healthConditions)
        {
            if (!healthConditions.Any()) return 1.0m; // No conditions = neutral
            
            decimal totalAlignment = 0m;
            decimal totalWeight = 0m;
            
            foreach (var condition in healthConditions)
            {
                // Use DATABASE FIELDS for precise health condition alignment
                decimal alignment = GetHealthAlignmentFromDatabase(foodType, condition.HealthCondition.Name);
                decimal weight = condition.SeverityLevel / 10.0m; // Convert 1-10 to 0.1-1.0
                
                totalAlignment += alignment * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? totalAlignment / totalWeight : 1.0m;
        }

        // DEPRECATED: Old keyword-based method - kept for backward compatibility
        private decimal GetHealthAlignment(FoodType foodType, string conditionName)
        {
            var foodName = foodType.Name.ToLowerInvariant();
            var description = (foodType.Description ?? "").ToLowerInvariant();
            var conditionLower = conditionName.ToLowerInvariant();
            
            switch (conditionLower)
            {
                case "diabetes":
                    return IsLowSugar(foodName, description) && IsLowGlycemic(foodName, description) ? 1m : 0.3m;
                case "hypertension":
                    return IsLowSodium(foodName, description) && IsHeartHealthy(foodName, description) ? 1m : 0.4m;
                case "heart disease":
                    return IsHeartHealthy(foodName, description) && IsLowCholesterol(foodName, description) ? 1m : 0.3m;
                case "obesity":
                    return IsLowCalorie(foodName, description) && IsHealthyFood(foodName, description) ? 1m : 0.4m;
                default:
                    return 1m; // Unknown condition = neutral
            }
        }

        // NEW: Database-based health alignment (Evidence-Based)
        // Academic Justification: Precise health condition alignment using structured database fields
        private decimal GetHealthAlignmentFromDatabase(FoodType foodType, string conditionName)
        {
            var conditionLower = conditionName.ToLowerInvariant();
            
            return conditionLower switch
            {
                // Diabetes: Prioritize low glycemic foods
                "diabetes" => foodType.IsLowGlycemic ? 0.95m : 
                             (foodType.IsHealthy && !foodType.ContainsGluten) ? 0.7m : 0.4m,
                
                // Hypertension: Prioritize low sodium foods
                "hypertension" => foodType.IsLowSodium ? 0.95m : 
                                 (foodType.IsHealthy && foodType.IsVegetarian) ? 0.7m : 0.4m,
                
                // Heart Disease: Prioritize heart healthy foods
                "heart disease" => foodType.IsHeartHealthy ? 0.95m : 
                                  (foodType.IsLowSodium && foodType.IsHealthy) ? 0.8m : 0.5m,
                
                // Obesity: Prioritize low calorie, nutrient dense foods
                "obesity" => foodType.IsLowCalorie ? 0.95m : 
                            (foodType.IsHealthy && foodType.IsHighProtein) ? 0.8m : 0.3m,
                
                // Anemia: Prioritize iron-rich foods
                "anemia" => foodType.IsIronRich ? 0.95m : 
                           (foodType.IsHighProtein && foodType.IsHealthy) ? 0.7m : 0.5m,
                
                // Celiac Disease: Must be gluten-free
                "celiac disease" => foodType.ContainsGluten ? 0.0m : 0.95m,
                
                // General health conditions: Favor healthy, nutrient-dense options
                _ => foodType.IsHealthy ? 0.8m : 
                     foodType.IsNutrientDense ? 0.7m : 0.6m
            };
        }

        // NEW: Behavioral Pattern Weight Calculation (100% Formula Aligned)
        // FORMULA: W_behavioral = min(1.0, S_base + S_cuisine + S_temporal + S_contextual + S_foodpref)
        private decimal CalculateBehavioralPatternWeight(FoodType foodType, List<UserPreferencePattern> userPatterns, string queryLower, string? cuisineType = null, string? currentLocation = null, List<UserFoodType>? userFoodTypes = null)
        {
            // S_base = 0.5 (baseline behavioral score)
            decimal S_base = 0.5m;
            
            // Get cuisine type for this food (mapped from food name)
            var mappedCuisine = cuisineType ?? MapFoodToCuisine(foodType.Name);
            
            // S_cuisine = 0.3 × Confidence_cuisine × M_cuisine
            decimal S_cuisine = 0.0m;
            var cuisinePattern = userPatterns.FirstOrDefault(p => p.PatternType == "cuisine");
            if (cuisinePattern != null && !string.IsNullOrEmpty(mappedCuisine))
            {
                decimal M_cuisine = mappedCuisine.Equals(cuisinePattern.PatternValue, StringComparison.OrdinalIgnoreCase) ? 1.0m : 0.0m;
                S_cuisine = 0.3m * cuisinePattern.Confidence * M_cuisine;
            }
            
            // S_temporal = 0.2 × Confidence_time × M_time
            decimal S_temporal = 0.0m;
            var timePattern = userPatterns.FirstOrDefault(p => p.PatternType == "time");
            if (timePattern != null)
            {
                var currentTime = GetTimeRange(DateTime.UtcNow.Hour);
                decimal M_time = currentTime.Equals(timePattern.PatternValue, StringComparison.OrdinalIgnoreCase) ? 1.0m : 0.0m;
                S_temporal = 0.2m * timePattern.Confidence * M_time;
            }
            
            // S_contextual = 0.2 × Confidence_location × M_location
            decimal S_contextual = 0.0m;
            var locationPattern = userPatterns.FirstOrDefault(p => p.PatternType == "location");
            if (locationPattern != null && !string.IsNullOrEmpty(currentLocation))
            {
                decimal M_location = DoesLocationMatch(currentLocation, locationPattern.PatternValue) ? 1.0m : 0.0m;
                S_contextual = 0.2m * locationPattern.Confidence * M_location;
            }
            
            // S_foodpref = 0.3 × UserFoodType score (from behavioral learning)
            decimal S_foodpref = 0.0m;
            if (userFoodTypes != null)
            {
                var foodPref = userFoodTypes.FirstOrDefault(uft => uft.FoodTypeId == foodType.FoodTypeId);
                if (foodPref != null)
                {
                    // Map PreferenceScore (1-10) to behavioral contribution (0-0.3)
                    S_foodpref = 0.3m * (foodPref.PreferenceScore / 10.0m);
                }
            }
            
            // W_behavioral = min(1.0, S_base + S_cuisine + S_temporal + S_contextual + S_foodpref)
            decimal W_behavioral = S_base + S_cuisine + S_temporal + S_contextual + S_foodpref;
            
            return Math.Min(1.0m, W_behavioral);
        }
        
        // Helper method to extract current location from query
        private string? ExtractCurrentLocationFromQuery(string queryLower)
        {
            var locationKeywords = new Dictionary<string, string>
            {
                { "downtown", "downtown" },
                { "mall", "mall" },
                { "university", "university" },
                { "near campus", "university" },
                { "city center", "downtown" },
                { "near me", "general" },
                { "nearby", "general" }
            };
            
            foreach (var keyword in locationKeywords)
            {
                if (queryLower.Contains(keyword.Key))
                {
                    return keyword.Value;
                }
            }
            
            return null;
        }
        
        // Helper method to check if current location matches user's location pattern
        private bool DoesLocationMatch(string currentLocation, string patternLocation)
        {
            if (string.IsNullOrEmpty(currentLocation) || string.IsNullOrEmpty(patternLocation))
                return false;
                
            // Exact match
            if (currentLocation.Equals(patternLocation, StringComparison.OrdinalIgnoreCase))
                return true;
                
            // Fuzzy matching for similar locations
            var currentLower = currentLocation.ToLowerInvariant();
            var patternLower = patternLocation.ToLowerInvariant();
            
            // Check for partial matches
            if ((currentLower.Contains("downtown") && patternLower.Contains("downtown")) ||
                (currentLower.Contains("mall") && patternLower.Contains("mall")) ||
                (currentLower.Contains("university") && patternLower.Contains("university")) ||
                (currentLower.Contains("general") && patternLower.Contains("general")))
            {
                return true;
            }
            
            return false;
        }

        // Helper methods for dietary and health checks
        private bool ContainsAnimalProducts(string foodName, string description)
        {
            var animalKeywords = new[] { "meat", "chicken", "beef", "pork", "fish", "cheese", "milk", "egg", "dairy", "butter" };
            return animalKeywords.Any(ak => foodName.Contains(ak) || description.Contains(ak));
        }

        private bool ContainsMeat(string foodName, string description)
        {
            var meatKeywords = new[] { "meat", "chicken", "beef", "pork", "lamb", "turkey", "bacon", "ham" };
            return meatKeywords.Any(mk => foodName.Contains(mk) || description.Contains(mk));
        }

        private bool ContainsGluten(string foodName, string description)
        {
            var glutenKeywords = new[] { "wheat", "bread", "pasta", "flour", "gluten", "barley", "rye" };
            return glutenKeywords.Any(gk => foodName.Contains(gk) || description.Contains(gk));
        }

        private bool ContainsDairy(string foodName, string description)
        {
            var dairyKeywords = new[] { "milk", "cheese", "butter", "cream", "dairy", "yogurt" };
            return dairyKeywords.Any(dk => foodName.Contains(dk) || description.Contains(dk));
        }

        private bool ContainsNuts(string foodName, string description)
        {
            var nutKeywords = new[] { "nuts", "peanut", "almond", "walnut", "cashew", "pistachio" };
            return nutKeywords.Any(nk => foodName.Contains(nk) || description.Contains(nk));
        }

        private bool IsLowSodium(string foodName, string description)
        {
            var highSodiumKeywords = new[] { "salty", "pickle", "processed", "cured", "smoked" };
            var lowSodiumKeywords = new[] { "fresh", "steamed", "grilled", "low sodium" };
            
            if (highSodiumKeywords.Any(hsk => foodName.Contains(hsk) || description.Contains(hsk)))
                return false;
                
            return lowSodiumKeywords.Any(lsk => foodName.Contains(lsk) || description.Contains(lsk));
        }

        private bool IsLowSugar(string foodName, string description)
        {
            var highSugarKeywords = new[] { "sweet", "candy", "dessert", "cake", "sugar", "syrup" };
            return !highSugarKeywords.Any(hsk => foodName.Contains(hsk) || description.Contains(hsk));
        }

        private bool IsHealthyFood(string foodName, string description)
        {
            var healthyKeywords = new[] { "salad", "vegetable", "fruit", "whole grain", "lean", "grilled", "steamed" };
            return healthyKeywords.Any(hk => foodName.Contains(hk) || description.Contains(hk));
        }

        private bool IsLowGlycemic(string foodName, string description)
        {
            var lowGlycemicKeywords = new[] { "vegetable", "protein", "fiber", "whole grain" };
            var highGlycemicKeywords = new[] { "white rice", "white bread", "sugar", "potato" };
            
            if (highGlycemicKeywords.Any(hgk => foodName.Contains(hgk) || description.Contains(hgk)))
                return false;
                
            return lowGlycemicKeywords.Any(lgk => foodName.Contains(lgk) || description.Contains(lgk));
        }

        private bool IsHeartHealthy(string foodName, string description)
        {
            var heartHealthyKeywords = new[] { "fish", "salmon", "olive oil", "nuts", "seeds", "avocado", "whole grain", "oats" };
            return heartHealthyKeywords.Any(hhk => foodName.Contains(hhk) || description.Contains(hhk));
        }

        private bool IsLowCholesterol(string foodName, string description)
        {
            var highCholesterolKeywords = new[] { "fried", "butter", "cream", "fatty meat" };
            var lowCholesterolKeywords = new[] { "steamed", "grilled", "lean", "vegetable", "fruit" };
            
            if (highCholesterolKeywords.Any(hck => foodName.Contains(hck) || description.Contains(hck)))
                return false;
                
            return lowCholesterolKeywords.Any(lck => foodName.Contains(lck) || description.Contains(lck));
        }

        private bool IsLowCalorie(string foodName, string description)
        {
            var lowCalorieKeywords = new[] { "salad", "vegetable", "lean", "steamed", "grilled", "low calorie" };
            return lowCalorieKeywords.Any(lck => foodName.Contains(lck) || description.Contains(lck));
        }

        // NEW: Nutritional Optimization Method (Evidence-Based Database Fields)
        // Academic Justification: Nutritional adequacy assessment based on health goals and food properties
        // FORMULA: W_nutrition = min(1.0, max(0.0, S_base + S_quality + S_micronutrient + S_health-specific))
        private decimal CalculateNutritionalOptimization(FoodType foodType, List<UserHealthCondition> healthConditions, bool wantsHealthy)
        {
            // S_base = 0.5 (baseline nutritional score)
            decimal S_base = 0.5m;
            
            // S_quality = 0.2·I_healthy + 0.1·I_low-cal·Q_healthy + 0.1·I_protein
            decimal I_healthy = foodType.IsHealthy ? 1.0m : 0.0m;
            decimal I_lowCal = foodType.IsLowCalorie ? 1.0m : 0.0m;
            decimal Q_healthy = wantsHealthy ? 1.0m : 0.0m;
            decimal I_protein = foodType.IsHighProtein ? 1.0m : 0.0m;
            
            decimal S_quality = (0.2m * I_healthy) + (0.1m * I_lowCal * Q_healthy) + (0.1m * I_protein);
            
            // S_micronutrient = 0.15·I_nutrient-dense + 0.15·I_vitamin-rich
            decimal I_nutrientDense = foodType.IsNutrientDense ? 1.0m : 0.0m;
            decimal I_vitaminRich = foodType.IsVitaminRich ? 1.0m : 0.0m;
            
            decimal S_micronutrient = (0.15m * I_nutrientDense) + (0.15m * I_vitaminRich);
            
            // S_health-specific = Σ(j=1 to m) 0.1·A_j·(severity_j/10)
            decimal S_healthSpecific = 0.0m;
            
            foreach (var condition in healthConditions)
            {
                decimal A_j = GetNutritionalAlignmentScore(foodType, condition.HealthCondition.Name);
                decimal severity_j = condition.SeverityLevel / 10.0m; // Normalize to 0.1-1.0
                
                S_healthSpecific += 0.1m * A_j * severity_j;
            }
            
            // W_nutrition = min(1.0, max(0.0, S_base + S_quality + S_micronutrient + S_health-specific))
            decimal W_nutrition = S_base + S_quality + S_micronutrient + S_healthSpecific;
            
            return Math.Min(1.0m, Math.Max(0.0m, W_nutrition));
        }
        
        // Helper method to calculate nutritional alignment score for health conditions
        // Returns alignment score A_j for condition j based on food's nutritional properties
        private decimal GetNutritionalAlignmentScore(FoodType foodType, string conditionName)
        {
            var conditionLower = conditionName.ToLowerInvariant();
            
            return conditionLower switch
            {
                // Diabetes: Prioritize low glycemic foods
                "diabetes" => foodType.IsLowGlycemic ? 0.95m : 
                             (foodType.IsHealthy && !foodType.ContainsGluten) ? 0.7m : 0.4m,
                
                // Hypertension: Prioritize low sodium foods  
                "hypertension" => foodType.IsLowSodium ? 0.95m :
                                 (foodType.IsHealthy && foodType.IsVegetarian) ? 0.7m : 0.4m,
                
                // Heart Disease: Prioritize heart healthy foods
                "heart disease" => foodType.IsHeartHealthy ? 0.95m :
                                  (foodType.IsLowSodium && foodType.IsHealthy) ? 0.8m : 0.5m,
                
                // Obesity: Prioritize low calorie, nutrient dense foods
                "obesity" => foodType.IsLowCalorie ? 0.95m :
                            (foodType.IsHealthy && foodType.IsHighProtein) ? 0.8m : 0.3m,
                
                // Anemia: Prioritize iron-rich foods
                "anemia" => foodType.IsIronRich ? 0.95m :
                           (foodType.IsHighProtein && foodType.IsHealthy) ? 0.7m : 0.5m,
                
                // Celiac Disease: Must be gluten-free
                "celiac disease" => foodType.ContainsGluten ? 0.0m : 0.95m,
                
                // General health conditions: Favor healthy, nutrient-dense options
                _ => foodType.IsHealthy ? 0.8m : 
                     foodType.IsNutrientDense ? 0.7m : 0.6m
            };
        }

        // Additional nutritional assessment methods
        private bool IsHighProtein(string foodName, string description)
        {
            var proteinKeywords = new[] { "chicken", "fish", "salmon", "tuna", "beef", "pork", "egg", "tofu", "quinoa", "beans", "lentils" };
            return proteinKeywords.Any(pk => foodName.Contains(pk) || description.Contains(pk));
        }

        private bool IsNutrientDense(string foodName, string description)
        {
            var nutrientDenseKeywords = new[] { "vegetable", "fruit", "whole grain", "nuts", "seeds", "fish", "leafy", "broccoli", "spinach" };
            return nutrientDenseKeywords.Any(ndk => foodName.Contains(ndk) || description.Contains(ndk));
        }

        private bool IsRichInVitamins(string foodName, string description)
        {
            var vitaminRichKeywords = new[] { "fruit", "vegetable", "citrus", "berry", "leafy", "colorful", "orange", "carrot", "bell pepper" };
            return vitaminRichKeywords.Any(vrk => foodName.Contains(vrk) || description.Contains(vrk));
        }

        private bool IsIronRich(string foodName, string description)
        {
            var ironRichKeywords = new[] { "red meat", "beef", "liver", "spinach", "lentils", "beans", "tofu", "dark leafy" };
            return ironRichKeywords.Any(irk => foodName.Contains(irk) || description.Contains(irk));
        }
    }
}


