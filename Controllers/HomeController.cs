using System.Diagnostics;
using Capstone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Capstone.Data;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, IConfiguration config)
        {
            _logger = logger;
            _context = context;
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Onboarding()
        {
            return View();
        }

        public async Task<IActionResult> Chat()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            // If logged in, enforce onboarding and load user; otherwise allow anonymous guest access
            if (userId.HasValue)
            {
                // Check if user has completed onboarding (has any preferences, restrictions, or conditions)
                var hasCompletedOnboarding = await HasUserCompletedOnboarding(userId.Value);
                if (!hasCompletedOnboarding)
                {
                    TempData["InfoMessage"] = "Please complete your food preferences to get personalized recommendations.";
                    return RedirectToAction("Index", "Preferences");
                }

                // Load current user for the Chat view (used by profile pill)
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId.Value);

                return View(user);
            }

            // Guest access: render Chat view with no user model
            return View(model: null);
        }

        public async Task<IActionResult> ChatMobile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            // Allow anonymous guest access on mobile as well
            if (!userId.HasValue)
            {
                return View(model: null);
            }

            // Check if user has completed onboarding (has any preferences, restrictions, or conditions)
            var hasCompletedOnboarding = await HasUserCompletedOnboarding(userId.Value);
            if (!hasCompletedOnboarding)
            {
                TempData["InfoMessage"] = "Please complete your food preferences to get personalized recommendations.";
                return RedirectToAction("Index", "Preferences");
            }

            // Load current user for the ChatMobile view (used by profile pill)
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            return View(user);
        }

        // Helper method to check if user has completed onboarding
        private async Task<bool> HasUserCompletedOnboarding(int userId)
        {
            // Check if user has any food preferences, dietary restrictions, or health conditions
            var hasFoodPreferences = await _context.UserFoodTypes.AnyAsync(uft => uft.UserId == userId);
            var hasDietaryRestrictions = await _context.UserDietaryRestrictions.AnyAsync(udr => udr.UserId == userId);
            var hasHealthConditions = await _context.UserHealthConditions.AnyAsync(uhc => uhc.UserId == userId);

            return hasFoodPreferences || hasDietaryRestrictions || hasHealthConditions;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult GoogleMapsTest()
        {
            return View();
        }

        public IActionResult PlacesMenuTest()
        {
            ViewBag.GoogleMapsApiKey = _config["GoogleMaps:ApiKey"] ?? string.Empty;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
