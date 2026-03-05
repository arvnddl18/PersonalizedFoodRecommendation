using System.Collections.Generic;
using Capstone.Models;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Models
{
    public class PreferencesViewModel
    {
        // REMOVED: DietaryPreference - replaced by DietaryRestrictions multi-value system
        // REMOVED: FavoriteFood - replaced by behavioral analysis and food preference ranking
        
        public string? PreferredLocation { get; set; }
        public string? PriceRange { get; set; }
        public List<FoodTypeSelection> FoodTypes { get; set; }
        
        // Multi-value preference system
        public List<DietaryRestrictionSelection> DietaryRestrictions { get; set; }
        public List<HealthConditionSelection> HealthConditions { get; set; }

        // Selected food type IDs coming from client JSON-driven UI
        public List<int>? SelectedFoodTypeIds { get; set; }

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string SearchTerm { get; set; } = string.Empty;
        public int TotalFoodTypes { get; set; } = 0;

        public PreferencesViewModel()
        {
            FoodTypes = new List<FoodTypeSelection>();
            DietaryRestrictions = new List<DietaryRestrictionSelection>();
            HealthConditions = new List<HealthConditionSelection>();
            SelectedFoodTypeIds = new List<int>();
        }
    }

    public class FoodTypeSelection
    {
        public int FoodTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class DietaryRestrictionSelection
    {
        public int DietaryRestrictionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public int ImportanceLevel { get; set; } = 1; // 1-10 scale
    }

    public class HealthConditionSelection
    {
        public int HealthConditionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public int SeverityLevel { get; set; } = 1; // 1-10 scale
    }
} 