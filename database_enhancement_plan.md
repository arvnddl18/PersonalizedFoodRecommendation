# Database Enhancement Plan for Evidence-Based Prescriptive Recommender

## Current Database Issues

The current database lacks essential fields required for the evidence-based weighted scoring algorithm. The documentation shows sample datasets with nutritional properties that don't exist in our current schema.

## Required Database Changes

### 1. Enhance FoodType Table

**Add Nutritional Property Fields:**
```sql
ALTER TABLE FoodTypes ADD COLUMN CuisineType NVARCHAR(50) NULL;
ALTER TABLE FoodTypes ADD COLUMN IsHealthy BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsLowCalorie BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsHighProtein BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsNutrientDense BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsVitaminRich BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsLowGlycemic BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsLowSodium BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsHeartHealthy BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsIronRich BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN ContainsGluten BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN ContainsDairy BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN ContainsNuts BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN ContainsMeat BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsVegetarian BIT NOT NULL DEFAULT 0;
ALTER TABLE FoodTypes ADD COLUMN IsVegan BIT NOT NULL DEFAULT 0;
```

### 2. Enhance UserProfile Table

**Add Demographics:**
```sql
ALTER TABLE UserProfiles ADD COLUMN Age INT NULL;
ALTER TABLE UserProfiles ADD COLUMN Gender NVARCHAR(20) NULL;
ALTER TABLE UserProfiles ADD COLUMN ActivityLevel NVARCHAR(20) NULL; -- 'Sedentary', 'Moderate', 'Active'
```

### 3. Enhance UserPreferencePattern Table

**Add Interaction Metrics:**
```sql
ALTER TABLE UserPreferencePatterns ADD COLUMN TotalInteractions INT NOT NULL DEFAULT 0;
ALTER TABLE UserPreferencePatterns ADD COLUMN SuccessfulInteractions INT NOT NULL DEFAULT 0;
```

### 4. Create New FoodNutrition Table

**Detailed Nutritional Data:**
```sql
CREATE TABLE FoodNutrition (
    FoodNutritionId INT IDENTITY(1,1) PRIMARY KEY,
    FoodTypeId INT NOT NULL,
    CaloriesPerServing DECIMAL(6,2) NULL,
    ProteinGrams DECIMAL(5,2) NULL,
    CarbGrams DECIMAL(5,2) NULL,
    FatGrams DECIMAL(5,2) NULL,
    FiberGrams DECIMAL(5,2) NULL,
    SodiumMg DECIMAL(7,2) NULL,
    SugarGrams DECIMAL(5,2) NULL,
    IronMg DECIMAL(5,2) NULL,
    VitaminCMg DECIMAL(5,2) NULL,
    CalciumMg DECIMAL(6,2) NULL,
    ServingSize NVARCHAR(50) NULL,
    LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (FoodTypeId) REFERENCES FoodTypes(FoodTypeId)
);
```

### 5. Create FoodAllergen Table

**Allergy Management:**
```sql
CREATE TABLE FoodAllergens (
    FoodAllergenId INT IDENTITY(1,1) PRIMARY KEY,
    FoodTypeId INT NOT NULL,
    AllergenName NVARCHAR(50) NOT NULL, -- 'Gluten', 'Dairy', 'Tree Nuts', 'Peanuts', 'Soy', 'Eggs', 'Fish', 'Shellfish'
    ContainsAllergen BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (FoodTypeId) REFERENCES FoodTypes(FoodTypeId)
);
```

### 6. Enhanced UserBehavior Table

**Add missing context fields:**
```sql
ALTER TABLE UserBehaviors ADD COLUMN QueryText NVARCHAR(500) NULL;
ALTER TABLE UserBehaviors ADD COLUMN TimeOfDay NVARCHAR(20) NULL; -- 'Morning', 'Afternoon', 'Evening', 'Night'
ALTER TABLE UserBehaviors ADD COLUMN LocationContext NVARCHAR(100) NULL;
ALTER TABLE UserBehaviors ADD COLUMN WeatherContext NVARCHAR(50) NULL;
ALTER TABLE UserBehaviors ADD COLUMN MoodContext NVARCHAR(50) NULL;
```

## Sample Data Population

### FoodType Enhanced Data
Based on documentation requirements, populate existing foods with nutritional properties:

```sql
-- Pizza
UPDATE FoodTypes SET 
    CuisineType = 'Italian',
    IsHealthy = 0,
    IsLowCalorie = 0,
    IsHighProtein = 1,
    IsNutrientDense = 0,
    IsVitaminRich = 0,
    ContainsGluten = 1,
    ContainsDairy = 1,
    IsVegetarian = 1,
    IsVegan = 0
WHERE Name = 'Pizza';

-- Sushi
UPDATE FoodTypes SET 
    CuisineType = 'Japanese',
    IsHealthy = 1,
    IsLowCalorie = 1,
    IsHighProtein = 1,
    IsNutrientDense = 1,
    IsVitaminRich = 0,
    ContainsMeat = 1,
    IsVegetarian = 0,
    IsVegan = 0
WHERE Name = 'Sushi';

-- Salad
UPDATE FoodTypes SET 
    CuisineType = 'International',
    IsHealthy = 1,
    IsLowCalorie = 1,
    IsHighProtein = 0,
    IsNutrientDense = 1,
    IsVitaminRich = 1,
    IsLowSodium = 1,
    IsVegetarian = 1,
    IsVegan = 1
WHERE Name = 'Salad';
```

## Migration Benefits

1. **Evidence-Based Scoring:** Enable the full weighted algorithm from documentation
2. **Safety Compliance:** Proper allergy and dietary restriction checking
3. **Nutritional Optimization:** Accurate health-based recommendations
4. **Behavioral Learning:** Better pattern recognition and confidence calculation
5. **Personalization:** Age and demographic-based adjustments

## Implementation Priority

1. **HIGH:** FoodType nutritional properties (required for current algorithm)
2. **HIGH:** Age field in UserProfile (demographic targeting)
3. **MEDIUM:** FoodNutrition table (enhanced scoring)
4. **MEDIUM:** Enhanced UserBehavior context
5. **LOW:** FoodAllergen table (comprehensive allergy management)

## Risk Assessment

- **Database Migration:** Low risk with proper backup
- **Algorithm Compatibility:** Current methods will work with default values
- **Data Population:** Requires manual/automated nutritional data entry
- **Performance Impact:** Minimal with proper indexing
