using Google.Cloud.Dialogflow.V2;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Capstone.Data;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Models
{
    public class DialogflowService
    {
        private readonly SessionsClient _sessionsClient;
        private readonly string _projectId;
        private readonly string _sessionId;
        private readonly UserBehaviorService _behaviorService;
        private readonly AppDbContext _context;
        

        public DialogflowService(string projectId, string sessionId, AppDbContext context)
        {
            
            _projectId = projectId;
            _sessionId = sessionId;
            _sessionsClient = SessionsClient.Create();
            _context = context;
            _behaviorService = new UserBehaviorService(context);
        }

        public async Task<DialogflowResponse> SendMessageAsync(string message, int? userId = null, int? sessionId = null, bool includeHealthDietRestrictions = true)
        {
            var session = SessionName.FromProjectSession(_projectId, _sessionId);
            var queryInput = new QueryInput
            {
                Text = new TextInput
                {
                    Text = message,
                    LanguageCode = "en-US"
                }
            };

            var request = new DetectIntentRequest
            {
                SessionAsSessionName = session,
                QueryInput = queryInput
            };

            var swDetect = Stopwatch.StartNew();
            var response = await _sessionsClient.DetectIntentAsync(request);
            swDetect.Stop();
            Console.WriteLine($"[Perf] Dialogflow DetectIntentAsync: {swDetect.ElapsedMilliseconds} ms");
            var result = response?.QueryResult ?? new QueryResult();

            var intentName = result.Intent?.DisplayName?.ToLower() ?? string.Empty;
            string responseText = string.Empty;
            var parametersStruct = result?.Parameters ?? new Struct();
            string parametersJson = JsonSerializer.Serialize(parametersStruct);
            
            // Initialize multi-value collections at method level scope
            var extractedDietaryRestrictions = new List<string>();
            var extractedHealthConditions = new List<string>();
            
            // Extract establishment name from "Search for [establishment name]" pattern
            string? establishmentName = null;
            var messageText = message ?? string.Empty;
            var messageLower = messageText.ToLowerInvariant();
            
            // Pattern: "Search for [establishment name]" or "Find [establishment name]" or "Look for [establishment name]"
            if (System.Text.RegularExpressions.Regex.IsMatch(messageLower, @"\b(?:search\s+for|find|look\s+for|lookup|find\s+me)\s+"))
            {
                var establishmentPattern = System.Text.RegularExpressions.Regex.Match(
                    messageText,
                    @"\b(?:search\s+for|find|look\s+for|lookup|find\s+me)\s+(.+?)(?:\.|\?|!|,|;|:|$)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (establishmentPattern.Success && establishmentPattern.Groups.Count > 1)
                {
                    var extractedName = establishmentPattern.Groups[1].Value.Trim();
                    // Remove trailing punctuation
                    extractedName = System.Text.RegularExpressions.Regex.Replace(extractedName, @"[.!?,;:]$", "").Trim();
                    
                    // Remove common stop words
                    var stopWords = new[] { "restaurant", "place", "establishment", "the", "a", "an", "near", "me", "nearby" };
                    var words = extractedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var nameWords = words.Where(w => !stopWords.Any(sw => string.Equals(w, sw, StringComparison.OrdinalIgnoreCase))).ToArray();
                    
                    if (nameWords.Length > 0)
                    {
                        // Preserve original capitalization
                        establishmentName = string.Join(" ", nameWords.Select(w => 
                        {
                            if (string.IsNullOrEmpty(w)) return w;
                            // If already has capital letters, preserve it; otherwise capitalize first letter
                            if (char.IsUpper(w[0]) || w.Any(c => char.IsUpper(c)))
                                return w;
                            return w.Length > 1 ? char.ToUpper(w[0]) + w.Substring(1) : w.ToUpper();
                        }));
                    }
                }
            }
            
            // If establishment name is detected, handle it as a specific establishment search
            if (!string.IsNullOrWhiteSpace(establishmentName))
            {
                var templates = new List<string>
                {
                    $"Searching for {establishmentName}...",
                    $"Looking up {establishmentName} for you...",
                    $"Finding {establishmentName}...",
                    $"I'll search for {establishmentName} now...",
                    $"Let me find {establishmentName} for you...",
                    $"Searching for {establishmentName} nearby...",
                    $"I'll look for {establishmentName}...",
                    $"Finding {establishmentName} in your area..."
                };
                var random = new Random();
                responseText = templates[random.Next(templates.Count)];
                
                // Track user interaction
                if (userId.HasValue)
                {
                    var establishmentParamsJson = JsonSerializer.Serialize(new { 
                        establishmentName = establishmentName,
                        location = "nearby"
                    });
                    
                    await _behaviorService.TrackUserInteraction(
                        userId.Value,
                        "search",
                        message ?? string.Empty,
                        responseText,
                        "establishment_search",
                        establishmentParamsJson
                    );
                }
                
                return new DialogflowResponse 
                { 
                    ResponseText = responseText,
                    ExtractedParameters = new ExtractedParameters
                    {
                        EstablishmentName = establishmentName,
                        Location = "nearby",
                        IntentName = "establishment_search",
                        QueryType = "generalized"
                    }
                };
            }

            if (intentName.Contains("recommend") || intentName.Contains("personalizedfoodrecommendation"))
            {
                // Extract parameters
                var cuisine = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("cuisine") ? result.Parameters.Fields["cuisine"].StringValue : null;
                var food = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("food") ? result.Parameters.Fields["food"].StringValue : null;
                var location = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("location") ? result.Parameters.Fields["location"].StringValue : null;
                var price = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("price") ? result.Parameters.Fields["price"].StringValue : null;
                var dietary = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("dietary") ? result.Parameters.Fields["dietary"].StringValue : null;

                // Enrich with raw message hints for location/price
                var raw = (message ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(location) && (raw.Contains("near me") || raw.Contains("nearby") || raw.Contains("around here")))
                {
                    location = "nearby";
                }
                if (string.IsNullOrWhiteSpace(price) && (raw.Contains("cheap") || raw.Contains("budget") || raw.Contains("affordable")))
                {
                    price = "budget";
                }
                
                // Extract food from "I want to try [food]" or similar patterns when Dialogflow doesn't extract it
                if (string.IsNullOrWhiteSpace(food))
                {
                    // messageText already declared at method level
                    
                    // Pattern 1: "I want to try [food]" - captures everything after "try" until end or punctuation
                    var tryPattern = System.Text.RegularExpressions.Regex.Match(
                        messageText,
                        @"(?:I\s+(?:want|would\s+like|'d\s+like)\s+to\s+try|I\s+want\s+to\s+try)\s+(.+?)(?:\.|\?|!|,|;|:|$)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (tryPattern.Success && tryPattern.Groups.Count > 1)
                    {
                        var extractedFood = tryPattern.Groups[1].Value.Trim();
                        // Remove trailing punctuation
                        extractedFood = System.Text.RegularExpressions.Regex.Replace(extractedFood, @"[.!?,;:]$", "").Trim();
                        
                        // Remove common stop words, but preserve the food name
                        var stopWords = new[] { "restaurant", "place", "food", "dish", "meal", "something", "some", "the", "a", "an" };
                        var words = extractedFood.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var foodWords = words.Where(w => !stopWords.Any(sw => string.Equals(w, sw, StringComparison.OrdinalIgnoreCase))).ToArray();
                        
                        if (foodWords.Length > 0)
                        {
                            // Preserve original capitalization, or capitalize properly
                            food = string.Join(" ", foodWords.Select(w => 
                            {
                                if (string.IsNullOrEmpty(w)) return w;
                                // If already has capital letters, preserve it; otherwise capitalize first letter
                                if (char.IsUpper(w[0]) || w.Any(c => char.IsUpper(c)))
                                    return w;
                                return w.Length > 1 ? char.ToUpper(w[0]) + w.Substring(1) : w.ToUpper();
                            }));
                        }
                    }
                    
                    // Pattern 2: Fallback - "try [food]" or "want [food]" (simpler pattern)
                    if (string.IsNullOrWhiteSpace(food))
                    {
                        var simplePattern = System.Text.RegularExpressions.Regex.Match(
                            messageText,
                            @"\b(?:try|want|like|get|have|eat|order)\s+([A-Z][^\s.!?,;:]+(?:\s+[A-Z][^\s.!?,;:]+)*)",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (simplePattern.Success && simplePattern.Groups.Count > 1)
                        {
                            var extractedFood = simplePattern.Groups[1].Value.Trim();
                            // Skip common non-food words
                            var skipWords = new[] { "Restaurant", "Place", "Food", "Something", "Some", "That", "This", "It", "Here", "There", "The", "A", "An" };
                            if (!skipWords.Any(w => string.Equals(extractedFood, w, StringComparison.OrdinalIgnoreCase)) && extractedFood.Length > 1)
                            {
                                food = extractedFood;
                            }
                        }
                    }
                    
                    // Pattern 3: Extract any capitalized words/phrases that look like food names
                    if (string.IsNullOrWhiteSpace(food))
                    {
                        // Match capitalized words (potential food names) after common verbs
                        var foodNamePattern = System.Text.RegularExpressions.Regex.Match(
                            messageText,
                            @"\b(?:try|want|like|get|have|eat|order|find|search\s+for)\s+((?:[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*))",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (foodNamePattern.Success && foodNamePattern.Groups.Count > 1)
                        {
                            var extractedFood = foodNamePattern.Groups[1].Value.Trim();
                            var skipWords = new[] { "Restaurant", "Place", "Food", "Something", "Some", "That", "This", "It", "Here", "There", "The", "A", "An", "Near", "Me", "Nearby" };
                            var words = extractedFood.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            // Filter out skip words and take the remaining words as food name
                            var foodWords = words.Where(w => !skipWords.Any(sw => string.Equals(w, sw, StringComparison.OrdinalIgnoreCase))).ToArray();
                            if (foodWords.Length > 0)
                            {
                                food = string.Join(" ", foodWords);
                            }
                        }
                    }
                }

                // Enhanced dietary preference extraction from message
                // Extract multiple dietary restrictions and health conditions

                if (string.IsNullOrWhiteSpace(dietary))
                {
                    var dietaryKeywords = new Dictionary<string, string>
                    {
                        { "vegan", "vegan" },
                        { "vegetarian", "vegetarian" },
                        { "gluten-free", "gluten-free" },
                        { "gluten free", "gluten-free" },
                        { "dairy-free", "dairy-free" },
                        { "dairy free", "dairy-free" },
                        { "nut-free", "nut-free" },
                        { "nut free", "nut-free" },
                        { "halal", "halal" },
                        { "kosher", "kosher" },
                        { "low-carb", "low-carb" },
                        { "low carb", "low-carb" },
                        { "low-fat", "low-fat" },
                        { "low fat", "low-fat" },
                        { "low-sodium", "low-sodium" },
                        { "low sodium", "low-sodium" },
                        { "low-sugar", "low-sugar" },
                        { "low sugar", "low-sugar" },
                        { "keto", "ketogenic" },
                        { "ketogenic", "ketogenic" },
                        { "paleo", "paleo" }
                    };

                    // Find all matching dietary restrictions
                    foreach (var keyword in dietaryKeywords)
                    {
                        if (raw.Contains(keyword.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!extractedDietaryRestrictions.Contains(keyword.Value))
                            {
                                extractedDietaryRestrictions.Add(keyword.Value);
                            }
                        }
                    }

                    // Set primary dietary restriction for backward compatibility
                    if (extractedDietaryRestrictions.Any())
                    {
                        dietary = extractedDietaryRestrictions.First();
                    }
                }
                else
                {
                    extractedDietaryRestrictions.Add(dietary);
                }

                // Extract health conditions from the query
                var healthKeywords = new Dictionary<string, string>
                {
                    { "diabetes", "diabetes" },
                    { "diabetic", "diabetes" },
                    { "hypertension", "hypertension" },
                    { "high blood pressure", "hypertension" },
                    { "heart disease", "heart disease" },
                    { "heart condition", "heart disease" },
                    { "high cholesterol", "high cholesterol" },
                    { "cholesterol", "high cholesterol" },
                    { "obesity", "obesity" },
                    { "overweight", "obesity" },
                    { "weight management", "obesity" }
                };

                foreach (var healthKeyword in healthKeywords)
                {
                    if (raw.Contains(healthKeyword.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!extractedHealthConditions.Contains(healthKeyword.Value))
                        {
                            extractedHealthConditions.Add(healthKeyword.Value);
                        }
                    }
                }

                // DUAL FLOW DETECTION: Determine if this is a generalized or personalized query
                bool isGeneralizedQuery = IsGeneralizedQuery(food, cuisine, location, price, dietary);
                bool isPersonalizedQuery = IsPersonalizedQuery(message, food, cuisine, location, price, dietary, userId);
                
                if (isPersonalizedQuery && userId.HasValue)
                {
                    // For personalized queries, generate food preference rankings from DB patterns/preferences
                    var swRank = Stopwatch.StartNew();
                    var topFoodPreferences = await _behaviorService.GenerateFoodPreferenceRankings(userId.Value, message ?? string.Empty, null, null, 5, includeHealthDietRestrictions);
                    swRank.Stop();
                    Console.WriteLine($"[Perf] GenerateFoodPreferenceRankings: {swRank.ElapsedMilliseconds} ms");
                    if (topFoodPreferences != null && topFoodPreferences.Count > 0)
                    {
                        // Extract meal type from query for response enhancement
                        var mealType = ExtractMealTypeFromQuery(message ?? string.Empty);
                        
                        // Build personalized response with food preference rankings
                        var swBuild = Stopwatch.StartNew();
                        var personalizedResponse = await BuildPersonalizedFoodPreferenceResponse(userId.Value, topFoodPreferences, includeHealthDietRestrictions, mealType);
                        swBuild.Stop();
                        Console.WriteLine($"[Perf] BuildPersonalizedFoodPreferenceResponse: {swBuild.ElapsedMilliseconds} ms");
                        responseText = personalizedResponse;
                        
                        // Extract parameters from the personalized response text
                        var extractedParams = ExtractParametersFromResponse(personalizedResponse);
                        
                        // Create parameters JSON with extracted values from the response
                        var extractedParamsJson = JsonSerializer.Serialize(new { 
                            food = extractedParams.Food ?? string.Empty, 
                            cuisine = extractedParams.Cuisine ?? string.Empty, 
                            location = extractedParams.Location ?? string.Empty, 
                            price = extractedParams.Price ?? string.Empty, 
                            dietary = extractedParams.Dietary ?? string.Empty 
                        });
                        
                        // Track user interaction AFTER building the response
                        if (userId.HasValue)
                        {
                            var intentDetected = "personalized_food_recommendation"; // New intent for personalized queries
                            await _behaviorService.TrackUserInteraction(
                                userId.Value,
                                "search", // Use "search" action for consistency
                                message ?? string.Empty,
                                responseText,
                                intentDetected,
                                extractedParamsJson
                            );
                        }
                        
                        // For personalized queries, generate parameters from prescriptive recommender
                        var personalizedParams = await GeneratePersonalizedParameters(userId.Value);
                        
                        return new DialogflowResponse 
                        { 
                            ResponseText = responseText ?? string.Empty,
                            ExtractedParameters = new ExtractedParameters
                            {
                                Food = personalizedParams.Food ?? food,
                                Cuisine = personalizedParams.Cuisine ?? cuisine,
                                Location = personalizedParams.Location ?? location,
                                Price = personalizedParams.Price ?? price,
                                Dietary = personalizedParams.Dietary ?? dietary,
                                IntentName = intentName,
                                DietaryRestrictions = extractedDietaryRestrictions,
                                HealthConditions = extractedHealthConditions,
                                QueryType = "personalized"
                            }
                        };
                    }
                    else
                    {
                        // Fallback to generic personalized response if no recommendations found
                        var fallbackResponse = await GeneratePersonalizedResponse(userId.Value);
                        if (!string.IsNullOrEmpty(fallbackResponse))
                        {
                            responseText = fallbackResponse;
                            
                            // Extract parameters from the fallback response text
                            var extractedParams = ExtractParametersFromResponse(fallbackResponse);
                            
                            // Create parameters JSON with extracted values from the response
                            var extractedParamsJson = JsonSerializer.Serialize(new { 
                                food = extractedParams.Food ?? string.Empty, 
                                cuisine = extractedParams.Cuisine ?? string.Empty, 
                                location = extractedParams.Location ?? string.Empty, 
                                price = extractedParams.Price ?? string.Empty, 
                                dietary = extractedParams.Dietary ?? string.Empty 
                            });
                            
                            // Track user interaction AFTER building the response
                            if (userId.HasValue)
                            {
                                var intentDetected = "personalized_fallback"; // Fallback intent
                                await _behaviorService.TrackUserInteraction(
                                    userId.Value,
                                    "search", // Use "search" action for consistency
                                    message ?? string.Empty,
                                    responseText,
                                    intentDetected,
                                    extractedParamsJson
                                );
                            }
                            
                            return new DialogflowResponse 
                            { 
                                ResponseText = responseText ?? string.Empty,
                                ExtractedParameters = new ExtractedParameters
                                {
                                    Food = extractedParams.Food ?? string.Empty,
                                    Cuisine = extractedParams.Cuisine ?? string.Empty,
                                    Location = extractedParams.Location ?? string.Empty,
                                    Price = extractedParams.Price ?? string.Empty,
                                    Dietary = extractedParams.Dietary ?? string.Empty,
                                    IntentName = intentName
                                }
                            };
                        }
                    }
                }
                else if (isGeneralizedQuery)
                {
                    // GENERALIZED QUERY: Use Dialogflow parameters directly to search Google Maps
                    var searchFood = !string.IsNullOrWhiteSpace(food) ? food : "restaurants";
                    var cuisineSuffix = !string.IsNullOrWhiteSpace(cuisine) ? $" ({cuisine} cuisine)" : string.Empty;
                    var locationPhrase = string.Empty;
                    if (string.IsNullOrWhiteSpace(location) ||
                        string.Equals(location, "nearby", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(location, "near me", StringComparison.OrdinalIgnoreCase))
                    {
                        locationPhrase = "nearby";
                    }
                    else
                    {
                        locationPhrase = $"in {location}";
                    }

                    // Randomized natural-language templates to avoid repetition
                    var genTemplates = new List<string>
                    {
                        $"Looking for {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"Searching {locationPhrase} for {searchFood}{cuisineSuffix}...",
                        $"I'll find {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"On it — finding {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"Let me check places {locationPhrase} for {searchFood}{cuisineSuffix}...",
                        $"Exploring options {locationPhrase} for {searchFood}{cuisineSuffix}...",
                        $"I’m pulling up {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"One sec — searching for {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"I’ll look up {searchFood}{cuisineSuffix} {locationPhrase} now...",
                        $"Finding nearby spots for {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"Gathering {searchFood}{cuisineSuffix} options {locationPhrase}...",
                        $"Let me scout {locationPhrase} for the best {searchFood}{cuisineSuffix}...",
                        $"Checking Google Maps for {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"I’ll surface top-rated {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"Searching for places that serve {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"Looking up recommendations for {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"Finding great {searchFood}{cuisineSuffix} spots {locationPhrase}...",
                        $"Let me locate {searchFood}{cuisineSuffix} {locationPhrase} for you...",
                        $"I’m on the hunt for {searchFood}{cuisineSuffix} {locationPhrase}...",
                        $"Pulling recommendations for {searchFood}{cuisineSuffix} {locationPhrase}..."
                    };
                    var genRandom = new Random();
                    responseText = genTemplates[genRandom.Next(genTemplates.Count)];
                    
                    // Track generalized search
                    if (userId.HasValue)
                    {
                        var generalizedParamsJson = JsonSerializer.Serialize(new { 
                            food = food ?? string.Empty, 
                            cuisine = cuisine ?? string.Empty, 
                            location = location ?? string.Empty, 
                            price = price ?? string.Empty, 
                            dietary = dietary ?? string.Empty 
                        });
                        
                        await _behaviorService.TrackUserInteraction(
                            userId.Value,
                            "search",
                            message ?? string.Empty,
                            responseText,
                            "generalized_search",
                            generalizedParamsJson
                        );
                    }
                    
                    return new DialogflowResponse 
                    { 
                        ResponseText = responseText,
                        ExtractedParameters = new ExtractedParameters
                        {
                            Food = food,
                            Cuisine = cuisine,
                            Location = location,
                            Price = price,
                            Dietary = dietary,
                            IntentName = intentName,
                            DietaryRestrictions = extractedDietaryRestrictions,
                            HealthConditions = extractedHealthConditions,
                            QueryType = "generalized"
                        }
                    };
                }

                // If we have a logged-in user, generate prescriptive recommendations from DB patterns/preferences
                if (userId.HasValue)
                {
                    // Extract meal type from query if present
                    var mealType = ExtractMealTypeFromQuery(message ?? string.Empty);
                    
                    var top = await _behaviorService.GenerateFoodPreferenceRankings(userId.Value, message ?? string.Empty, null, null, 5, includeHealthDietRestrictions, mealType);
                    if (top != null && top.Count > 0)
                    {
                        // Decide format: general vs personalized
                        bool wantsExplicitList = message != null && System.Text.RegularExpressions.Regex.IsMatch(
                            message.ToLowerInvariant(),
                            @"\b(list|options|choices|show|top|recommendations|give me some|what are|suggest some)\b");
                        bool isPersonalizedRequest = !string.IsNullOrWhiteSpace(food) || !string.IsNullOrWhiteSpace(cuisine) || !string.IsNullOrWhiteSpace(dietary);

                        // Try to infer requested cuisine from provided inputs
                        string requestedCuisine = (cuisine ?? string.Empty).ToLowerInvariant();
                        if (string.IsNullOrEmpty(requestedCuisine) && !string.IsNullOrEmpty(food))
                        {
                            var f = food.ToLowerInvariant();
                            if (f.Contains("sushi") || f.Contains("ramen")) requestedCuisine = "japanese";
                            else if (f.Contains("pizza") || f.Contains("pasta")) requestedCuisine = "italian";
                            else if (f.Contains("burger")) requestedCuisine = "american";
                            else if (f.Contains("taco") || f.Contains("burrito") || f.Contains("quesadilla")) requestedCuisine = "mexican";
                            else if (f.Contains("curry")) requestedCuisine = "indian";
                        }

                        if (isPersonalizedRequest && !wantsExplicitList)
                        {
                            // Build label from requested food/dietary
                            var requestedLabelParts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(dietary)) requestedLabelParts.Add(dietary.Trim());
                            if (!string.IsNullOrWhiteSpace(food)) requestedLabelParts.Add(food.Trim());
                            var requestedLabel = requestedLabelParts.Count > 0 ? string.Join(" ", requestedLabelParts) : (requestedCuisine ?? "option");

                            // Prefer a candidate matching requested cuisine if present, else best by score
                            var best = top[0];
                            if (!string.IsNullOrEmpty(requestedCuisine))
                            {
                                var match = top.FirstOrDefault(r => r.FoodName.Equals(requestedCuisine, StringComparison.OrdinalIgnoreCase));
                                if (match != null) best = match;
                            }

                            // Compose natural "Looking for ..." template for personalized requests
                            var pfDetails = new List<string>();
                            if (!string.IsNullOrEmpty(food)) pfDetails.Add(food);
                            else if (!string.IsNullOrEmpty(cuisine)) pfDetails.Add(cuisine + " cuisine");
                            else if (!string.IsNullOrEmpty(dietary)) pfDetails.Add(dietary + " options");
                            string pfWhat = pfDetails.Count > 0 ? string.Join(" and ", pfDetails) : requestedLabel;
                            string pfWhereText = !string.IsNullOrEmpty(location) ? $" {location}" : " nearby";
                            string pfPriceTextOut = !string.IsNullOrEmpty(price) ? $" in the {price} price range" : "";

                            var pfTemplates = new List<string>
                            {
                                $"Looking for {pfWhat}{pfWhereText}{pfPriceTextOut}! I will find the best options for you.",
                                $"Searching for the best {pfWhat}{pfWhereText}{pfPriceTextOut}. Sit tight while I find some great places!",
                                $"Let me check for {pfWhat}{pfWhereText}{pfPriceTextOut}. I'll get back to you with some tasty options!",
                                $"On it! Finding {pfWhat}{pfWhereText}{pfPriceTextOut} just for you.",
                                $"I'll help you discover {pfWhat}{pfWhereText}{pfPriceTextOut}. Give me a moment!"
                            };
                            var pfRandom = new Random();
                            responseText = pfTemplates[pfRandom.Next(pfTemplates.Count)] ?? string.Empty;

                            await _behaviorService.TrackRecommendation(
                                userId.Value,
                                requestedLabel,
                                best.FoodName,
                                price ?? string.Empty,
                                "based on your request",
                                null
                            );
                        }
                        else
                        {
                            // List-style response for generic queries or explicit list requests
                            var lines = top.Take(5)
                                .Select((r, idx) => $"{idx + 1}. {r.FoodName} – {r.Reason}");
                            responseText = "Here are options based on your preferences and context:\n" + string.Join("\n", lines);

                            foreach (var r in top)
                            {
                                await _behaviorService.TrackRecommendation(
                                    userId.Value,
                                    r.FoodName,
                                    r.FoodName,
                                    price ?? string.Empty,
                                    r.Reason ?? string.Empty,
                                    null
                                );
                            }
                        }

                        // Track user interaction as a search with extracted parameters
                        parametersJson = JsonSerializer.Serialize(new { food, cuisine, location, price, dietary });
                        var intentDetected = (result != null && result.Intent != null && !string.IsNullOrEmpty(result.Intent.DisplayName)) ? result.Intent.DisplayName : "unknown";
                        await _behaviorService.TrackUserInteraction(
                            userId.Value,
                            "search",
                            message ?? string.Empty,
                            responseText ?? string.Empty,
                            intentDetected,
                            parametersJson
                        );

                        // Add intelligent review prompt if user has recently visited any of the recommended establishments
                        if (top.Any())
                        {
                            var topEstablishment = top.First();
                            var reviewPrompt = await GenerateReviewPrompt(userId.Value, topEstablishment.FoodName);
                            if (!string.IsNullOrEmpty(reviewPrompt))
                            {
                                responseText += reviewPrompt;
                            }
                        }

                        return new DialogflowResponse 
                        { 
                            ResponseText = responseText ?? string.Empty,
                            ExtractedParameters = new ExtractedParameters
                            {
                                Food = food,
                                Cuisine = cuisine,
                                Location = location,
                                Price = price,
                                Dietary = dietary,
                                IntentName = intentName
                            }
                        };
                    }
                }

                // Build a more natural, user-friendly fallback response
                var details = new List<string>();
                if (!string.IsNullOrEmpty(food)) details.Add(food);
                if (!string.IsNullOrEmpty(cuisine)) details.Add(cuisine + " cuisine");
                if (!string.IsNullOrEmpty(dietary)) details.Add(dietary + " options");
                string what = details.Count > 0 ? string.Join(" and ", details) : "restaurants";

                string where = !string.IsNullOrEmpty(location) ? $" {location}" : " nearby";
                string priceText = !string.IsNullOrEmpty(price) ? $" in the {price} price range" : "";

                var templates = new List<string>
                {
                    $"Looking for {what}{where}{priceText}! I will find the best options for you.",
                    $"Searching for the best {what}{where}{priceText}. Sit tight while I find some great places!",
                    $"Let me check for {what}{where}{priceText}. I'll get back to you with some tasty options!",
                    $"On it! Finding {what}{where}{priceText} just for you.",
                    $"I'll help you discover {what}{where}{priceText}. Give me a moment!"
                };
                var random = new Random();
                responseText = templates[random.Next(templates.Count)] ?? string.Empty;

                if (string.IsNullOrEmpty(food) && string.IsNullOrEmpty(cuisine))
                {
                    responseText = "Could you tell me what kind of food or cuisine you're interested in?";
                }

                parametersJson = JsonSerializer.Serialize(new { food, cuisine, location, price, dietary });
                
                if (userId.HasValue)
                {
                    var intentDetected = (result != null && result.Intent != null && !string.IsNullOrEmpty(result.Intent.DisplayName)) ? result.Intent.DisplayName : "unknown";
                    await _behaviorService.TrackUserInteraction(
                        userId.Value,
                        "search",
                        message ?? string.Empty,
                        responseText,
                        intentDetected,
                        parametersJson
                    );
                    if (sessionId.HasValue)
                    {
                        await _context.ChatMessages.AddAsync(new NomsaurModel.ChatMessage
                        {
                            SessionId = sessionId.Value,
                            Sender = "ai",
                            MessageText = responseText,
                            Timestamp = DateTime.UtcNow,
                            IntentDetected = intentDetected,
                            ParametersJson = parametersJson
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                return new DialogflowResponse 
                { 
                    ResponseText = responseText ?? string.Empty,
                    ExtractedParameters = new ExtractedParameters
                    {
                        Food = food,
                        Cuisine = cuisine,
                        Location = location,
                        Price = price,
                        Dietary = dietary,
                        IntentName = intentName,
                        DietaryRestrictions = extractedDietaryRestrictions,
                        HealthConditions = extractedHealthConditions,
                        QueryType = "legacy" // For legacy flows
                    }
                };
            }
            else
            {
                responseText = result?.FulfillmentText ?? string.Empty;
                // If no fulfillment text, try to get it from fulfillment messages
                if (string.IsNullOrEmpty(responseText))
                {
                    var fulfillmentMessages = result?.FulfillmentMessages;
                    if (fulfillmentMessages != null && fulfillmentMessages.Count > 0)
                    {
                        var fulfillmentMessage = fulfillmentMessages.FirstOrDefault();
                        if (fulfillmentMessage?.Text?.Text_ != null && fulfillmentMessage.Text.Text_.Count > 0)
                        {
                            responseText = fulfillmentMessage.Text.Text_.FirstOrDefault() ?? string.Empty;
                        }
                    }
                }
            }

            // Extract parameters from the result for the final return
            var finalFood = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("food") ? result.Parameters.Fields["food"].StringValue : null;
            var finalCuisine = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("cuisine") ? result.Parameters.Fields["cuisine"].StringValue : null;
            var finalLocation = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("location") ? result.Parameters.Fields["location"].StringValue : null;
            var finalPrice = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("price") ? result.Parameters.Fields["price"].StringValue : null;
            var finalDietary = result?.Parameters?.Fields != null && result.Parameters.Fields.ContainsKey("dietary") ? result.Parameters.Fields["dietary"].StringValue : null;
            
            return new DialogflowResponse 
            { 
                ResponseText = responseText ?? string.Empty,
                ExtractedParameters = new ExtractedParameters
                {
                    Food = finalFood,
                    Cuisine = finalCuisine,
                    Location = finalLocation,
                    Price = finalPrice,
                    Dietary = finalDietary,
                    IntentName = intentName,
                    DietaryRestrictions = extractedDietaryRestrictions,
                    HealthConditions = extractedHealthConditions,
                    QueryType = "standard" // Default for non-recommendation intents
                }
            };
        }

        // New method to generate personalized responses based on user preferences
        private async Task<string?> GeneratePersonalizedResponse(int userId)
        {
            try
            {
                // Get user profile
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userProfile == null)
                {
                    return null; // No profile found, fall back to generic response
                }

                // Get user's food preferences
                var foodPreferences = await _context.UserFoodTypes
                    .Include(uft => uft.FoodType)
                    .Where(uft => uft.UserId == userId && uft.PreferenceLevel == "Preferred")
                    .OrderByDescending(fp => fp.LastSelected)
                    .Take(3)
                    .ToListAsync();

                // Get learned patterns
                var patterns = await _context.UserPreferencePatterns
                    .Where(upp => upp.UserId == userId && upp.Confidence >= 0.3m)
                    .OrderByDescending(upp => upp.Confidence)
                    .ToListAsync();

                var responseParts = new List<string>();

                // REMOVED: Old single DietaryPreference and FavoriteFood fields
                // These are now handled by the multi-value system and behavioral analysis

                // Add preferred cuisine from patterns
                var cuisinePattern = patterns.FirstOrDefault(p => p.PatternType == "cuisine");
                if (cuisinePattern != null)
                {
                    responseParts.Add($"I've noticed you often enjoy {cuisinePattern.PatternValue} cuisine");
                }

                // Add food preferences from database
                if (foodPreferences.Any())
                {
                    var foodTypes = string.Join(", ", foodPreferences.Select(fp => fp.FoodType.Name));
                    responseParts.Add($"and you've shown interest in {foodTypes}");
                }

                // Add price preference
                var pricePattern = patterns.FirstOrDefault(p => p.PatternType == "price");
                if (pricePattern != null)
                {
                    responseParts.Add($"preferring {pricePattern.PatternValue} options");
                }

                // Build personalized response
                if (responseParts.Any())
                {
                    var personalizedContext = string.Join(" ", responseParts);
                    
                    // Multiple personalized response templates
                    var templates = new List<string>
                    {
                        $"{personalizedContext}. Let me find some great recommendations for you!",
                        $"{personalizedContext}. I'll search for the perfect places that match your preferences.",
                        $"{personalizedContext}. Sit tight while I find some amazing options just for you!",
                        $"{personalizedContext}. I'll discover some fantastic places that suit your taste.",
                        $"{personalizedContext}. Let me check for the best matches based on what you love!"
                    };
                    
                    var random = new Random();
                    return templates[random.Next(templates.Count)];
                }

                return null;
            }
            catch (Exception)
            {
                // If any error occurs, return null to fall back to generic response
                return null;
            }
        }

        // Method to get user behavior insights
        public async Task<Dictionary<string, object>> GetUserInsights(int userId)
        {
            return await _behaviorService.GetUserInsights(userId);
        }

        // Method to mark recommendation as selected
        public async Task MarkRecommendationSelected(int userId, string establishmentName, int? rating = null, string? feedback = null)
        {
            await _behaviorService.MarkRecommendationSelected(userId, establishmentName, rating, feedback);
        }

        // Method to track recommendation
        public async Task TrackRecommendation(int userId, string establishmentName, string cuisineType, 
            string priceRange, string reason, string? establishmentInfo = null)
        {
            await _behaviorService.TrackRecommendation(userId, establishmentName, cuisineType, priceRange, reason, establishmentInfo);
        }

        // DEPRECATED: Use UserBehaviorService.GenerateFoodPreferenceRankings() directly
        [Obsolete("This method is deprecated. Use UserBehaviorService.GenerateFoodPreferenceRankings() directly for food preference ranking.")]
        public async Task<Dictionary<string, object>> GeneratePersonalizedRecommendations(int userId, string query, 
            decimal? latitude = null, decimal? longitude = null)
        {
            return await _behaviorService.GeneratePersonalizedRecommendations(userId, query, latitude, longitude);
        }

        // New method to generate personalized parameters from user patterns
        private async Task<ExtractedParameters> GeneratePersonalizedParameters(int userId)
        {
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId);
            var userPatterns = await _context.UserPreferencePatterns
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var parameters = new ExtractedParameters();

            // Extract cuisine preference from patterns
            var cuisinePattern = userPatterns.FirstOrDefault(p => p.PatternType == "cuisine");
            if (cuisinePattern != null && cuisinePattern.Confidence > 0.5m)
            {
                parameters.Cuisine = cuisinePattern.PatternValue;
            }

            // Extract price preference from patterns
            var pricePattern = userPatterns.FirstOrDefault(p => p.PatternType == "price");
            if (pricePattern != null && pricePattern.Confidence > 0.5m)
            {
                parameters.Price = pricePattern.PatternValue;
            }

            // Extract primary dietary restriction from user profile (replacing old single DietaryPreference)
            var userDietaryRestrictions = await _context.UserDietaryRestrictions
                .Include(udr => udr.DietaryRestriction)
                .Where(udr => udr.UserId == userId)
                .OrderByDescending(udr => udr.ImportanceLevel)
                .FirstOrDefaultAsync();
            
            if (userDietaryRestrictions != null)
            {
                parameters.Dietary = userDietaryRestrictions.DietaryRestriction.Name;
            }

            // Set location to "nearby" for personalized queries
            parameters.Location = "nearby";

            return parameters;
        }

        // Method to intelligently ask for reviews only when appropriate
        private async Task<string> GenerateReviewPrompt(int userId, string establishmentName)
        {
            try
            {
                // Check if user has recently visited this establishment
                var recentVisit = await _context.UserBehaviors
                    .Where(ub => ub.UserId == userId && 
                                ub.EstablishmentName == establishmentName &&
                                ub.Action == "recommendation" &&
                                ub.SelectedAt.HasValue)
                    .OrderByDescending(ub => ub.SelectedAt)
                    .FirstOrDefaultAsync();

                if (recentVisit != null)
                {
                    // Check if it's been a reasonable time since the visit (within last 24 hours)
                    var timeSinceVisit = DateTime.UtcNow - (recentVisit.SelectedAt ?? DateTime.UtcNow);
                    if (timeSinceVisit.TotalHours <= 24)
                    {
                        // Check if user hasn't already given a review
                        if (recentVisit.Satisfaction == null && string.IsNullOrEmpty(recentVisit.Result))
                        {
                            return $"\n\nHow was your experience at {establishmentName}? I'd love to hear your review to help improve future recommendations!";
                        }
                    }
                }

                return string.Empty; // No review prompt needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating review prompt: {ex.Message}");
                return string.Empty;
            }
        }

        // DEPRECATED: Use BuildPersonalizedFoodPreferenceResponse() instead
        [Obsolete("This method builds responses for establishment recommendations. Use BuildPersonalizedFoodPreferenceResponse() for food preference ranking.")]
        private async Task<string> BuildPersonalizedRecommendationResponse(int userId, List<UserBehaviorService.PrescriptiveRecommendationResult> recommendations)
        {
            var responseParts = new List<string>();
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId);

            // REMOVED: Old single DietaryPreference and FavoriteFood fields
            // These are now handled by the multi-value system and behavioral analysis
            if (userProfile != null)
            {
                // Could add logic here to include dietary restrictions from the new system if needed
            }

            // Get learned patterns for cuisine and price
            var patterns = await _context.UserPreferencePatterns
                .Where(upp => upp.UserId == userId && upp.Confidence >= 0.3m)
                .OrderByDescending(upp => upp.Confidence)
                .ToListAsync();

            var cuisinePattern = patterns.FirstOrDefault(p => p.PatternType == "cuisine");
            if (cuisinePattern != null)
            {
                responseParts.Add($"I've noticed you often enjoy {cuisinePattern.PatternValue} cuisine");
            }

            var foodPreferences = await _context.UserFoodTypes
                .Include(uft => uft.FoodType)
                .Where(uft => uft.UserId == userId && uft.PreferenceLevel == "Preferred")
                .OrderByDescending(fp => fp.LastSelected)
                .Take(3)
                .ToListAsync();

            if (foodPreferences.Any())
            {
                var foodTypes = string.Join(", ", foodPreferences.Select(fp => fp.FoodType.Name));
                responseParts.Add($"and you've shown interest in {foodTypes}");
            }

            var pricePattern = patterns.FirstOrDefault(p => p.PatternType == "price");
            if (pricePattern != null)
            {
                responseParts.Add($"preferring {pricePattern.PatternValue} options");
            }

            // Add top recommendation
            if (recommendations.Any())
            {
                var topRecommendation = recommendations.First();
                responseParts.Add($"Based on your preferences, I recommend {topRecommendation.CandidateName} (Score: {topRecommendation.Score:F2})");
                responseParts.Add($"Reason: {topRecommendation.Reason}");
            }

            var personalizedContext = string.Join(" ", responseParts);
            var templates = new List<string>
            {
                $"{personalizedContext}. I've found some great recommendations for you!",
                $"{personalizedContext}. I'll search for the perfect places that match your preferences.",
                $"{personalizedContext}. Sit tight while I find some amazing options just for you!",
                $"{personalizedContext}. I'll discover some fantastic places that suit your taste.",
                $"{personalizedContext}. Let me check for the best matches based on what you love!"
            };
            var random = new Random();
            return templates[random.Next(templates.Count)];
        }

        // New method to build personalized food preference response with organized structure
        private async Task<string> BuildPersonalizedFoodPreferenceResponse(int userId, List<NomsaurModel.FoodPreferenceScore> foodPreferences, bool includeHealthDietRestrictions = true, string? detectedMealType = null)
        {
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId);
            
            // Get user dietary restrictions and health conditions only if health/diet mode is enabled
            List<UserDietaryRestriction> userDietaryRestrictions = new List<UserDietaryRestriction>();
            List<UserHealthCondition> userHealthConditions = new List<UserHealthCondition>();
            
            if (includeHealthDietRestrictions)
            {
                userDietaryRestrictions = await _context.UserDietaryRestrictions
                    .Include(udr => udr.DietaryRestriction)
                    .Where(udr => udr.UserId == userId)
                    .OrderByDescending(udr => udr.ImportanceLevel)
                    .ToListAsync();
                    
                userHealthConditions = await _context.UserHealthConditions
                    .Include(uhc => uhc.HealthCondition)
                    .Where(uhc => uhc.UserId == userId)
                    .OrderByDescending(uhc => uhc.SeverityLevel)
                    .ToListAsync();
            }

            // Get learned patterns for cuisine and price
            var patterns = await _context.UserPreferencePatterns
                .Where(upp => upp.UserId == userId && upp.Confidence >= 0.3m)
                .OrderByDescending(upp => upp.Confidence)
                .ToListAsync();

            // Build introduction section with much more variety
            var introductionParts = new List<string>();
            
            // Add meal type context if detected
            if (!string.IsNullOrEmpty(detectedMealType))
            {
                var mealTypeTemplates = new List<string>
                {
                    $"I see you're looking for {detectedMealType} options",
                    $"I understand you want {detectedMealType} recommendations",
                    $"I know you're interested in {detectedMealType} suggestions",
                    $"I see you're seeking {detectedMealType} choices",
                    $"I understand you're looking for {detectedMealType} ideas",
                    $"I know you want {detectedMealType} recommendations",
                    $"I see you're after {detectedMealType} options",
                    $"I understand you're searching for {detectedMealType} suggestions"
                };
                var random = new Random();
                introductionParts.Add(mealTypeTemplates[random.Next(mealTypeTemplates.Count)]);
            }
            
            if (userProfile != null)
            {
                if (includeHealthDietRestrictions)
                {
                    if (userDietaryRestrictions.Any())
                    {
                        var restrictions = userDietaryRestrictions.Select(dr => dr.DietaryRestriction.Name);
                        var dietaryTemplates = new List<string>
                        {
                            $"I know you follow {string.Join(", ", restrictions)} dietary requirements",
                            $"I see you're following {string.Join(", ", restrictions)} dietary guidelines",
                            $"I understand you maintain {string.Join(", ", restrictions)} dietary restrictions",
                            $"I'm aware of your {string.Join(", ", restrictions)} dietary needs",
                            $"I know you're committed to {string.Join(", ", restrictions)} dietary choices",
                            $"I see you prioritize {string.Join(", ", restrictions)} dietary preferences",
                            $"I understand you follow {string.Join(", ", restrictions)} dietary patterns",
                            $"I know you stick to {string.Join(", ", restrictions)} dietary requirements"
                        };
                        var random = new Random();
                        introductionParts.Add(dietaryTemplates[random.Next(dietaryTemplates.Count)]);
                    }

                    if (userHealthConditions.Any())
                    {
                        var conditions = userHealthConditions.Select(hc => hc.HealthCondition.Name);
                        var healthTemplates = new List<string>
                        {
                            $"and I'm considering your {string.Join(", ", conditions)} health needs",
                            $"while keeping your {string.Join(", ", conditions)} health conditions in mind",
                            $"and I'm mindful of your {string.Join(", ", conditions)} health requirements",
                            $"while respecting your {string.Join(", ", conditions)} health considerations",
                            $"and I'm factoring in your {string.Join(", ", conditions)} health needs",
                            $"while being considerate of your {string.Join(", ", conditions)} health status",
                            $"and I'm taking into account your {string.Join(", ", conditions)} health conditions",
                            $"while prioritizing your {string.Join(", ", conditions)} health requirements"
                        };
                        var random = new Random();
                        introductionParts.Add(healthTemplates[random.Next(healthTemplates.Count)]);
                    }
                }
                else
                {
                    var generalTemplates = new List<string>
                    {
                        "I'm focusing on your general food preferences",
                        "I'm considering your overall food tastes",
                        "I'm looking at your general culinary preferences",
                        "I'm focusing on your personal food choices",
                        "I'm considering your individual food preferences",
                        "I'm looking at your unique food tastes",
                        "I'm focusing on your personal culinary style",
                        "I'm considering your individual food preferences"
                    };
                    var random = new Random();
                    introductionParts.Add(generalTemplates[random.Next(generalTemplates.Count)]);
                }
            }

            var cuisinePattern = patterns.FirstOrDefault(p => p.PatternType == "cuisine");
            if (cuisinePattern != null)
            {
                var cuisineTemplates = new List<string>
                {
                    $"I've noticed you often enjoy {cuisinePattern.PatternValue} cuisine",
                    $"I see you have a preference for {cuisinePattern.PatternValue} cuisine",
                    $"I know you're fond of {cuisinePattern.PatternValue} cuisine",
                    $"I've observed your love for {cuisinePattern.PatternValue} cuisine",
                    $"I can see you appreciate {cuisinePattern.PatternValue} cuisine",
                    $"I've noticed your taste for {cuisinePattern.PatternValue} cuisine",
                    $"I see you gravitate toward {cuisinePattern.PatternValue} cuisine",
                    $"I know you enjoy {cuisinePattern.PatternValue} cuisine"
                };
                var random = new Random();
                introductionParts.Add(cuisineTemplates[random.Next(cuisineTemplates.Count)]);
            }

            var pricePattern = patterns.FirstOrDefault(p => p.PatternType == "price");
            if (pricePattern != null)
            {
                var priceTemplates = new List<string>
                {
                    $"preferring {pricePattern.PatternValue} options",
                    $"with a preference for {pricePattern.PatternValue} choices",
                    $"leaning toward {pricePattern.PatternValue} selections",
                    $"favoring {pricePattern.PatternValue} options",
                    $"with a taste for {pricePattern.PatternValue} choices",
                    $"preferring {pricePattern.PatternValue} selections",
                    $"leaning toward {pricePattern.PatternValue} options",
                    $"with a preference for {pricePattern.PatternValue} choices"
                };
                var random = new Random();
                introductionParts.Add(priceTemplates[random.Next(priceTemplates.Count)]);
            }

            // Build options section - ensure at least 5 options with variety
            var optionsSection = "";
            if (foodPreferences.Any())
            {
                var topFoods = foodPreferences.Take(5).ToList(); // Always show exactly 5 foods
                
                // Create numbered list format for JavaScript to parse
                var numberedFoodList = new List<string>();
                for (int i = 0; i < topFoods.Count; i++)
                {
                    numberedFoodList.Add($"{i + 1}. {topFoods[i].FoodName} (Score: {topFoods[i].TotalScore:F2})");
                }
                
                // Add variety to the options header
                var optionsHeaders = new List<string>();
                
                // Add meal type specific headers if detected
                if (!string.IsNullOrEmpty(detectedMealType))
                {
                    optionsHeaders.AddRange(new List<string>
                    {
                        $"Here are your personalized {detectedMealType} recommendations:",
                        $"These {detectedMealType} options are perfect for you:",
                        $"I've selected these {detectedMealType} choices for your taste:",
                        $"These {detectedMealType} suggestions match your preferences:",
                        $"Your ideal {detectedMealType} recommendations:",
                        $"These {detectedMealType} options are tailored for you:"
                    });
                }
                
                // Add general headers
                optionsHeaders.AddRange(new List<string>
                {
                    "Based on your comprehensive profile, I recommend these foods:",
                    "Here are my top recommendations for you:",
                    "These are the best options I've found for you:",
                    "I've selected these foods specifically for you:",
                    "Here are the perfect choices for your profile:",
                    "These recommendations are tailored to your needs:",
                    "I've chosen these options with you in mind:",
                    "Here are the ideal selections for your preferences:",
                    "These foods are perfectly suited to your profile:",
                    "I've picked these recommendations just for you:",
                    "Here are the best matches for your taste profile:",
                    "These selections are customized for your needs:",
                    "I've curated these options specifically for you:",
                    "Here are the perfect picks for your preferences:",
                    "These recommendations are designed for your profile:",
                    "I've handpicked these foods for your enjoyment:",
                    "Here are the ideal choices for your taste:",
                    "These options are perfectly matched to your needs:",
                    "I've selected these foods with your preferences in mind:",
                    "Here are the best recommendations for your profile:"
                });
                var random = new Random();
                var selectedHeader = optionsHeaders[random.Next(optionsHeaders.Count)];
                
                optionsSection = selectedHeader + "\n" + string.Join("\n", numberedFoodList);
            }

            // Build conclusion section
            var conclusionParts = new List<string>();
            
            // Add meal type specific conclusion if detected
            if (!string.IsNullOrEmpty(detectedMealType) && foodPreferences.Any())
            {
                var mealTypeConclusionTemplates = new List<string>
                {
                    $"These {detectedMealType} options are perfect for your preferences",
                    $"All these {detectedMealType} recommendations are tailored to your taste",
                    $"These {detectedMealType} choices match your dietary needs perfectly",
                    $"I've selected these {detectedMealType} options based on your profile",
                    $"These {detectedMealType} suggestions are ideal for your requirements"
                };
                var random = new Random();
                conclusionParts.Add(mealTypeConclusionTemplates[random.Next(mealTypeConclusionTemplates.Count)]);
            }
            
            if (foodPreferences.Any())
            {
                var safeFoods = foodPreferences.Where(fp => fp.IsSafeForUser).ToList();
                var unsafeFoods = foodPreferences.Where(fp => !fp.IsSafeForUser).ToList();
                
                if (safeFoods.Any())
                {
                    conclusionParts.Add($"These are all safe for your dietary requirements: {string.Join(", ", safeFoods.Select(sf => sf.FoodName))}");
                }
                
                if (unsafeFoods.Any())
                {
                    conclusionParts.Add($"Note: Please be cautious with {string.Join(", ", unsafeFoods.Select(uf => uf.FoodName))} due to your restrictions");
                }

                var topFood = foodPreferences.First();
                if (!string.IsNullOrEmpty(topFood.Reason))
                {
                    conclusionParts.Add($"Top recommendation reasoning: {topFood.Reason}");
                }
            }

            // Build the complete response with organized structure
            var response = new List<string>();
            
            // Introduction section with extensive variety
            if (introductionParts.Any())
            {
                var introduction = string.Join(". ", introductionParts);
                var introductionTemplates = new List<string>
                {
                    // Analysis and discovery themes
                    $"{introduction}. I've analyzed your preferences and found some perfect matches!",
                    $"{introduction}. I've carefully studied your profile and discovered excellent options!",
                    $"{introduction}. After reviewing your preferences, I've found some ideal choices!",
                    $"{introduction}. I've examined your tastes and uncovered some fantastic matches!",
                    $"{introduction}. I've analyzed your requirements and identified perfect options!",
                    
                    // Sharing and presenting themes
                    $"{introduction}. Let me share some personalized recommendations that suit your taste!",
                    $"{introduction}. I'd love to present some carefully selected options for you!",
                    $"{introduction}. Allow me to share some wonderful recommendations tailored to you!",
                    $"{introduction}. I'm excited to present some fantastic choices just for you!",
                    $"{introduction}. Let me show you some amazing options I've found!",
                    
                    // Curation and selection themes
                    $"{introduction}. I've curated these options specifically for your needs!",
                    $"{introduction}. I've handpicked these selections with you in mind!",
                    $"{introduction}. I've carefully chosen these options based on your profile!",
                    $"{introduction}. I've selected these recommendations just for you!",
                    $"{introduction}. I've curated a special list tailored to your preferences!",
                    
                    // Discovery and exploration themes
                    $"{introduction}. Here are some fantastic choices tailored just for you!",
                    $"{introduction}. I've discovered some amazing options that match your profile!",
                    $"{introduction}. I've found some incredible choices that suit your needs!",
                    $"{introduction}. Here are some wonderful discoveries I've made for you!",
                    $"{introduction}. I've uncovered some excellent options perfect for you!",
                    
                    // Preparation and planning themes
                    $"{introduction}. Let me present some carefully selected recommendations!",
                    $"{introduction}. I've prepared some wonderful options that suit your needs!",
                    $"{introduction}. I've crafted some special recommendations just for you!",
                    $"{introduction}. I've designed these suggestions with your preferences in mind!",
                    $"{introduction}. I've put together some perfect options for you!",
                    
                    // Personal and intimate themes
                    $"{introduction}. I've found some excellent choices based on your preferences!",
                    $"{introduction}. Here are some personalized suggestions just for you!",
                    $"{introduction}. I've created some custom recommendations for you!",
                    $"{introduction}. I've tailored these options specifically to your taste!",
                    $"{introduction}. I've personalized these suggestions just for you!",
                    
                    // Enthusiasm and excitement themes
                    $"{introduction}. Let me share some perfect matches for your taste profile!",
                    $"{introduction}. I'm thrilled to present these amazing options for you!",
                    $"{introduction}. I'm excited to share these fantastic recommendations!",
                    $"{introduction}. I can't wait to show you these perfect choices!",
                    $"{introduction}. I'm delighted to present these wonderful options!",
                    
                    // Professional and confident themes
                    $"{introduction}. Based on my analysis, here are your ideal recommendations!",
                    $"{introduction}. I'm confident these selections will suit your preferences!",
                    $"{introduction}. I've identified the perfect options for your needs!",
                    $"{introduction}. These recommendations are specifically designed for you!",
                    $"{introduction}. I've determined the best choices for your profile!"
                };
                var random = new Random();
                response.Add(introductionTemplates[random.Next(introductionTemplates.Count)]);
            }

            // Options section
            if (!string.IsNullOrEmpty(optionsSection))
            {
                response.Add(optionsSection);
            }

            // Conclusion section with extensive variety
            if (conclusionParts.Any())
            {
                var conclusion = string.Join(". ", conclusionParts);
                var conclusionTemplates = new List<string>
                {
                    // Direct invitation themes
                    $"{conclusion}. Choose any option that appeals to you!",
                    $"{conclusion}. Feel free to select whichever option catches your interest!",
                    $"{conclusion}. Pick the one that sounds most appealing to you!",
                    $"{conclusion}. Select any option that matches your current mood!",
                    $"{conclusion}. Choose whichever option excites you the most!",
                    $"{conclusion}. Go ahead and pick the one that calls to you!",
                    $"{conclusion}. Select any option that sounds delicious to you!",
                    $"{conclusion}. Choose the option that makes your mouth water!",
                    $"{conclusion}. Pick whichever one sounds most tempting!",
                    $"{conclusion}. Select the option that best fits your current craving!",
                    
                    // Encouraging and supportive themes
                    $"{conclusion}. I encourage you to choose whatever speaks to you!",
                    $"{conclusion}. Take your time and pick what feels right for you!",
                    $"{conclusion}. I'm confident any of these will satisfy your taste!",
                    $"{conclusion}. Feel confident in choosing any option that calls to you!",
                    $"{conclusion}. I trust you'll make a great choice from these options!",
                    $"{conclusion}. Any selection you make will be a wonderful choice!",
                    $"{conclusion}. I'm sure you'll love whichever option you choose!",
                    $"{conclusion}. Trust your instincts and pick what appeals to you!",
                    $"{conclusion}. I believe any of these will make you happy!",
                    $"{conclusion}. Go with your gut and choose what feels right!",
                    
                    // Enthusiastic and excited themes
                    $"{conclusion}. I'm excited to see which option you'll choose!",
                    $"{conclusion}. I can't wait to see what catches your eye!",
                    $"{conclusion}. I'm thrilled to present these options for your selection!",
                    $"{conclusion}. I'm eager to see which one you'll pick!",
                    $"{conclusion}. I'm delighted to offer these choices for you!",
                    $"{conclusion}. I'm so excited to see your choice!",
                    $"{conclusion}. I'm looking forward to seeing what you select!",
                    $"{conclusion}. I'm enthusiastic about your upcoming choice!",
                    $"{conclusion}. I'm pumped to see which option you'll go with!",
                    $"{conclusion}. I'm overjoyed to present these options to you!",
                    
                    // Personal and intimate themes
                    $"{conclusion}. I've prepared these especially for you to choose from!",
                    $"{conclusion}. These options are all yours to select from!",
                    $"{conclusion}. I've crafted these choices specifically for your selection!",
                    $"{conclusion}. These recommendations are tailored for your choosing!",
                    $"{conclusion}. I've personalized these options for your decision!",
                    $"{conclusion}. These are all yours to pick from!",
                    $"{conclusion}. I've made these selections just for you to choose!",
                    $"{conclusion}. These options are designed for your personal choice!",
                    $"{conclusion}. I've curated these specifically for your selection!",
                    $"{conclusion}. These choices are all yours to decide from!",
                    
                    // Casual and friendly themes
                    $"{conclusion}. Take your pick from any of these great options!",
                    $"{conclusion}. Feel free to choose whatever strikes your fancy!",
                    $"{conclusion}. Go ahead and pick whatever looks good to you!",
                    $"{conclusion}. Just choose whatever sounds tasty to you!",
                    $"{conclusion}. Pick whatever makes you hungry!",
                    $"{conclusion}. Choose whatever gets your taste buds excited!",
                    $"{conclusion}. Go with whatever sounds most delicious!",
                    $"{conclusion}. Pick whatever makes you want to eat right now!",
                    $"{conclusion}. Choose whatever makes your stomach growl!",
                    $"{conclusion}. Go for whatever makes you salivate!",
                    
                    // Professional and confident themes
                    $"{conclusion}. I recommend selecting any option that meets your criteria!",
                    $"{conclusion}. Please choose the option that best suits your needs!",
                    $"{conclusion}. I suggest selecting whichever option aligns with your preferences!",
                    $"{conclusion}. I recommend choosing the option that fits your requirements!",
                    $"{conclusion}. Please select the option that best matches your profile!",
                    $"{conclusion}. I advise choosing the option that serves your needs best!",
                    $"{conclusion}. I suggest selecting the option that meets your standards!",
                    $"{conclusion}. I recommend choosing the option that satisfies your criteria!",
                    $"{conclusion}. Please pick the option that best fulfills your requirements!",
                    $"{conclusion}. I suggest selecting the option that best serves your interests!"
                };
                var random = new Random();
                response.Add(conclusionTemplates[random.Next(conclusionTemplates.Count)]);
            }

            return string.Join("\n\n", response);
        }

        // Helper method to extract parameters from a response string
        private ExtractedParameters ExtractParametersFromResponse(string responseText)
        {
            var extractedParams = new ExtractedParameters();
            
            // Extract dietary preference
            if (responseText.Contains("vegetarian"))
                extractedParams.Dietary = "vegetarian";
            else if (responseText.Contains("vegan"))
                extractedParams.Dietary = "vegan";
            else if (responseText.Contains("gluten-free"))
                extractedParams.Dietary = "gluten-free";
            
            // Extract food preferences
            if (responseText.Contains("love Sushi"))
                extractedParams.Food = "sushi";
            else if (responseText.Contains("love"))
            {
                var loveMatch = Regex.Match(responseText, @"love\s+([^,\s]+)");
                if (loveMatch.Success)
                    extractedParams.Food = loveMatch.Groups[1].Value.Trim();
            }
            
            // Extract cuisine preferences
            if (responseText.Contains("enjoy american cuisine"))
                extractedParams.Cuisine = "american";
            else if (responseText.Contains("enjoy"))
            {
                var cuisineMatch = Regex.Match(responseText, @"enjoy\s+([^,\s]+)\s+cuisine");
                if (cuisineMatch.Success)
                    extractedParams.Cuisine = cuisineMatch.Groups[1].Value.Trim();
            }
            
            // Extract price preference
            if (responseText.Contains("preferring budget options"))
                extractedParams.Price = "budget";
            else if (responseText.Contains("budget"))
                extractedParams.Price = "budget";
            else if (responseText.Contains("expensive") || responseText.Contains("high-end"))
                extractedParams.Price = "high-end";
            else if (responseText.Contains("mid-range") || responseText.Contains("moderate"))
                extractedParams.Price = "mid-range";
            
            // Set default location for personalized queries
            extractedParams.Location = "nearby";
            
            return extractedParams;
        }

        // DUAL FLOW DETECTION METHODS
        private bool IsGeneralizedQuery(string? food, string? cuisine, string? location, string? price, string? dietary)
        {
            // Generalized query: User provides specific parameters from Dialogflow
            // Has at least one concrete search parameter (food, cuisine, location, etc.)
            return !string.IsNullOrEmpty(food) || 
                   !string.IsNullOrEmpty(cuisine) || 
                   !string.IsNullOrEmpty(location) || 
                   !string.IsNullOrEmpty(price) || 
                   !string.IsNullOrEmpty(dietary);
        }

        private bool IsPersonalizedQuery(string? message, string? food, string? cuisine, string? location, string? price, string? dietary, int? userId)
        {
            // Personalized query: Vague request that should use user's profile and prescriptive algorithm
            var messageText = (message ?? string.Empty).ToLowerInvariant();
            
            // Check for vague/personal intent keywords
            bool hasPersonalKeywords = System.Text.RegularExpressions.Regex.IsMatch(messageText,
                @"\b(recommend|suggest|what should i|help me|find me|good for|best for|i want|i need|my|preference)\b");
            
            // Check for explicit list requests
            bool wantsExplicitList = System.Text.RegularExpressions.Regex.IsMatch(messageText,
                @"\b(list|options|choices|show|top|recommendations|what are|suggest some)\b");
            
            // Is personalized if: has personal keywords, user is logged in, no specific parameters provided, and not asking for explicit list
            return hasPersonalKeywords && 
                   userId.HasValue && 
                   string.IsNullOrEmpty(food) && 
                   string.IsNullOrEmpty(cuisine) && 
                   !wantsExplicitList;
        }

        // NEW: Extract meal type from user query
        private string? ExtractMealTypeFromQuery(string query)
        {
            if (string.IsNullOrEmpty(query)) return null;
            
            var queryLower = query.ToLowerInvariant();
            
            // Breakfast keywords
            if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, @"\b(breakfast|morning meal|am|early|first meal|morning food)\b"))
                return "breakfast";
            
            // Lunch keywords
            if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, @"\b(lunch|midday|noon|afternoon meal|lunchtime)\b"))
                return "lunch";
            
            // Dinner keywords
            if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, @"\b(dinner|evening meal|pm|night|supper|dinnertime)\b"))
                return "dinner";
            
            // Snacks keywords
            if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, @"\b(snack|snacks|light bite|quick bite|meryenda|merienda|appetizer)\b"))
                return "snacks";
            
            // Dessert keywords
            if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, @"\b(dessert|sweet|ice cream|cake|pastry|after meal)\b"))
                return "dessert";
            
            // Beverage keywords
            if (System.Text.RegularExpressions.Regex.IsMatch(queryLower, @"\b(drink|beverage|juice|coffee|tea|soda|water|alcohol)\b"))
                return "beverage";
            
            return null;
        }
    }
} 