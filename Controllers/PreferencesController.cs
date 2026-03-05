using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using static Capstone.Models.NomsaurModel;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace Capstone.Controllers
{
    public class PreferencesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private const int CACHE_DURATION_MINUTES = 30;

        public PreferencesController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            // Get cached dietary restrictions and health conditions (these are typically small datasets)
            var dietaryRestrictions = await GetCachedDietaryRestrictionsAsync();
            var healthConditions = await GetCachedHealthConditionsAsync();
            
            // Build food types query (no server-side search; handled on frontend)
            var foodTypesQuery = _context.FoodTypes.AsQueryable();
            
            // Fetch all (no pagination)
            var totalFoodTypes = await foodTypesQuery.CountAsync();
            var foodTypes = await foodTypesQuery
                .OrderBy(ft => ft.Name)
                .Select(ft => new FoodTypeSelection
                {
                    FoodTypeId = ft.FoodTypeId,
                    Name = ft.Name,
                    IsSelected = false
                })
                .ToListAsync();
            
            var viewModel = new PreferencesViewModel
            {
                FoodTypes = foodTypes,
                DietaryRestrictions = dietaryRestrictions.Select(dr => new DietaryRestrictionSelection
                {
                    DietaryRestrictionId = dr.DietaryRestrictionId,
                    Name = dr.Name,
                    IsSelected = false,
                    ImportanceLevel = 1
                }).ToList(),
                HealthConditions = healthConditions.Select(hc => new HealthConditionSelection
                {
                    HealthConditionId = hc.HealthConditionId,
                    Name = hc.Name,
                    IsSelected = false,
                    SeverityLevel = 1
                }).ToList(),
                SearchTerm = string.Empty,
                CurrentPage = 1,
                TotalPages = 1,
                TotalFoodTypes = totalFoodTypes
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Save(PreferencesViewModel model)
        {
            // Defensive: ensure model is not null to avoid NullReferenceException on bad binds
            if (model == null)
            {
                model = new PreferencesViewModel();
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Validation: Ensure user selects at least some preferences
            // Accept selections from either server-rendered list or client JSON IDs
            // Collect selected food type IDs from both model binding and raw form (fallback)
            var selectedFoodTypeIds = (model.SelectedFoodTypeIds ?? new List<int>()).ToList();
            var formFoodIds = new List<int>();
            var formCollection = Request != null ? Request.Form : null;
            if (formCollection != null && formCollection.ContainsKey("SelectedFoodTypeIds"))
            {
                foreach (var val in formCollection["SelectedFoodTypeIds"]) {
                    if (int.TryParse(val, out var parsed)) formFoodIds.Add(parsed);
                }
            }
            selectedFoodTypeIds.AddRange(formFoodIds);
            selectedFoodTypeIds = selectedFoodTypeIds.Distinct().ToList();

            // Ensure lists are initialized to avoid null checks later
            model.FoodTypes = model.FoodTypes ?? new List<FoodTypeSelection>();
            model.DietaryRestrictions = model.DietaryRestrictions ?? new List<DietaryRestrictionSelection>();
            model.HealthConditions = model.HealthConditions ?? new List<HealthConditionSelection>();

            bool hasFoodPreferences = selectedFoodTypeIds.Any()
                || model.FoodTypes.Any(ft => ft.IsSelected)
                || (formCollection != null && formCollection.Where(kvp => kvp.Key.Contains("FoodTypes") && kvp.Key.EndsWith(".IsSelected")).Any(kvp => kvp.Value.Any(v => v.Contains("true") || v.Equals("on", System.StringComparison.OrdinalIgnoreCase))));

            bool hasDietaryRestrictions = model.DietaryRestrictions.Any(dr => dr.IsSelected)
                || (formCollection != null && formCollection.Where(kvp => kvp.Key.Contains("DietaryRestrictions") && kvp.Key.EndsWith(".IsSelected")).Any(kvp => kvp.Value.Any(v => v.Contains("true") || v.Equals("on", System.StringComparison.OrdinalIgnoreCase))));
            bool hasHealthConditions = model.HealthConditions.Any(hc => hc.IsSelected)
                || (formCollection != null && formCollection.Where(kvp => kvp.Key.Contains("HealthConditions") && kvp.Key.EndsWith(".IsSelected")).Any(kvp => kvp.Value.Any(v => v.Contains("true") || v.Equals("on", System.StringComparison.OrdinalIgnoreCase))));

            if (!hasFoodPreferences && !hasDietaryRestrictions && !hasHealthConditions)
            {
                TempData["ErrorMessage"] = "Please select at least one food preference, dietary restriction, or health condition to get personalized recommendations.";
                return RedirectToAction("Index");
            }

            // Save UserProfile (cleaned up - removed deprecated fields)
            var userProfile = new UserProfile
            {
                UserId = userId.Value,
                // REMOVED: DietaryPreference - now handled by UserDietaryRestriction table
                // REMOVED: FavoriteFood - now handled by behavioral analysis
                PreferredLocation = model.PreferredLocation,
                PriceRange = model.PriceRange,
                LastUpdated = System.DateTime.Now
            };
            _context.UserProfiles.Add(userProfile);

            // Save UserFoodTypes (onboarding food preferences)
            // Merge any checked items from the server-rendered list as fallback
            if (model.FoodTypes != null)
            {
                selectedFoodTypeIds.AddRange(model.FoodTypes.Where(ft => ft.IsSelected).Select(ft => ft.FoodTypeId));
                selectedFoodTypeIds = selectedFoodTypeIds.Distinct().ToList();
            }

            foreach (var foodTypeId in selectedFoodTypeIds)
            {
                var userFoodType = new UserFoodType
                {
                    UserId = userId.Value,
                    FoodTypeId = foodTypeId,
                    PreferenceLevel = "Preferred",
                    PreferenceScore = 7, // Higher score for onboarding selections
                    AddedAt = DateTime.Now
                };
                _context.UserFoodTypes.Add(userFoodType);
            }

            // Save Dietary Restrictions
            if (model.DietaryRestrictions != null)
            {
                foreach (var restriction in model.DietaryRestrictions.Where(dr => dr.IsSelected))
                {
                    var userDietaryRestriction = new UserDietaryRestriction
                    {
                        UserId = userId.Value,
                        DietaryRestrictionId = restriction.DietaryRestrictionId,
                        ImportanceLevel = restriction.ImportanceLevel,
                        AddedAt = System.DateTime.Now
                    };
                    _context.UserDietaryRestrictions.Add(userDietaryRestriction);
                }
            }

            // Save Health Conditions
            if (model.HealthConditions != null)
            {
                foreach (var condition in model.HealthConditions.Where(hc => hc.IsSelected))
                {
                    var userHealthCondition = new UserHealthCondition
                    {
                        UserId = userId.Value,
                        HealthConditionId = condition.HealthConditionId,
                        SeverityLevel = condition.SeverityLevel,
                        DiagnosedAt = System.DateTime.Now
                    };
                    _context.UserHealthConditions.Add(userHealthCondition);
                }
            }

            await _context.SaveChangesAsync();

            // Mark user as having completed onboarding
            HttpContext.Session.SetString("OnboardingCompleted", "true");

            return RedirectToAction("Chat", "Home");
        }

        // Helper method to check if user has completed onboarding
        public async Task<bool> HasUserCompletedOnboarding(int userId)
        {
            // Check if user has any food preferences, dietary restrictions, or health conditions
            var hasFoodPreferences = await _context.UserFoodTypes.AnyAsync(uft => uft.UserId == userId);
            var hasDietaryRestrictions = await _context.UserDietaryRestrictions.AnyAsync(udr => udr.UserId == userId);
            var hasHealthConditions = await _context.UserHealthConditions.AnyAsync(uhc => uhc.UserId == userId);

            return hasFoodPreferences || hasDietaryRestrictions || hasHealthConditions;
        }


        // Helper method to get cached dietary restrictions
        private async Task<List<DietaryRestriction>> GetCachedDietaryRestrictionsAsync()
        {
            const string cacheKey = "dietary_restrictions";
            
            if (!_cache.TryGetValue(cacheKey, out List<DietaryRestriction>? dietaryRestrictions))
            {
                dietaryRestrictions = await _context.DietaryRestrictions.ToListAsync();
                _cache.Set(cacheKey, dietaryRestrictions, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            }

            return dietaryRestrictions ?? new List<DietaryRestriction>();
        }

        // Helper method to get cached health conditions
        private async Task<List<HealthCondition>> GetCachedHealthConditionsAsync()
        {
            const string cacheKey = "health_conditions";
            
            if (!_cache.TryGetValue(cacheKey, out List<HealthCondition>? healthConditions))
            {
                healthConditions = await _context.HealthConditions.ToListAsync();
                _cache.Set(cacheKey, healthConditions, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            }

            return healthConditions ?? new List<HealthCondition>();
        }
    }
} 