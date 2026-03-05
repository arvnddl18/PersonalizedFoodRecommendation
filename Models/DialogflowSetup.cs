using Google.Cloud.Dialogflow.V2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Google.Api.Gax;
using Google.Api.Gax.ResourceNames;

namespace Capstone.Models
{
    public class DialogflowSetup
    {
        private readonly string _projectId;
        private const int ENTITY_BATCH_SIZE = 100;
        private const int INTENT_BATCH_SIZE = 1000;

        public DialogflowSetup(string projectId)
        {
            _projectId = projectId;
        }

        public async Task SetupFoodRecommendationAsync()
        {
            try
            {
                var foodTypes = new Dictionary<string, List<string>>
                {
                    { "pizza", new List<string> { "pizza", "pizzas", "pepperoni pizza", "cheese pizza", "margherita", "hawaiian pizza", "calzone", "stromboli", "four cheese pizza", "meat lovers pizza", "supreme pizza", "white pizza", "deep dish pizza", "thin crust pizza", "stuffed crust pizza", "neapolitan pizza", "sicilian pizza", "detroit style pizza" } },
                    { "burger", new List<string> { "burger", "burgers", "cheeseburger", "hamburger", "veggie burger", "double burger", "sliders", "bacon burger", "mushroom burger", "turkey burger", "salmon burger", "black bean burger", "beyond burger", "impossible burger", "big mac", "whopper" } },
                    { "sushi", new List<string> { "sushi", "sushis", "maki", "nigiri", "california roll", "sashimi", "temaki", "dragon roll", "philadelphia roll", "rainbow roll", "spicy tuna roll", "salmon roll", "eel roll", "chirashi", "gunkan", "inari", "onigiri" } },
                    { "pasta", new List<string> { "pasta", "spaghetti", "fettuccine", "carbonara", "lasagna", "penne", "macaroni", "ravioli", "tortellini", "bolognese", "alfredo", "linguine", "rigatoni", "fusilli", "tagliatelle", "gnocchi", "cannelloni", "manicotti", "agnolotti", "orecchiette" } },
                    { "salad", new List<string> { "salad", "salads", "caesar salad", "greek salad", "garden salad", "cobb salad", "chef salad", "caprese salad", "waldorf salad", "nicoise salad", "spinach salad", "arugula salad", "kale salad", "quinoa salad", "potato salad", "coleslaw", "fruit salad" } },
                    { "steak", new List<string> { "steak", "beef steak", "ribeye", "sirloin", "t-bone", "filet mignon", "porterhouse", "flank steak", "new york strip", "top round", "bottom round", "chuck steak", "skirt steak", "hanger steak", "tenderloin", "prime rib" } },
                    { "chicken", new List<string> { "chicken", "fried chicken", "grilled chicken", "roast chicken", "chicken wings", "chicken nuggets", "chicken tenders", "chicken sandwich", "buffalo chicken", "chicken parmesan", "chicken teriyaki", "chicken tikka", "rotisserie chicken", "chicken thighs", "chicken breast", "popcorn chicken" } },
                    { "noodles", new List<string> { "noodles", "ramen", "udon", "soba", "lo mein", "chow mein", "pad thai", "pho", "glass noodles", "egg noodles", "rice noodles", "yakisoba", "dandan noodles", "beef noodle soup", "chicken noodle soup", "shirataki noodles" } },
                    { "tacos", new List<string> { "tacos", "taco", "burrito", "quesadilla", "enchilada", "fajita", "nachos", "taquitos", "fish tacos", "carnitas tacos", "al pastor tacos", "barbacoa tacos", "carne asada tacos", "chicken tacos", "street tacos", "soft tacos", "hard tacos" } },
                    { "seafood", new List<string> { "seafood", "shrimp", "crab", "lobster", "oysters", "clams", "mussels", "scallops", "prawns", "calamari", "octopus", "sea bass", "grouper", "snapper", "crab cakes", "lobster roll", "shrimp cocktail", "fried shrimp" } },
                    { "sandwich", new List<string> { "sandwich", "sandwiches", "sub", "hoagie", "panini", "club sandwich", "blt", "reuben", "grilled cheese", "philly cheesesteak", "monte cristo", "po' boy", "meatball sub", "italian sub", "turkey sandwich", "ham sandwich", "tuna sandwich" } },
                    { "soup", new List<string> { "soup", "soups", "chicken soup", "tomato soup", "clam chowder", "miso soup", "minestrone", "french onion soup", "vegetable soup", "beef stew", "chicken noodle soup", "split pea soup", "lentil soup", "mushroom soup", "gazpacho", "bisque", "broth" } },
                    { "rice", new List<string> { "rice", "fried rice", "steamed rice", "risotto", "paella", "pilaf", "biryani", "jambalaya", "sushi rice", "brown rice", "wild rice", "jasmine rice", "basmati rice", "sticky rice", "rice bowl", "congee", "arroz con pollo" } },
                    { "barbecue", new List<string> { "barbecue", "bbq", "grilled meat", "barbeque", "ribs", "pulled pork", "brisket", "barbecue chicken", "smoked meat", "grilled vegetables", "bbq sauce", "dry rub", "smoky flavor", "charcoal grilled" } },
                    { "fish", new List<string> { "fish", "grilled fish", "fried fish", "salmon", "tuna", "tilapia", "cod", "trout", "halibut", "mahi mahi", "swordfish", "catfish", "bass", "flounder", "sole", "mackerel", "sardines", "anchovies" } },
                    { "dessert", new List<string> { "dessert", "desserts", "cake", "ice cream", "pie", "pudding", "brownie", "cheesecake", "tiramisu", "mousse", "gelato", "sorbet", "cookies", "pastry", "tart", "eclair", "cannoli", "macarons", "fudge", "caramel", "chocolate" } },
                    { "breakfast", new List<string> { "breakfast", "pancakes", "waffles", "omelette", "scrambled eggs", "bacon", "sausage", "hash browns", "french toast", "cereal", "oatmeal", "granola", "yogurt", "toast", "bagel", "croissant", "breakfast burrito", "eggs benedict" } },
                    { "dim sum", new List<string> { "dim sum", "dumplings", "siomai", "bao", "spring rolls", "har gow", "xiao long bao", "shu mai", "char siu bao", "turnip cake", "sticky rice", "phoenix claws", "egg tarts", "sesame balls" } },
                    { "curry", new List<string> { "curry", "chicken curry", "beef curry", "vegetable curry", "thai curry", "indian curry", "massaman curry", "korma", "vindaloo", "tikka masala", "butter chicken", "dal", "paneer curry", "green curry", "red curry", "yellow curry" } },
                    { "shawarma", new List<string> { "shawarma", "gyro", "doner", "kebab", "souvlaki", "falafel wrap", "chicken shawarma", "lamb shawarma", "beef shawarma", "pita bread", "tzatziki", "hummus" } },
                    { "fries", new List<string> { "fries", "french fries", "potato wedges", "curly fries", "sweet potato fries", "chips", "shoestring fries", "steak fries", "waffle fries", "loaded fries", "cheese fries", "garlic fries" } },
                    { "hotdog", new List<string> { "hotdog", "hot dog", "corn dog", "chili dog", "bratwurst", "polish sausage", "foot long hot dog", "vienna sausage", "frankfurt" } },
                    { "vegetarian", new List<string> { "vegetarian", "vegan", "plant-based", "tofu", "tempeh", "seitan", "veggie burger", "quinoa", "lentils", "chickpeas", "black beans", "vegetables", "salad", "veggie wrap", "hummus", "avocado" } },
                    { "pancake", new List<string> { "pancake", "pancakes", "crepe", "blintz", "hotcake", "buttermilk pancakes", "blueberry pancakes", "chocolate chip pancakes", "banana pancakes", "protein pancakes" } },
                    { "donut", new List<string> { "donut", "doughnut", "cruller", "eclair", "long john", "bear claw", "glazed donut", "chocolate donut", "jelly donut", "boston cream", "old fashioned", "cake donut" } },
                    { "milk tea", new List<string> { "milk tea", "bubble tea", "boba", "pearl tea", "tapioca tea", "thai tea", "taro milk tea", "matcha milk tea", "brown sugar milk tea", "fruit tea" } },
                    { "coffee", new List<string> { "coffee", "latte", "espresso", "cappuccino", "americano", "mocha", "macchiato", "frappuccino", "cold brew", "iced coffee", "black coffee", "decaf", "cortado", "flat white" } },
                    { "tea", new List<string> { "tea", "green tea", "black tea", "herbal tea", "oolong tea", "chai", "earl grey", "jasmine tea", "chamomile tea", "peppermint tea", "white tea", "matcha", "iced tea" } },
                    { "falafel", new List<string> { "falafel", "falafels", "chickpea balls", "falafel wrap", "falafel plate" } },
                    { "gnocchi", new List<string> { "gnocchi", "potato gnocchi", "ricotta gnocchi", "spinach gnocchi" } },
                    { "bacon", new List<string> { "bacon", "crispy bacon", "turkey bacon", "canadian bacon", "pancetta", "back bacon" } },
                    { "sausage", new List<string> { "sausage", "sausages", "bratwurst", "chorizo", "italian sausage", "breakfast sausage", "kielbasa", "andouille", "merguez" } },
                    { "meatballs", new List<string> { "meatballs", "swedish meatballs", "italian meatballs", "turkey meatballs", "lamb meatballs", "pork meatballs" } },
                    { "ribs", new List<string> { "ribs", "pork ribs", "beef ribs", "baby back ribs", "spare ribs", "short ribs", "st louis ribs" } },
                    { "duck", new List<string> { "duck", "roast duck", "peking duck", "duck confit", "duck breast", "duck leg" } },
                    { "lamb", new List<string> { "lamb", "lamb chops", "roast lamb", "leg of lamb", "lamb shank", "rack of lamb", "ground lamb" } },
                    { "turkey", new List<string> { "turkey", "roast turkey", "turkey sandwich", "turkey burger", "smoked turkey", "turkey breast", "ground turkey" } },
                    { "egg", new List<string> { "egg", "eggs", "boiled egg", "poached egg", "fried egg", "scrambled egg", "deviled eggs", "egg salad", "quiche" } },
                    { "muffin", new List<string> { "muffin", "muffins", "blueberry muffin", "chocolate chip muffin", "bran muffin", "banana muffin", "lemon muffin" } },
                    { "bagel", new List<string> { "bagel", "bagels", "everything bagel", "sesame bagel", "plain bagel", "cinnamon raisin bagel", "poppy seed bagel" } },
                    { "wrap", new List<string> { "wrap", "wraps", "chicken wrap", "veggie wrap", "turkey wrap", "buffalo chicken wrap", "caesar wrap", "tuna wrap" } },
                    { "pizza roll", new List<string> { "pizza roll", "pizza rolls", "totino's pizza rolls" } },
                    { "spring roll", new List<string> { "spring roll", "spring rolls", "fresh spring rolls", "vietnamese spring rolls" } },
                    { "empanada", new List<string> { "empanada", "empanadas", "beef empanada", "chicken empanada", "cheese empanada" } },
                    { "tamale", new List<string> { "tamale", "tamales", "pork tamales", "chicken tamales", "cheese tamales" } },
                    { "samosa", new List<string> { "samosa", "samosas", "vegetable samosa", "meat samosa", "potato samosa" } },
                    { "pierogi", new List<string> { "pierogi", "pierogies", "potato pierogi", "cheese pierogi", "sauerkraut pierogi" } },
                    { "goulash", new List<string> { "goulash", "hungarian goulash", "beef goulash" } },
                    { "ceviche", new List<string> { "ceviche", "fish ceviche", "shrimp ceviche", "mixed ceviche" } },
                    { "paella", new List<string> { "paella", "seafood paella", "chicken paella", "vegetable paella", "mixed paella" } },
                    { "ratatouille", new List<string> { "ratatouille", "vegetable ratatouille" } },
                    { "bibimbap", new List<string> { "bibimbap", "korean rice bowl", "dolsot bibimbap" } },
                    { "kimchi", new List<string> { "kimchi", "fermented cabbage", "kimchi fried rice" } },
                    { "ramen", new List<string> { "ramen", "tonkotsu ramen", "miso ramen", "shoyu ramen", "shio ramen", "instant ramen" } },
                    { "pho", new List<string> { "pho", "beef pho", "chicken pho", "vegetable pho", "pho bo", "pho ga" } },
                    { "burrito", new List<string> { "burrito", "burritos", "bean burrito", "beef burrito", "chicken burrito", "breakfast burrito", "california burrito" } },
                    { "quesadilla", new List<string> { "quesadilla", "quesadillas", "cheese quesadilla", "chicken quesadilla", "steak quesadilla" } },
                    { "enchilada", new List<string> { "enchilada", "enchiladas", "chicken enchiladas", "cheese enchiladas", "beef enchiladas" } },
                    { "fajita", new List<string> { "fajita", "fajitas", "chicken fajitas", "beef fajitas", "shrimp fajitas", "vegetable fajitas" } },
                    { "nachos", new List<string> { "nachos", "loaded nachos", "cheese nachos", "beef nachos", "chicken nachos" } },
                    { "taquito", new List<string> { "taquito", "taquitos", "rolled tacos", "flautas" } },

                    // Additional categories
                    { "bread", new List<string> { "bread", "white bread", "wheat bread", "sourdough", "rye bread", "pumpernickel", "french bread", "italian bread", "pita bread", "naan", "focaccia", "ciabatta", "baguette", "dinner rolls" } },
                    { "cheese", new List<string> { "cheese", "cheddar", "mozzarella", "swiss", "provolone", "gouda", "brie", "camembert", "blue cheese", "feta", "parmesan", "ricotta", "cream cheese", "cottage cheese" } },
                    { "yogurt", new List<string> { "yogurt", "greek yogurt", "frozen yogurt", "vanilla yogurt", "strawberry yogurt", "plain yogurt", "probiotic yogurt" } },
                    { "cereal", new List<string> { "cereal", "cornflakes", "cheerios", "granola", "oatmeal", "muesli", "bran flakes", "rice krispies", "fruit loops" } },
                    { "smoothie", new List<string> { "smoothie", "fruit smoothie", "protein smoothie", "green smoothie", "berry smoothie", "mango smoothie", "banana smoothie" } },
                    { "juice", new List<string> { "juice", "orange juice", "apple juice", "grape juice", "cranberry juice", "pineapple juice", "tomato juice", "vegetable juice", "fresh juice" } },
                    { "wine", new List<string> { "wine", "red wine", "white wine", "rose wine", "sparkling wine", "champagne", "merlot", "cabernet", "chardonnay", "pinot noir" } },
                    { "beer", new List<string> { "beer", "lager", "ale", "stout", "pilsner", "ipa", "wheat beer", "craft beer", "light beer", "dark beer" } },
                    { "cocktail", new List<string> { "cocktail", "margarita", "mojito", "martini", "manhattan", "old fashioned", "whiskey sour", "cosmopolitan", "bloody mary", "mai tai" } },
                    { "fruit", new List<string> { "fruit", "apple", "banana", "orange", "grapes", "strawberry", "blueberry", "raspberry", "pineapple", "mango", "peach", "pear", "watermelon", "cantaloupe" } },
                    { "vegetables", new List<string> { "vegetables", "broccoli", "carrots", "spinach", "lettuce", "tomato", "cucumber", "bell pepper", "onion", "garlic", "potato", "sweet potato", "corn", "green beans" } },
                    { "nuts", new List<string> { "nuts", "almonds", "peanuts", "walnuts", "cashews", "pistachios", "pecans", "hazelnuts", "macadamia nuts", "brazil nuts" } },
                    { "snacks", new List<string> { "snacks", "chips", "crackers", "pretzels", "popcorn", "trail mix", "granola bars", "protein bars", "candy", "chocolate bars" } },
                    { "indian", new List<string> { "indian food", "naan", "chapati", "dal", "tandoori", "biryani", "samosa", "curry", "masala", "tikka", "vindaloo", "korma", "palak paneer" } },
                    { "chinese", new List<string> { "chinese food", "fried rice", "chow mein", "sweet and sour", "kung pao", "orange chicken", "beef and broccoli", "dim sum", "wonton", "egg rolls" } },
                    { "thai", new List<string> { "thai food", "pad thai", "green curry", "red curry", "tom yum", "pad see ew", "massaman curry", "thai basil", "mango sticky rice" } },
                    { "japanese", new List<string> { "japanese food", "sushi", "ramen", "tempura", "teriyaki", "miso soup", "udon", "soba", "yakitori", "katsu", "donburi" } },
                    { "korean", new List<string> { "korean food", "kimchi", "bulgogi", "bibimbap", "korean bbq", "galbi", "japchae", "tteokbokki", "korean fried chicken" } },
                    { "mediterranean", new List<string> { "mediterranean", "hummus", "falafel", "tabbouleh", "greek salad", "gyro", "pita", "olives", "tzatziki", "baklava" } },
                    { "mexican", new List<string> { "mexican food", "tacos", "burritos", "quesadillas", "enchiladas", "guacamole", "salsa", "chips and salsa", "mexican rice", "refried beans" } },
                    { "italian", new List<string> { "italian food", "pasta", "pizza", "risotto", "gelato", "tiramisu", "bruschetta", "antipasto", "osso buco", "carbonara" } },
                    { "french", new List<string> { "french food", "croissant", "baguette", "french fries", "crepe", "quiche", "coq au vin", "bouillabaisse", "ratatouille", "escargot" } }
                };

                // New entity types for dietary restrictions
                var dietaryRestrictions = new Dictionary<string, List<string>>
                {
                    { "vegetarian", new List<string> { "vegetarian", "veg", "no meat", "meatless", "plant-based" } },
                    { "vegan", new List<string> { "vegan", "strict vegetarian", "no animal products", "plant-based only" } },
                    { "gluten-free", new List<string> { "gluten-free", "no gluten", "celiac", "gluten sensitive" } },
                    { "dairy-free", new List<string> { "dairy-free", "no dairy", "lactose-free", "lactose intolerant" } },
                    { "nut-free", new List<string> { "nut-free", "no nuts", "peanut-free", "tree nut free" } },
                    { "halal", new List<string> { "halal", "islamic", "muslim-friendly" } },
                    { "kosher", new List<string> { "kosher", "jewish dietary", "jewish food" } },
                    { "keto", new List<string> { "keto", "ketogenic", "low carb", "high fat" } },
                    { "paleo", new List<string> { "paleo", "paleolithic", "caveman diet" } },
                    { "low-carb", new List<string> { "low-carb", "low carbohydrate", "reduced carbs" } },
                    { "low-fat", new List<string> { "low-fat", "low fat", "reduced fat" } },
                    { "low-sodium", new List<string> { "low-sodium", "low salt", "reduced sodium" } },
                    { "sugar-free", new List<string> { "sugar-free", "no sugar", "unsweetened" } }
                };

                // New entity types for location preferences
                var locationPreferences = new Dictionary<string, List<string>>
                {
                    { "nearby", new List<string> { "nearby", "close by", "around here", "in the area", "local", "near me" } },
                    { "downtown", new List<string> { "downtown", "city center", "central", "business district" } },
                    { "uptown", new List<string> { "uptown", "upper city", "north side" } },
                    { "suburbs", new List<string> { "suburbs", "suburban", "residential area", "outskirts" } },
                    { "shopping_mall", new List<string> { "shopping mall", "mall", "shopping center", "plaza" } },
                    { "airport", new List<string> { "airport", "near airport", "airport area" } },
                    { "beach", new List<string> { "beach", "beachfront", "coastal", "seaside" } },
                    { "park", new List<string> { "park", "near park", "park area" } },
                    { "university", new List<string> { "university", "campus", "college", "near university" } },
                    { "business_district", new List<string> { "business district", "commercial area", "office area" } }
                };

                // New entity types for price range
                var priceRanges = new Dictionary<string, List<string>>
                {
                    { "budget", new List<string> { "budget", "cheap", "affordable", "inexpensive", "low-cost", "economical" } },
                    { "moderate", new List<string> { "moderate", "mid-range", "average price", "reasonable", "standard" } },
                    { "upscale", new List<string> { "upscale", "expensive", "high-end", "luxury", "premium", "fine dining" } }
                };

                // Create all entity types
                await CreateFoodTypeEntityAsync(_projectId, foodTypes);
                await CreateEntityTypeAsync(_projectId, "DietaryRestriction", dietaryRestrictions);
                await CreateEntityTypeAsync(_projectId, "LocationPreference", locationPreferences);
                await CreateEntityTypeAsync(_projectId, "PriceRange", priceRanges);

                // Create intents with the new parameters
                await CreateFoodRecommendationIntentsAsync(_projectId, foodTypes, dietaryRestrictions, locationPreferences, priceRanges);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up food recommendation: {ex.Message}");
                throw;
            }
        }

