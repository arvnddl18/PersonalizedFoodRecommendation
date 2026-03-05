namespace Capstone.Models
{
    public class DialogflowResponse
    {
        public string ResponseText { get; set; } = string.Empty;
        public ExtractedParameters ExtractedParameters { get; set; } = new ExtractedParameters();
    }

    public class ExtractedParameters
    {
        public string? Food { get; set; }
        public string? Cuisine { get; set; }
        public string? Location { get; set; }
        public string? Price { get; set; }
        public string? Dietary { get; set; }
        public string? EstablishmentName { get; set; }
        public string IntentName { get; set; } = string.Empty;
        
        // New fields for multiple values
        public List<string> DietaryRestrictions { get; set; } = new List<string>();
        public List<string> HealthConditions { get; set; } = new List<string>();
        
        // Query type for dual flow detection
        public string QueryType { get; set; } = string.Empty; // "generalized" or "personalized"
    }
}
