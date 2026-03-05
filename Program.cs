using Capstone.Data;
using Capstone.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using static Capstone.Models.NomsaurModel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Increase model binding collection size to handle large food type lists (1154+ items)
    options.MaxModelBindingCollectionSize = 5000;
})
.AddJsonOptions(options =>
{
    // Prevent "A possible object cycle was detected" when returning EF entities that have navigation properties
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Real-time direct messaging
builder.Services.AddSignalR();

// Allow large forms (many checkboxes/hidden inputs)
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 100000; // default 1024; raise to handle many inputs
    options.KeyLengthLimit = int.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
});

// Configure DB Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

// Add memory caching for better performance with large datasets
builder.Services.AddMemoryCache();

// Add ScrapingBee service
builder.Services.AddHttpClient<ScrapingBeeService>();

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/auth/google-callback";
})
.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
    options.CallbackPath = "/auth/facebook-callback";
});


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Optional: export database to JSON for panelists (e.g. DVD submission)
if (args.Contains("--export-database"))
{
	var outputPath = args.SkipWhile(a => a != "--export-database").Skip(1).FirstOrDefault() ?? "DatabaseExport.json";
	using (var scope = app.Services.CreateScope())
	{
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		Capstone.Data.DatabaseExport.ExportToJson(db, outputPath);
	}
	Console.WriteLine($"Database exported to {Path.GetFullPath(outputPath)}");
	return;
}

