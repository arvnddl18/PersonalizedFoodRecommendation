using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using static Capstone.Models.NomsaurModel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Capstone.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            // Verify reCAPTCHA
            var recaptchaResponse = Request.Form["g-recaptcha-response"].ToString();
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                ModelState.AddModelError("", "Please complete the reCAPTCHA verification.");
                return View(model);
            }

            var isRecaptchaValid = await VerifyRecaptcha(recaptchaResponse);
            if (!isRecaptchaValid)
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            // Debug: Log model state and data
            System.Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            System.Console.WriteLine($"Name: {model.Name}");
            System.Console.WriteLine($"Email: {model.Email}");
            System.Console.WriteLine($"Password: {model.Password}");
            System.Console.WriteLine($"ConfirmPassword: {model.ConfirmPassword}");
            
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if user already exists
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "User with this email already exists.");
                        return View(model);
                    }

                    // Hash the password using BCrypt
                    model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    model.CreatedAt = System.DateTime.Now;

                    _context.Users.Add(model);
                    await _context.SaveChangesAsync();

                    // Redirect to login page after successful registration
                    return RedirectToAction("Login", "Auth");
                }
                catch (System.Exception ex)
                {
                    // Log the detailed exception to the console for debugging
                    System.Console.WriteLine(ex.ToString());
                    // Add a model-level error to be displayed to the user
                    ModelState.AddModelError("", "An unexpected error occurred while creating your account. Please try again.");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User model)
        {
            // Verify reCAPTCHA
            var recaptchaResponse = Request.Form["g-recaptcha-response"].ToString();
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                ModelState.AddModelError("", "Please complete the reCAPTCHA verification.");
                return View(model);
            }

            var isRecaptchaValid = await VerifyRecaptcha(recaptchaResponse);
            if (!isRecaptchaValid)
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            // Debug: Log login attempt
            System.Console.WriteLine($"Login attempt - Email: {model.Email}, Password: {model.Password}");
            System.Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            // Debug: Log validation errors
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }
            
            // Clear validation errors for fields not used in login
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Name");
            ModelState.Remove("ConfirmPassword");
            
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                System.Console.WriteLine($"User found: {user != null}");
                if (user != null)
                {
                    System.Console.WriteLine($"Stored PasswordHash: {user.PasswordHash}");
                    System.Console.WriteLine($"Password verification: {BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash)}");
                }

                // Check if user exists and has a password (not OAuth user)
                if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                {
                    System.Console.WriteLine("Login failed - User not found or no password set");
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                // Verify password for regular users
                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    System.Console.WriteLine("Login failed - Invalid password");
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                // Update last login time
                user.LastLogin = System.DateTime.Now;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserId", user.UserId);

                // If user has no profile, redirect to preferences page
                if (user.UserProfile == null)
                {
                    return RedirectToAction("Index", "Preferences");
                }

                // Otherwise, redirect to chat
                return RedirectToAction("Chat", "Home");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    var openSessions = _context.ChatSessions
                        .Where(cs => cs.UserId == userId.Value && cs.EndedAt == null)
                        .ToList();
                    if (openSessions.Count > 0)
                    {
                        foreach (var s in openSessions)
                        {
                            s.EndedAt = System.DateTime.Now;
                        }
                        _context.SaveChanges();
                    }
                }
            }
            catch { /* best-effort session close */ }

            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // OAuth Login Methods
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleCallback", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public IActionResult FacebookLogin()
        {
            var redirectUrl = Url.Action("FacebookCallback", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Facebook");
        }

        // OAuth Registration Methods (same as login but with context)
        [HttpGet]
        public IActionResult GoogleRegister()
        {
            var redirectUrl = Url.Action("GoogleCallback", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            // Store context that this is a registration attempt
            HttpContext.Session.SetString("OAuthContext", "register");
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public IActionResult FacebookRegister()
        {
            var redirectUrl = Url.Action("FacebookCallback", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            // Store context that this is a registration attempt
            HttpContext.Session.SetString("OAuthContext", "register");
            return Challenge(properties, "Facebook");
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            System.Console.WriteLine("=== Google OAuth Callback Started ===");
            
            var result = await HttpContext.AuthenticateAsync("Google");
            System.Console.WriteLine($"Google authentication result: {result.Succeeded}");
            
            if (!result.Succeeded)
            {
                System.Console.WriteLine("Google authentication failed");
                TempData["ErrorMessage"] = "Google authentication failed.";
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            var providerId = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            System.Console.WriteLine($"Email: {email}");
            System.Console.WriteLine($"Name: {name}");
            System.Console.WriteLine($"ProviderId: {providerId}");

            if (string.IsNullOrEmpty(email))
            {
                System.Console.WriteLine("No email found in Google claims");
                TempData["ErrorMessage"] = "Unable to retrieve email from Google.";
                return RedirectToAction("Login");
            }

            System.Console.WriteLine("=== Calling ProcessOAuthLogin ===");
            return await ProcessOAuthLogin(email, name ?? "User", "google", providerId ?? "", claims);
        }

        [HttpGet]
        public async Task<IActionResult> FacebookCallback()
        {
            System.Console.WriteLine("=== Facebook OAuth Callback Started ===");
            
            var result = await HttpContext.AuthenticateAsync("Facebook");
            System.Console.WriteLine($"Facebook authentication result: {result.Succeeded}");
            
            if (!result.Succeeded)
            {
                System.Console.WriteLine("Facebook authentication failed");
                TempData["ErrorMessage"] = "Facebook authentication failed.";
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            var providerId = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            System.Console.WriteLine($"Email: {email}");
            System.Console.WriteLine($"Name: {name}");
            System.Console.WriteLine($"ProviderId: {providerId}");

            if (string.IsNullOrEmpty(email))
            {
                System.Console.WriteLine("No email found in Facebook claims");
                TempData["ErrorMessage"] = "Unable to retrieve email from Facebook.";
                return RedirectToAction("Login");
            }

            System.Console.WriteLine("=== Calling ProcessOAuthLogin ===");
            return await ProcessOAuthLogin(email, name ?? "User", "facebook", providerId ?? "", claims);
        }

        private async Task<IActionResult> ProcessOAuthLogin(string email, string name, string provider, string providerId, List<Claim> claims)
        {
            try
            {
                System.Console.WriteLine($"=== ProcessOAuthLogin Started ===");
                System.Console.WriteLine($"Email: {email}, Name: {name}, Provider: {provider}");
                
                // Test database connection first
                try
                {
                    var userCount = await _context.Users.CountAsync();
                    System.Console.WriteLine($"Database connection successful. Total users: {userCount}");
                }
                catch (Exception dbTestEx)
                {
                    System.Console.WriteLine($"Database connection test failed: {dbTestEx.Message}");
                    throw;
                }
                
                // Check if user exists by email
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Email == email);

                System.Console.WriteLine($"User found in database: {user != null}");
                
                var isNewUser = user == null;
                var oauthContext = HttpContext.Session.GetString("OAuthContext");
                System.Console.WriteLine($"OAuth Context: {oauthContext}");
                
                if (user == null)
                {
                    System.Console.WriteLine("Creating new user...");
                    
                    // Create new user
                    user = new User
                    {
                        Name = !string.IsNullOrEmpty(name) ? name : "User",
                        Email = email,
                        PasswordHash = null, // OAuth users don't have passwords
                        Provider = provider,
                        ProviderId = providerId,
                        ProviderData = JsonSerializer.Serialize(new { 
                            Name = name,
                            Email = email,
                            ProviderId = providerId,
                            LoginTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        }),
                        CreatedAt = System.DateTime.Now,
                        LastLogin = System.DateTime.Now
                    };
                    
                    // Validate required fields before saving
                    if (string.IsNullOrEmpty(user.Name))
                    {
                        user.Name = "User";
                    }
                    if (string.IsNullOrEmpty(user.Email))
                    {
                        throw new Exception("Email is required but was null or empty");
                    }

                    System.Console.WriteLine($"New user object created: {user.Name}, {user.Email}, {user.Provider}");
                    
                    _context.Users.Add(user);
                    System.Console.WriteLine("User added to context");
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        System.Console.WriteLine("Changes saved to database successfully");
                    }
                    catch (Exception dbEx)
                    {
                        System.Console.WriteLine($"Database save error: {dbEx.Message}");
                        System.Console.WriteLine($"Inner exception: {dbEx.InnerException?.Message}");
                        System.Console.WriteLine($"Stack trace: {dbEx.StackTrace}");
                        throw; // Re-throw to be caught by outer catch block
                    }
                    
                    // Clear OAuth context
                    HttpContext.Session.Remove("OAuthContext");
                    
                    // Show success message for new user
                    TempData["SuccessMessage"] = $"Welcome to Nomsaur, {user.Name}! Your account has been created successfully.";
                    System.Console.WriteLine("New user created successfully");
                }
                else
                {
                    System.Console.WriteLine("User already exists, updating...");
                    
                    // Update existing user with OAuth info if not already set
                    if (string.IsNullOrEmpty(user.Provider))
                    {
                        user.Provider = provider;
                        user.ProviderId = providerId;
                        user.ProviderData = JsonSerializer.Serialize(new { 
                            Name = name,
                            Email = email,
                            ProviderId = providerId,
                            LoginTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }

                    user.LastLogin = System.DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    // Clear OAuth context
                    HttpContext.Session.Remove("OAuthContext");
                    
                    // Show login message for existing user
                    TempData["SuccessMessage"] = $"Welcome back, {user.Name}! You've been logged in successfully.";
                    System.Console.WriteLine("Existing user updated successfully");
                }

                // Set session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                System.Console.WriteLine($"Session set with UserId: {user.UserId}");

                // If user has no profile, redirect to preferences page
                if (user.UserProfile == null)
                {
                    System.Console.WriteLine("Redirecting to preferences page");
                    return RedirectToAction("Index", "Preferences");
                }

                // Otherwise, redirect to chat
                System.Console.WriteLine("Redirecting to chat page");
                return RedirectToAction("Chat", "Home");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"OAuth login error: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred during authentication. Please try again.";
                return RedirectToAction("Login");
            }
        }

        private async Task<bool> VerifyRecaptcha(string recaptchaResponse)
        {
            try
            {
                var secretKey = _configuration["Recaptcha:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    System.Console.WriteLine("reCAPTCHA secret key not configured");
                    return false;
                }

                using (var httpClient = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("secret", secretKey),
                        new KeyValuePair<string, string>("response", recaptchaResponse)
                    });

                    var response = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    System.Console.WriteLine($"reCAPTCHA verification response: {responseString}");
                    
                    var result = JObject.Parse(responseString);
                    var success = result["success"]?.Value<bool>() ?? false;
                    
                    return success;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error verifying reCAPTCHA: {ex.Message}");
                return false;
            }
        }
    }
} 