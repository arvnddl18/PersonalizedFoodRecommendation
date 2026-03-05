using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Capstone.Models.NomsaurModel;
using BCrypt.Net;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Text.Json;

namespace Capstone.Controllers
{
    public class FoodTypeJson
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
    }
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private const int CACHE_DURATION_MINUTES = 30;

        public AccountController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string email)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                // Check if email is being changed and if it's already taken
                if (email != user.Email)
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.UserId != userId.Value);
                    if (existingUser != null)
                    {
                        return Json(new { success = false, error = "This email is already in use by another account." });
                    }
                    user.Email = email;
                }

                user.Name = name;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile updated successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, error = "Failed to update profile" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                // Check if user has a password (OAuth users don't)
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    return Json(new { success = false, error = "OAuth users cannot change password. Your account is linked with " + (user.Provider ?? "external provider") + "." });
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    return Json(new { success = false, error = "Current password is incorrect." });
                }

                // Validate new password
                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, error = "New password and confirmation do not match." });
                }

                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, error = "New password must be at least 6 characters long." });
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Password changed successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, error = "Failed to change password" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string confirmEmail)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Verify email confirmation
            if (confirmEmail != user.Email)
            {
                TempData["ErrorMessage"] = "Email confirmation does not match. Account deletion cancelled.";
                return RedirectToAction("Index");
            }

            // Delete all user-related data
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId.Value);
            if (userProfile != null)
            {
                _context.UserProfiles.Remove(userProfile);
            }

            var userFoodTypes = await _context.UserFoodTypes.Where(uft => uft.UserId == userId.Value).ToListAsync();
            _context.UserFoodTypes.RemoveRange(userFoodTypes);

            var userDietaryRestrictions = await _context.UserDietaryRestrictions.Where(udr => udr.UserId == userId.Value).ToListAsync();
            _context.UserDietaryRestrictions.RemoveRange(userDietaryRestrictions);

            var userHealthConditions = await _context.UserHealthConditions.Where(uhc => uhc.UserId == userId.Value).ToListAsync();
            _context.UserHealthConditions.RemoveRange(userHealthConditions);

            var favorites = await _context.UserFavoriteRestaurants.Where(f => f.UserId == userId.Value).ToListAsync();
            _context.UserFavoriteRestaurants.RemoveRange(favorites);

            var userBehaviors = await _context.UserBehaviors.Where(ub => ub.UserId == userId.Value).ToListAsync();
            _context.UserBehaviors.RemoveRange(userBehaviors);

            var userPreferencePatterns = await _context.UserPreferencePatterns.Where(upp => upp.UserId == userId.Value).ToListAsync();
            _context.UserPreferencePatterns.RemoveRange(userPreferencePatterns);

            var chatSessions = await _context.ChatSessions.Where(cs => cs.UserId == userId.Value).ToListAsync();
            // Delete chat messages associated with these sessions
            foreach (var session in chatSessions)
            {
                var messages = await _context.ChatMessages.Where(cm => cm.SessionId == session.SessionId).ToListAsync();
                _context.ChatMessages.RemoveRange(messages);
            }
            _context.ChatSessions.RemoveRange(chatSessions);

            // Finally, delete the user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Clear session and redirect to home
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Your account has been permanently deleted.";
            return RedirectToAction("Index", "Home");
        }

        // API endpoints for the preferences modal
        [HttpGet]
        public async Task<IActionResult> GetFoodTypes()
        {
            try
            {
                var foodTypes = new List<object>();
                
                // Load from JSON files
                var jsonFiles = new[]
                {
                    "wwwroot/data/foodtypes-51-500.json",
                    "wwwroot/data/foodtypes-501-800.json", 
                    "wwwroot/data/foodtypes-801-1154.json"
                };

                foreach (var file in jsonFiles)
                {
                    if (System.IO.File.Exists(file))
                    {
                        var jsonContent = await System.IO.File.ReadAllTextAsync(file);
                        var fileData = JsonSerializer.Deserialize<List<FoodTypeJson>>(jsonContent);
                        if (fileData != null)
                        {
                            foodTypes.AddRange(fileData.Select(item => new
                            {
                                foodTypeId = item.id,
                                name = item.name
                            }));
                        }
                    }
                }

                return Json(foodTypes.OrderBy(ft => ((dynamic)ft).name));
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to load food types" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDietaryRestrictions()
        {
            try
            {
                var dietaryRestrictions = await GetCachedDietaryRestrictionsAsync();
                var result = dietaryRestrictions.Select(dr => new
                {
                    dietaryRestrictionId = dr.DietaryRestrictionId,
                    name = dr.Name
                }).ToList();

                return Json(result);
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to load dietary restrictions" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHealthConditions()
        {
            try
            {
                var healthConditions = await GetCachedHealthConditionsAsync();
                var result = healthConditions.Select(hc => new
                {
                    healthConditionId = hc.HealthConditionId,
                    name = hc.Name
                }).ToList();

                return Json(result);
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to load health conditions" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentPreferences()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { error = "User not authenticated" });
            }

            try
            {
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId.Value);

                var selectedFoodTypes = await _context.UserFoodTypes
                    .Where(uft => uft.UserId == userId.Value)
                    .Select(uft => uft.FoodTypeId)
                    .ToListAsync();

                var selectedDietaryRestrictions = await _context.UserDietaryRestrictions
                    .Where(udr => udr.UserId == userId.Value)
                    .Select(udr => new
                    {
                        id = udr.DietaryRestrictionId,
                        importanceLevel = udr.ImportanceLevel
                    })
                    .ToListAsync();

                var selectedHealthConditions = await _context.UserHealthConditions
                    .Where(uhc => uhc.UserId == userId.Value)
                    .Select(uhc => new
                    {
                        id = uhc.HealthConditionId,
                        severityLevel = uhc.SeverityLevel
                    })
                    .ToListAsync();

                return Json(new
                {
                    preferredLocation = userProfile?.PreferredLocation ?? "",
                    priceRange = userProfile?.PriceRange ?? "",
                    selectedFoodTypes = selectedFoodTypes,
                    selectedDietaryRestrictions = selectedDietaryRestrictions,
                    selectedHealthConditions = selectedHealthConditions
                });
            }
            catch (Exception)
            {
                return Json(new { error = "Failed to load current preferences" });
            }
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

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFoodTypes()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            return await SaveFoodTypesInternal(userId.Value);
        }

        private async Task<IActionResult> SaveFoodTypesInternal(int userId)
        {
            try
            {
                var form = Request.Form;
                var selectedFoodTypeIds = new List<int>();
                
                // Debug: Log all form keys and values
                Console.WriteLine("=== SaveFoodTypesInternal Debug ===");
                Console.WriteLine($"UserId: {userId}");
                foreach (var key in form.Keys)
                {
                    Console.WriteLine($"Form Key: {key}, Value: {form[key]}");
                }
                
                // Get all selected food type IDs by checking each indexed field
                var maxIndex = 0;
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("FoodTypes[") && key.EndsWith("].FoodTypeId"))
                    {
                        var indexStr = key.Split('[')[1].Split(']')[0];
                        if (int.TryParse(indexStr, out var index))
                        {
                            maxIndex = Math.Max(maxIndex, index);
                        }
                    }
                }
                
                Console.WriteLine($"Max index found: {maxIndex}");
                
                for (int i = 0; i <= maxIndex; i++)
                {
                    var isSelectedKey = $"FoodTypes[{i}].IsSelected";
                    var foodTypeIdKey = $"FoodTypes[{i}].FoodTypeId";
                    
                    if (form.ContainsKey(isSelectedKey) && form.ContainsKey(foodTypeIdKey))
                    {
                        var isSelected = form[isSelectedKey].ToString();
                        var foodTypeIdStr = form[foodTypeIdKey].ToString();
                        
                        Console.WriteLine($"Index {i}: IsSelected={isSelected}, FoodTypeId={foodTypeIdStr}");
                        
                        if (isSelected == "on" && int.TryParse(foodTypeIdStr, out var foodTypeId))
                        {
                            selectedFoodTypeIds.Add(foodTypeId);
                            Console.WriteLine($"Added food type ID: {foodTypeId}");
                        }
                    }
                }
                
                Console.WriteLine($"Total selected food types: {selectedFoodTypeIds.Count}");

                // Remove existing food type preferences
                var existingFoodTypes = await _context.UserFoodTypes
                    .Where(uft => uft.UserId == userId)
                    .ToListAsync();
                _context.UserFoodTypes.RemoveRange(existingFoodTypes);

                // Add new food type preferences
                foreach (var foodTypeId in selectedFoodTypeIds)
                {
                    _context.UserFoodTypes.Add(new UserFoodType
                    {
                        UserId = userId,
                        FoodTypeId = foodTypeId,
                        PreferenceLevel = "1",
                        AddedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Food types updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Failed to save food types: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDietaryRestrictions()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            return await SaveDietaryRestrictionsInternal(userId.Value);
        }

        private async Task<IActionResult> SaveDietaryRestrictionsInternal(int userId)
        {
            try
            {
                var form = Request.Form;
                var selectedDietaryRestrictions = new List<(int Id, int ImportanceLevel)>();
                
                // Debug: Log all form keys and values
                Console.WriteLine("=== SaveDietaryRestrictionsInternal Debug ===");
                Console.WriteLine($"UserId: {userId}");
                foreach (var key in form.Keys)
                {
                    Console.WriteLine($"Form Key: {key}, Value: {form[key]}");
                }
                
                // Get all selected dietary restrictions by checking each indexed field
                var maxIndex = 0;
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("DietaryRestrictions[") && key.EndsWith("].DietaryRestrictionId"))
                    {
                        var indexStr = key.Split('[')[1].Split(']')[0];
                        if (int.TryParse(indexStr, out var index))
                        {
                            maxIndex = Math.Max(maxIndex, index);
                        }
                    }
                }
                
                Console.WriteLine($"Max dietary restrictions index found: {maxIndex}");
                
                for (int i = 0; i <= maxIndex; i++)
                {
                    var isSelectedKey = $"DietaryRestrictions[{i}].IsSelected";
                    var dietaryIdKey = $"DietaryRestrictions[{i}].DietaryRestrictionId";
                    var importanceLevelKey = $"DietaryRestrictions[{i}].ImportanceLevel";
                    
                    if (form.ContainsKey(isSelectedKey) && form.ContainsKey(dietaryIdKey) && form.ContainsKey(importanceLevelKey))
                    {
                        var isSelected = form[isSelectedKey].ToString();
                        var dietaryIdStr = form[dietaryIdKey].ToString();
                        var importanceLevelStr = form[importanceLevelKey].ToString();
                        
                        Console.WriteLine($"Dietary Index {i}: IsSelected={isSelected}, DietaryId={dietaryIdStr}, Importance={importanceLevelStr}");
                        
                        if (isSelected == "on" && int.TryParse(dietaryIdStr, out var dietaryId) && int.TryParse(importanceLevelStr, out var importanceLevel))
                        {
                            selectedDietaryRestrictions.Add((dietaryId, importanceLevel));
                            Console.WriteLine($"Added dietary restriction ID: {dietaryId} with importance: {importanceLevel}");
                        }
                    }
                }
                
                Console.WriteLine($"Total selected dietary restrictions: {selectedDietaryRestrictions.Count}");

                // Remove existing dietary restrictions
                var existingDietaryRestrictions = await _context.UserDietaryRestrictions
                    .Where(udr => udr.UserId == userId)
                    .ToListAsync();
                _context.UserDietaryRestrictions.RemoveRange(existingDietaryRestrictions);

                // Add new dietary restrictions
                foreach (var (id, importanceLevel) in selectedDietaryRestrictions)
                {
                    _context.UserDietaryRestrictions.Add(new UserDietaryRestriction
                    {
                        UserId = userId,
                        DietaryRestrictionId = id,
                        ImportanceLevel = importanceLevel,
                        AddedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Dietary restrictions updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Failed to save dietary restrictions: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveHealthConditions()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            return await SaveHealthConditionsInternal(userId.Value);
        }

        private async Task<IActionResult> SaveHealthConditionsInternal(int userId)
        {
            try
            {
                var form = Request.Form;
                var selectedHealthConditions = new List<(int Id, int SeverityLevel)>();
                
                // Get all selected health conditions by checking each indexed field
                var maxIndex = 0;
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("HealthConditions[") && key.EndsWith("].HealthConditionId"))
                    {
                        var indexStr = key.Split('[')[1].Split(']')[0];
                        if (int.TryParse(indexStr, out var index))
                        {
                            maxIndex = Math.Max(maxIndex, index);
                        }
                    }
                }
                
                Console.WriteLine($"Max health conditions index found: {maxIndex}");
                
                for (int i = 0; i <= maxIndex; i++)
                {
                    var isSelectedKey = $"HealthConditions[{i}].IsSelected";
                    var healthIdKey = $"HealthConditions[{i}].HealthConditionId";
                    var severityLevelKey = $"HealthConditions[{i}].SeverityLevel";
                    
                    if (form.ContainsKey(isSelectedKey) && form.ContainsKey(healthIdKey) && form.ContainsKey(severityLevelKey))
                    {
                        var isSelected = form[isSelectedKey].ToString();
                        var healthIdStr = form[healthIdKey].ToString();
                        var severityLevelStr = form[severityLevelKey].ToString();
                        
                        Console.WriteLine($"Health Index {i}: IsSelected={isSelected}, HealthId={healthIdStr}, Severity={severityLevelStr}");
                        
                        if (isSelected == "on" && int.TryParse(healthIdStr, out var healthId) && int.TryParse(severityLevelStr, out var severityLevel))
                        {
                            selectedHealthConditions.Add((healthId, severityLevel));
                            Console.WriteLine($"Added health condition ID: {healthId} with severity: {severityLevel}");
                        }
                    }
                }
                
                Console.WriteLine($"Total selected health conditions: {selectedHealthConditions.Count}");

                // Remove existing health conditions
                var existingHealthConditions = await _context.UserHealthConditions
                    .Where(uhc => uhc.UserId == userId)
                    .ToListAsync();
                _context.UserHealthConditions.RemoveRange(existingHealthConditions);

                // Add new health conditions
                foreach (var (id, severityLevel) in selectedHealthConditions)
                {
                    _context.UserHealthConditions.Add(new UserHealthCondition
                    {
                        UserId = userId,
                        HealthConditionId = id,
                        SeverityLevel = severityLevel,
                        DiagnosedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Health conditions updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Failed to save health conditions: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLocationBudget()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            return await SaveLocationBudgetInternal(userId.Value);
        }

        private async Task<IActionResult> SaveLocationBudgetInternal(int userId)
        {
            try
            {
                var form = Request.Form;
                var preferredLocation = form["preferredLocation"].ToString();
                var priceRange = form["priceRange"].ToString();

                // Update or create user profile
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userProfile == null)
                {
                    userProfile = new UserProfile
                    {
                        UserId = userId,
                        PreferredLocation = preferredLocation,
                        PriceRange = priceRange,
                        LastUpdated = DateTime.Now
                    };
                    _context.UserProfiles.Add(userProfile);
                }
                else
                {
                    userProfile.PreferredLocation = preferredLocation;
                    userProfile.PriceRange = priceRange;
                    userProfile.LastUpdated = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Location and budget updated successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, error = "Failed to save location and budget" });
            }
        }
    }
}

