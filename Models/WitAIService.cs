using System.Text.Json;
using System.Text;

namespace Capstone.Models
{
    public class WitAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;

        public WitAiService(string accessToken)
        {
            _accessToken = accessToken;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        }

        public async Task<string> SendMessageAsync(string message)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.wit.ai/message?q={Uri.EscapeDataString(message)}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var witResponse = JsonSerializer.Deserialize<WitResponse>(content);

                var intent = witResponse?.Intents?.FirstOrDefault()?.Name ?? "unknown";
                var entities = witResponse?.Entities;

                switch (intent.ToLower())
                {
                    case "food_recommendation":
                        return HandleFoodRecommendation(entities);
                    case "restaurant_search":
                        return HandleRestaurantSearch(entities);
                    case "greeting":
                        return "Hello! I'm your food recommendation assistant. How can I help you today?";
                    default:
                        return "I'm not sure I understand. Could you please rephrase your question about food or restaurants?";
                }
            }
            catch (Exception ex)
            {
                return $"Sorry, I encountered an error: {ex.Message}";
            }
        }

        private string HandleFoodRecommendation(dynamic entities)
        {
            var cuisine = GetEntityValue(entities, "cuisine");
            var dietary = GetEntityValue(entities, "dietary");
            var location = GetEntityValue(entities, "location");

            var response = "Based on your preferences, I recommend ";
            
            if (!string.IsNullOrEmpty(cuisine))
            {
                response += $"trying {cuisine} cuisine";
            }
            else
            {
                response += "exploring local restaurants";
            }

            if (!string.IsNullOrEmpty(dietary))
            {
                response += $" that accommodate {dietary} dietary requirements";
            }

            if (!string.IsNullOrEmpty(location))
            {
                response += $" in {location}";
            }

            response += ". Would you like specific restaurant recommendations?";

            return response;
        }

        private string HandleRestaurantSearch(dynamic entities)
        {
            var location = GetEntityValue(entities, "location");
            var cuisine = GetEntityValue(entities, "cuisine");

            var response = "I can help you find restaurants";
            
            if (!string.IsNullOrEmpty(cuisine))
            {
                response += $" serving {cuisine} cuisine";
            }

            if (!string.IsNullOrEmpty(location))
            {
                response += $" in {location}";
            }

            response += ". What type of food are you in the mood for?";

            return response;
        }

        private string GetEntityValue(dynamic entities, string entityName)
        {
            try
            {
                return entities?[entityName]?[0]?["value"]?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    public class WitResponse
    {
        public List<WitIntent> Intents { get; set; }
        public Dictionary<string, List<Dictionary<string, object>>> Entities { get; set; }
    }

    public class WitIntent
    {
        public string Name { get; set; }
        public double Confidence { get; set; }
    }
} 