        private async Task CreateFoodTypeEntityAsync(string projectId, Dictionary<string, List<string>> foodTypes)
        {
            var client = EntityTypesClient.Create();
            var parent = AgentName.FromProject(projectId);

            // First, check if the entity type already exists
            var existingEntityTypes = new List<EntityType>();
            await foreach (var existingType in client.ListEntityTypesAsync(parent))
            {
                existingEntityTypes.Add(existingType);
            }
            var foodTypeEntity = existingEntityTypes.FirstOrDefault(et => et.DisplayName == "FoodType");

            if (foodTypeEntity != null)
            {
                // Delete existing entity type if it exists
                await client.DeleteEntityTypeAsync(foodTypeEntity.Name);
            }

            // Create new entity type
            var entityType = new EntityType
            {
                DisplayName = "FoodType",
                Kind = EntityType.Types.Kind.Map
            };

            // Add entities in batches
            var foodEntries = foodTypes.ToList();
            for (int i = 0; i < foodEntries.Count; i += ENTITY_BATCH_SIZE)
            {
                var batch = foodEntries.Skip(i).Take(ENTITY_BATCH_SIZE);
                foreach (var food in batch)
                {
                    entityType.Entities.Add(new EntityType.Types.Entity
                    {
                        Value = food.Key,
                        Synonyms = { food.Value }
                    });
                }
            }

            try
            {
                await client.CreateEntityTypeAsync(parent, entityType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating entity type: {ex.Message}");
                throw;
            }
        }

        private async Task CreateEntityTypeAsync(string projectId, string displayName, Dictionary<string, List<string>> entities)
        {
            var client = EntityTypesClient.Create();
            var parent = AgentName.FromProject(projectId);

            // First, check if the entity type already exists
            var existingEntityTypes = new List<EntityType>();
            await foreach (var existingType in client.ListEntityTypesAsync(parent))
            {
                existingEntityTypes.Add(existingType);
            }
            var existingEntity = existingEntityTypes.FirstOrDefault(et => et.DisplayName == displayName);

            if (existingEntity != null)
            {
                // Delete existing entity type if it exists
                await client.DeleteEntityTypeAsync(existingEntity.Name);
            }

            // Create new entity type
            var entityType = new EntityType
            {
                DisplayName = displayName,
                Kind = EntityType.Types.Kind.Map
            };

            // Add entities in batches
            var entries = entities.ToList();
            for (int i = 0; i < entries.Count; i += ENTITY_BATCH_SIZE)
            {
                var batch = entries.Skip(i).Take(ENTITY_BATCH_SIZE);
                foreach (var entry in batch)
                {
                    entityType.Entities.Add(new EntityType.Types.Entity
                    {
                        Value = entry.Key,
                        Synonyms = { entry.Value }
                    });
                }
            }

            try
            {
                await client.CreateEntityTypeAsync(parent, entityType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating entity type {displayName}: {ex.Message}");
                throw;
            }
        }

        private async Task CreateFoodRecommendationIntentsAsync(
            string projectId, 
            Dictionary<string, List<string>> foodTypes,
            Dictionary<string, List<string>> dietaryRestrictions,
            Dictionary<string, List<string>> locationPreferences,
            Dictionary<string, List<string>> priceRanges)
        {
            var client = IntentsClient.Create();
            var parent = AgentName.FromProject(projectId);

            // First, check and delete existing intents
            var existingIntents = new List<Intent>();
            await foreach (var intent in client.ListIntentsAsync(parent))
            {
                existingIntents.Add(intent);
            }
            var foodIntents = existingIntents.Where(i => i.DisplayName.StartsWith("FoodRecommendation"));
            foreach (var intent in foodIntents)
            {
                await client.DeleteIntentAsync(intent.Name);
            }

            // Use a diverse set of natural language example training phrases (example mode)
            var examplePhrases = new List<(string text, List<(string value, string entityType, string alias, int start, int end)> annotations)>
            {
                ("I want to eat pizza", new List<(string, string, string, int, int)>{ ("pizza", "@FoodType", "food", 14, 19) }),
                ("Show me vegan burgers nearby", new List<(string, string, string, int, int)>{ ("vegan", "@DietaryRestriction", "dietary", 8, 13), ("burgers", "@FoodType", "food", 14, 21), ("nearby", "@LocationPreference", "location", 22, 28) }),
                ("Find cheap sushi downtown", new List<(string, string, string, int, int)>{ ("cheap", "@PriceRange", "price", 5, 10), ("sushi", "@FoodType", "food", 11, 16), ("downtown", "@LocationPreference", "location", 17, 25) }),
                ("Recommend me gluten-free pasta", new List<(string, string, string, int, int)>{ ("gluten-free", "@DietaryRestriction", "dietary", 13, 24), ("pasta", "@FoodType", "food", 25, 30) }),
                ("Where can I get vegetarian tacos?", new List<(string, string, string, int, int)>{ ("vegetarian", "@DietaryRestriction", "dietary", 16, 26), ("tacos", "@FoodType", "food", 27, 32) }),
                ("Any affordable ramen in the mall?", new List<(string, string, string, int, int)>{ ("affordable", "@PriceRange", "price", 4, 14), ("ramen", "@FoodType", "food", 15, 20), ("mall", "@LocationPreference", "location", 28, 32) }),
                ("Order halal chicken near the airport", new List<(string, string, string, int, int)>{ ("halal", "@DietaryRestriction", "dietary", 6, 11), ("chicken", "@FoodType", "food", 12, 19), ("airport", "@LocationPreference", "location", 30, 37) }),
                ("Suggest a place for vegan pizza downtown", new List<(string, string, string, int, int)>{ ("vegan", "@DietaryRestriction", "dietary", 20, 25), ("pizza", "@FoodType", "food", 26, 31), ("downtown", "@LocationPreference", "location", 32, 40) }),
                ("Where's the best sushi for lunch?", new List<(string, string, string, int, int)>{ ("sushi", "@FoodType", "food", 17, 22) }),
                ("Find me a gluten-free burger nearby", new List<(string, string, string, int, int)>{ ("gluten-free", "@DietaryRestriction", "dietary", 12, 23), ("burger", "@FoodType", "food", 24, 30), ("nearby", "@LocationPreference", "location", 31, 37) }),
                ("I want a cheap vegetarian sandwich", new List<(string, string, string, int, int)>{ ("cheap", "@PriceRange", "price", 9, 14), ("vegetarian", "@DietaryRestriction", "dietary", 15, 25), ("sandwich", "@FoodType", "food", 26, 34) }),
                ("Show me keto options in the city center", new List<(string, string, string, int, int)>{ ("keto", "@DietaryRestriction", "dietary", 8, 12), ("city center", "@LocationPreference", "location", 24, 35) }),
                ("Where can I get sushi near the university?", new List<(string, string, string, int, int)>{ ("sushi", "@FoodType", "food", 16, 21), ("university", "@LocationPreference", "location", 32, 41) }),
                ("Find upscale Italian food by the beach", new List<(string, string, string, int, int)>{ ("upscale", "@PriceRange", "price", 5, 12), ("Italian", "@FoodType", "food", 13, 20), ("beach", "@LocationPreference", "location", 32, 37) }),
                ("Order a vegan salad for delivery", new List<(string, string, string, int, int)>{ ("vegan", "@DietaryRestriction", "dietary", 9, 14), ("salad", "@FoodType", "food", 15, 20) })
            };

            var foodRecommendationIntent = new Intent
                    {
                DisplayName = "FoodRecommendation",
                        Parameters = {
                            new Intent.Types.Parameter
                            {
                                DisplayName = "food",
                                EntityTypeDisplayName = "@FoodType",
                                Mandatory = false
                    },
                    new Intent.Types.Parameter
                    {
                        DisplayName = "dietary",
                        EntityTypeDisplayName = "@DietaryRestriction",
                        Mandatory = false
                    },
                    new Intent.Types.Parameter
                    {
                        DisplayName = "location",
                        EntityTypeDisplayName = "@LocationPreference",
                        Mandatory = false
                    },
                    new Intent.Types.Parameter
                    {
                        DisplayName = "price",
                        EntityTypeDisplayName = "@PriceRange",
                                Mandatory = false
                            }
                        },
                        Messages = {
                            new Intent.Types.Message
                            {
                                Text = new Intent.Types.Message.Types.Text
                                {
                            Text_ = { 
                                "I'll help you find $food options that are $dietary in the $location area within your $price budget. Let me search for the best matches near you."
                            }
                        }
                    }
                }
            };

            // Add the example-mode training phrases
            foreach (var (text, annotations) in examplePhrases)
            {
                var parts = new List<Intent.Types.TrainingPhrase.Types.Part>();
                int lastIdx = 0;
                foreach (var (value, entityType, alias, start, end) in annotations.OrderBy(a => a.start))
                {
                    if (start > lastIdx)
                    {
                        parts.Add(new Intent.Types.TrainingPhrase.Types.Part { Text = text.Substring(lastIdx, start - lastIdx) });
                    }
                    parts.Add(new Intent.Types.TrainingPhrase.Types.Part
                    {
                        Text = value,
                        EntityType = entityType,
                        Alias = alias
                    });
                    lastIdx = end;
                }
                if (lastIdx < text.Length)
                {
                    parts.Add(new Intent.Types.TrainingPhrase.Types.Part { Text = text.Substring(lastIdx) });
                }
                foodRecommendationIntent.TrainingPhrases.Add(new Intent.Types.TrainingPhrase { Parts = { parts } });
            }

            try
            {
                await client.CreateIntentAsync(parent, foodRecommendationIntent);
                }
                catch (Exception ex)
                {
                Console.WriteLine($"Error creating intent: {ex.Message}");
                    throw;
            }
        }

        public async Task SetupPersonalizedFoodRecommendationAsync()
        {
            try
            {
                var client = IntentsClient.Create();
                var parent = AgentName.FromProject(_projectId);

                // First, check and delete existing personalized food recommendation intent
                var existingIntents = new List<Intent>();
                await foreach (var intent in client.ListIntentsAsync(parent))
                {
                    existingIntents.Add(intent);
                }
                
                var personalizedIntent = existingIntents.FirstOrDefault(i => i.DisplayName == "PersonalizedFoodRecommendation");
                if (personalizedIntent != null)
                {
                    await client.DeleteIntentAsync(personalizedIntent.Name);
                }

                // Create the personalized food recommendation intent
                var personalizedFoodRecommendationIntent = new Intent
                {
                    DisplayName = "PersonalizedFoodRecommendation",
                    // No specific parameters needed - this intent relies on user profile data
                    Messages = {
                        new Intent.Types.Message
                        {
                            Text = new Intent.Types.Message.Types.Text
                            {
                                Text_ = { 
                                    "Let me find personalized recommendations based on your preferences and dining history.",
                                    "I'll suggest something perfect for you based on what you usually enjoy.",
                                    "Based on your preferences, let me find the best options for you.",
                                    "I'll check your taste profile and find great recommendations.",
                                    "Let me discover something amazing that matches your style."
                                }
                            }
                        }
                    }
                };

                // Training phrases for personalized queries
                var personalizedTrainingPhrases = new List<string>
                {
                    // Basic requests
                    "Can you recommend something?",
                    "I'm hungry",
                    "What should I eat?",
                    "Give me some suggestions",
                    "I need food ideas",
                    "What do you recommend?",
                    "Suggest something for me",
                    "I want to eat something",
                    "Help me choose food",
                    "What's good to eat?",
                    
                    // Contextual requests
                    "I'm hungry, what do you suggest?",
                    "Can you recommend something good?",
                    "What should I eat today?",
                    "Give me some food options",
                    "I need a meal recommendation",
                    "What's recommended for me?",
                    "Suggest something tasty",
                    "I want to try something new",
                    "What would you recommend?",
                    "Help me pick something to eat",
                    
                    // Mood-based requests
                    "I don't know what I want",
                    "Surprise me with something",
                    "What's good right now?",
                    "I need inspiration",
                    "What should I have for lunch?",
                    "What should I have for dinner?",
                    "What should I have for breakfast?",
                    "I'm craving something",
                    "What sounds good?",
                    "I'm in the mood for food",
                    
                    // Decision help requests
                    "Help me decide what to eat",
                    "I can't decide on food",
                    "What would be perfect for me?",
                    "Show me my options",
                    "What fits my taste?",
                    "What do I usually like?",
                    "Based on my preferences",
                    "What matches my style?",
                    "Find something I'd enjoy",
                    "What's my usual type?",
                    
                    // General food exploration
                    "Food suggestions please",
                    "I want food recommendations",
                    "Show me food options",
                    "Food ideas for me",
                    "Meal suggestions",
                    "What's available for me?",
                    "Food choices please",
                    "Dining options for me",
                    "What can I eat?",
                    "Food recommendations"
                };

                // Add training phrases (no entity annotations needed for personalized queries)
                foreach (var phrase in personalizedTrainingPhrases)
                {
                    personalizedFoodRecommendationIntent.TrainingPhrases.Add(new Intent.Types.TrainingPhrase
                    {
                        Parts = {
                            new Intent.Types.TrainingPhrase.Types.Part { Text = phrase }
                        }
                    });
                }

                await client.CreateIntentAsync(parent, personalizedFoodRecommendationIntent);
                Console.WriteLine("Personalized Food Recommendation intent created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up personalized food recommendation intent: {ex.Message}");
                throw;
            }
        }

        public async Task SetupAllIntentsAsync()
        {
            try
            {
                Console.WriteLine("Setting up food recommendation entities and intents...");
                await SetupFoodRecommendationAsync();
                
                Console.WriteLine("Setting up personalized food recommendation intent...");
                await SetupPersonalizedFoodRecommendationAsync();
                
                Console.WriteLine("All Dialogflow setup completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during complete setup: {ex.Message}");
                throw;
            }
        }
    }
} 