// Apply database migrations and seed minimal data
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	db.Database.Migrate();

	// Seed baseline FoodTypes if empty to enable prescriptive recommendations
	if (!db.FoodTypes.Any())
	{
		var foodSeeds = new[]
		{
			// Italian Foods
			new { Name = "Pizza", Description = "Italian flatbread with cheese and toppings" },
			new { Name = "Pasta", Description = "Italian noodles with various sauces" },
			new { Name = "Lasagna", Description = "Layered Italian pasta dish with meat and cheese" },
			new { Name = "Risotto", Description = "Creamy Italian rice dish" },
			
			// Japanese Foods
			new { Name = "Sushi", Description = "Japanese raw fish with rice" },
			new { Name = "Ramen", Description = "Japanese noodle soup" },
			new { Name = "Tempura", Description = "Japanese battered and fried vegetables or seafood" },
			new { Name = "Teriyaki Chicken", Description = "Japanese grilled chicken with sweet sauce" },
			
			// American Foods
			new { Name = "Burger", Description = "American grilled meat patty in a bun" },
			new { Name = "BBQ Ribs", Description = "American barbecued pork ribs" },
			new { Name = "Mac and Cheese", Description = "American pasta with cheese sauce" },
			new { Name = "Fried Chicken", Description = "American crispy fried chicken" },
			
			// Mexican Foods
			new { Name = "Tacos", Description = "Mexican tortillas with meat and vegetables" },
			new { Name = "Burrito", Description = "Mexican wrapped tortilla with fillings" },
			new { Name = "Quesadilla", Description = "Mexican cheese-filled tortilla" },
			new { Name = "Nachos", Description = "Mexican tortilla chips with cheese and toppings" },
			
			// Indian Foods
			new { Name = "Curry", Description = "Indian spiced stew with vegetables or meat" },
			new { Name = "Biryani", Description = "Indian spiced rice dish with meat or vegetables" },
			new { Name = "Tandoori Chicken", Description = "Indian clay oven-baked spiced chicken" },
			new { Name = "Naan Bread", Description = "Indian flatbread" },
			
			// Chinese Foods
			new { Name = "Fried Rice", Description = "Chinese stir-fried rice with vegetables and meat" },
			new { Name = "Sweet and Sour Chicken", Description = "Chinese chicken in sweet and tangy sauce" },
			new { Name = "Dumplings", Description = "Chinese steamed or fried dough parcels with filling" },
			new { Name = "Chow Mein", Description = "Chinese stir-fried noodles" },
			
			// Healthy Options
			new { Name = "Grilled Salmon", Description = "Healthy grilled fish rich in omega-3" },
			new { Name = "Caesar Salad", Description = "Fresh lettuce with dressing and croutons" },
			new { Name = "Quinoa Bowl", Description = "Healthy grain bowl with vegetables" },
			new { Name = "Vegetable Stir Fry", Description = "Mixed vegetables cooked quickly in a wok" },
			
			// Popular Comfort Foods
			new { Name = "Fish and Chips", Description = "Battered fish with fried potatoes" },
			new { Name = "Chicken Tikka Masala", Description = "Indian-style chicken in creamy tomato sauce" },
			new { Name = "Pad Thai", Description = "Thai stir-fried noodles with tamarind sauce" },
			new { Name = "Pho", Description = "Vietnamese noodle soup with herbs" }
		};
		
		foreach (var seed in foodSeeds)
		{
			db.FoodTypes.Add(new FoodType { Name = seed.Name, Description = seed.Description });
		}
		db.SaveChanges();
	}

	// REMOVED: Establishment seeding - Using Google Maps API instead of local establishment data

	// REMOVED: Establishment-FoodType linking code

	// Seed DietaryRestrictions if empty
	if (!db.DietaryRestrictions.Any())
	{
		var dietaryRestrictions = new[]
		{
			new { Name = "Vegetarian", Description = "Does not eat meat" },
			new { Name = "Vegan", Description = "Does not eat any animal products" },
			new { Name = "Gluten-Free", Description = "Cannot consume gluten" },
			new { Name = "Dairy-Free", Description = "Cannot consume dairy products" },
			new { Name = "Nut-Free", Description = "Cannot consume nuts" },
			new { Name = "Shellfish-Free", Description = "Cannot consume shellfish" },
			new { Name = "Soy-Free", Description = "Cannot consume soy products" },
			new { Name = "Low-Sodium", Description = "Needs to limit sodium intake" },
			new { Name = "Low-Sugar", Description = "Needs to limit sugar intake" },
			new { Name = "Keto", Description = "Follows ketogenic diet" },
			new { Name = "Paleo", Description = "Follows paleolithic diet" },
			new { Name = "Halal", Description = "Follows Islamic dietary laws" },
			new { Name = "Kosher", Description = "Follows Jewish dietary laws" },
			new { Name = "Low-Carb", Description = "Limits carbohydrate intake" },
			new { Name = "Pescatarian", Description = "Eats fish but no other meat" }
		};

		foreach (var restriction in dietaryRestrictions)
		{
			db.DietaryRestrictions.Add(new DietaryRestriction 
			{ 
				Name = restriction.Name, 
				Description = restriction.Description 
			});
		}
		db.SaveChanges();
	}

	// Seed HealthConditions if empty
	if (!db.HealthConditions.Any())
	{
		var healthConditions = new[]
		{
			new { Name = "Diabetes", Description = "Blood sugar regulation issues" },
			new { Name = "Hypertension", Description = "High blood pressure" },
			new { Name = "Heart Disease", Description = "Cardiovascular conditions" },
			new { Name = "High Cholesterol", Description = "Elevated cholesterol levels" },
			new { Name = "Obesity", Description = "Excess body weight" },
			new { Name = "Kidney Disease", Description = "Kidney function issues" },
			new { Name = "Liver Disease", Description = "Liver function issues" },
			new { Name = "Gastrointestinal Issues", Description = "Digestive system problems" },
			new { Name = "Food Allergies", Description = "Allergic reactions to specific foods" },
			new { Name = "Acid Reflux", Description = "Stomach acid issues" },
			new { Name = "IBS", Description = "Irritable Bowel Syndrome" },
			new { Name = "Celiac Disease", Description = "Severe gluten intolerance" },
			new { Name = "Lactose Intolerance", Description = "Cannot digest lactose" },
			new { Name = "Gout", Description = "Joint inflammation from uric acid" },
			new { Name = "Anemia", Description = "Low iron levels" }
		};

		foreach (var condition in healthConditions)
		{
			db.HealthConditions.Add(new HealthCondition 
			{ 
				Name = condition.Name, 
				Description = condition.Description 
			});
		}
		db.SaveChanges();
	}

	// Seed sample user preference patterns for testing prescriptive recommendations
	if (!db.UserPreferencePatterns.Any())
	{
		var users = db.Users.Take(3).ToList(); // Get first 3 users if they exist
		if (users.Any())
		{
			var patterns = new List<UserPreferencePattern>();
			
			foreach (var user in users)
			{
				// Add cuisine preference pattern
				patterns.Add(new UserPreferencePattern
				{
					UserId = user.UserId,
					PatternType = "cuisine",
					PatternValue = "Italian",
					Confidence = 0.85m,
					LastObserved = DateTime.UtcNow,
					ObservationCount = 15
				});
				
				// Add time preference pattern
				patterns.Add(new UserPreferencePattern
				{
					UserId = user.UserId,
					PatternType = "time",
					PatternValue = "evening",
					Confidence = 0.75m,
					LastObserved = DateTime.UtcNow,
					ObservationCount = 12
				});
				
				// Add price preference pattern
				patterns.Add(new UserPreferencePattern
				{
					UserId = user.UserId,
					PatternType = "price",
					PatternValue = "Mid-range",
					Confidence = 0.80m,
					LastObserved = DateTime.UtcNow,
					ObservationCount = 18
				});
			}
			
			db.UserPreferencePatterns.AddRange(patterns);
			db.SaveChanges();
		}
	}
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapHub<Capstone.DirectMessageHub>("/hubs/directmessages");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
