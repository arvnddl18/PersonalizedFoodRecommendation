# FoodieSaur - System Architecture & Data Flow Diagram

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER (Browser)                           │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Presentation Layer (Razor Views + JavaScript + CSS)              │  │
│  │  - Chat.cshtml / ChatMobile.cshtml                               │  │
│  │  - Google Maps JavaScript API (Client-side)                      │  │
│  │  - TailwindCSS + Custom CSS                                      │  │
│  │  - jQuery + Fetch API                                            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ HTTP/HTTPS
                                    │
┌───────────────────────────────────▼──────────────────────────────────────┐
│                    APPLICATION SERVER (ASP.NET Core)                     │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Controller Layer                                                │  │
│  │  - HomeController (Views, Routing)                               │  │
│  │  - ChatController (API Endpoints)                                │  │
│  │  - AuthController (Login/Register/OAuth)                          │  │
│  │  - PreferencesController (User Preferences)                       │  │
│  │  - FavoritesController (Favorites Management)                    │  │
│  │  - AccountController (User Account)                              │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                    │                                      │
│  ┌────────────────────────────────▼──────────────────────────────────┐  │
│  │  Service Layer                                                    │  │
│  │  - DialogflowService (NLP, Intent Detection)                     │  │
│  │  - UserBehaviorService (Prescriptive Algorithm)                  │  │
│  │  - ScrapingBeeService (Web Scraping)                             │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                    │                                      │
│  ┌────────────────────────────────▼──────────────────────────────────┐  │
│  │  Data Access Layer                                                │  │
│  │  - AppDbContext (Entity Framework Core)                           │  │
│  │  - LINQ Queries → SQL                                             │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ TDS Protocol (SQL)
                                    │
┌───────────────────────────────────▼──────────────────────────────────────┐
│                        SQL SERVER DATABASE                               │
│  - Users, UserProfiles                                                  │
│  - FoodTypes, UserFoodTypes                                             │
│  - ChatSessions, ChatMessages                                           │
│  - UserBehaviors, UserPreferencePatterns                               │
│  - DietaryRestrictions, HealthConditions                                │
│  - UserFavoriteRestaurants                                              │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      EXTERNAL SERVICES LAYER                            │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐      │
│  │ Google Maps API  │  │ Dialogflow API  │  │  OAuth Providers │      │
│  │ - Places API     │  │ - Intent Detect  │  │  - Google        │      │
│  │ - Geocoding      │  │ - Entity Extract │  │  - Facebook      │      │
│  │ - JavaScript API │  │                  │  │                  │      │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘      │
│                                                                          │
│  ┌──────────────────┐                                                   │
│  │ ScrapingBee API  │                                                   │
│  │ - Web Scraping   │                                                   │
│  └──────────────────┘                                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

## Data Flow: User Authentication

```
┌──────────┐
│  User    │
└────┬─────┘
     │ 1. Clicks "Sign in with Google/Facebook" or enters credentials
     ▼
┌─────────────────────┐
│  AuthController     │
│  - Login/Register   │
└────┬────────────────┘
     │
     ├─── OAuth Flow ───────────────────────────────┐
     │                                               │
     │ 2. Redirect to OAuth Provider                 │
     ▼                                               │
┌─────────────────────┐                             │
│ Google/Facebook     │                             │
│ OAuth Endpoint      │                             │
└────┬────────────────┘                             │
     │ 3. User grants permissions                    │
     │ 4. Redirect with authorization code          │
     ▼                                               │
┌─────────────────────┐                             │
│ AuthController      │                             │
│ Callback Handler    │                             │
└────┬────────────────┘                             │
     │ 5. Exchange code for token                   │
     │ 6. Retrieve user profile                     │
     │                                               │
     └─── Local Auth Flow ───────────────────────────┘
     │
     │ 2. Validate credentials (BCrypt)
     ▼
┌─────────────────────┐
│  AppDbContext       │
│  - Create/Update    │
│    User record      │
└────┬────────────────┘
     │ 7. Create session
     ▼
┌─────────────────────┐
│  Session Storage    │
│  - UserId           │
│  - Authentication   │
└────┬────────────────┘
     │ 8. Redirect to Preferences or Chat
     ▼
┌─────────────────────┐
│  User Dashboard     │
└─────────────────────┘
```

## Data Flow: Chat Message Processing

```
┌──────────┐
│  User    │ Types message: "I want Italian food near me"
└────┬─────┘
     │
     ▼
┌─────────────────────┐
│  Chat.cshtml        │ JavaScript sends POST request
│  (Frontend)         │
└────┬────────────────┘
     │ POST /api/Chat/send
     ▼
┌─────────────────────┐
│  ChatController     │
│  SendMessage()      │
└────┬────────────────┘
     │
     ├─── 1. Create/Retrieve ChatSession
     │    └───► AppDbContext.ChatSessions
     │
     ├─── 2. Save User Message
     │    └───► AppDbContext.ChatMessages
     │
     ├─── 3. Process with Dialogflow
     │    └───► DialogflowService.SendMessageAsync()
     │         │
     │         ├───► Google Cloud Dialogflow API
     │         │    - Detect Intent
     │         │    - Extract Entities (food type, location, etc.)
     │         │
     │         └───► Intent: PersonalizedFoodRecommendation
     │
     ├─── 4. Generate Recommendations
     │    └───► UserBehaviorService.GenerateFoodPreferenceRankings()
     │         │
     │         ├─── Load User Data
     │         │    ├───► UserFoodTypes (preferences)
     │         │    ├───► UserDietaryRestrictions
     │         │    ├───► UserHealthConditions
     │         │    ├───► UserPreferencePatterns (learned patterns)
     │         │    └───► UserBehaviors (interaction history)
     │         │
     │         ├─── Apply Prescriptive Algorithm
     │         │    Score = (UserPreferenceWeight × 0.4) +
     │         │            (BehavioralPatternWeight × 0.3) +
     │         │            (ContextualFactorWeight × 0.2) +
     │         │            (HealthGoalWeight × 0.1)
     │         │
     │         └─── Query Google Maps Places API
     │              ├─── Search by food type + location
     │              ├─── Filter by proximity (Haversine formula)
     │              └─── Rank by prescriptive score
     │
     ├─── 5. Save AI Response
     │    └───► AppDbContext.ChatMessages
     │
     └─── 6. Track User Behavior
          └───► UserBehaviorService.TrackUserInteraction()
               └───► AppDbContext.UserBehaviors
                    └───► Trigger Pattern Analysis
                         └───► Update UserPreferencePatterns

     │
     ▼
┌─────────────────────┐
│  JSON Response      │ { response, parameters, sessionId }
└────┬────────────────┘
     │
     ▼
┌─────────────────────┐
│  Chat.cshtml        │ Display response, update map markers
│  (Frontend)         │
└─────────────────────┘
```

## Data Flow: Prescriptive Recommendation Algorithm

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PRESCRIPTIVE ALGORITHM FLOW                     │
└─────────────────────────────────────────────────────────────────────┘

Input: User Query + Location (lat/lng) + UserId
│
├─── STEP 1: Load User Context
│    ├───► UserFoodTypes (explicit preferences, score 1-10)
│    ├───► UserDietaryRestrictions (importance 1-10)
│    ├───► UserHealthConditions (severity 1-10)
│    ├───► UserPreferencePatterns (learned patterns, confidence 0-1)
│    └───► UserBehaviors (interaction history)
│
├─── STEP 2: Query FoodTypes Catalog
│    └───► Filter by:
│         - Nutritional properties (IsHealthy, IsLowCalorie, etc.)
│         - Dietary compliance (IsVegetarian, ContainsGluten, etc.)
│         - Health-specific (IsLowGlycemic, IsLowSodium, etc.)
│         - Meal type (IsBreakfast, IsLunch, IsDinner, etc.)
│
├─── STEP 3: Calculate Component Weights
│    │
│    ├─── UserPreferenceWeight (0.4 - 0.9)
│    │    └─── Based on UserFoodTypes.PreferenceScore
│    │
│    ├─── BehavioralPatternWeight (0.3 - 1.0)
│    │    └─── Based on UserPreferencePatterns.Confidence
│    │         - Cuisine patterns
│    │         - Time patterns (breakfast/lunch/dinner)
│    │         - Price patterns
│    │
│    ├─── ContextualFactorWeight (0.4 - 0.9)
│    │    └─── Based on:
│    │         - Time of day → Meal type matching
│    │         - Location proximity → Distance score
│    │         - Query keywords → Food type matching
│    │
│    └─── HealthGoalWeight (0.5 - 0.8)
│         └─── Based on:
│              - Dietary restrictions compliance
│              - Health conditions alignment
│              - Nutritional properties matching
│
├─── STEP 4: Calculate Prescriptive Score
│    └─── Score = (UserPreferenceWeight × 0.4) +
│                 (BehavioralPatternWeight × 0.3) +
│                 (ContextualFactorWeight × 0.2) +
│                 (HealthGoalWeight × 0.1)
│
├─── STEP 5: Query Google Maps Places API
│    └─── Search Parameters:
│         - Query: Food type name + cuisine
│         - Location: User's lat/lng
│         - Radius: 5km or 10km
│         - Type: restaurant, food
│
├─── STEP 6: Filter & Rank Restaurants
│    ├─── Calculate distance (Haversine formula)
│    ├─── Apply prescriptive score
│    ├─── Filter by dietary restrictions
│    └─── Sort by combined score (prescriptive + proximity)
│
└─── STEP 7: Generate Response
     ├─── Top 10 recommendations
     ├─── Score for each (0.00 - 1.00)
     ├─── Reason for recommendation
     └─── Safety check (dietary/health compliance)
```

## Data Flow: Behavior Learning System

```
┌─────────────────────────────────────────────────────────────────────┐
│                    BEHAVIOR LEARNING FLOW                            │
└─────────────────────────────────────────────────────────────────────┘

User Interaction Occurs
│
├─── Track Interaction
│    └───► UserBehaviorService.TrackUserInteraction()
│         └───► Save to UserBehaviors table
│              - Action (search, select, rate, recommendation, conversation)
│              - Context (time, location, query, food preferences)
│              - Result (selected item, satisfaction rating)
│              - Timestamp
│
├─── Analyze Patterns
│    └───► UserBehaviorService.AnalyzeAndUpdatePatterns()
│         │
│         ├─── Extract Pattern Data
│         │    ├─── Time patterns (morning/afternoon/evening)
│         │    ├─── Cuisine patterns (Italian, Japanese, etc.)
│         │    ├─── Location patterns (frequent areas)
│         │    └─── Price patterns (budget/moderate/premium)
│         │
│         ├─── Calculate Confidence
│         │    └─── Confidence = SuccessfulInteractions / TotalInteractions
│         │         - Successful: Satisfaction >= 4
│         │         - Range: 0.00 - 1.00
│         │
│         └─── Update/Create Pattern
│              └───► UserPreferencePatterns table
│                   - PatternType (time, cuisine, location, price)
│                   - PatternValue (evening, Italian, etc.)
│                   - Confidence (calculated)
│                   - ObservationCount (incremented)
│                   - LastObserved (updated)
│
└─── Influence Future Recommendations
     └─── Patterns used in BehavioralPatternWeight calculation
          └─── Higher confidence → Higher weight → Better recommendations
```

## External API Integration Points

```
┌─────────────────────────────────────────────────────────────────────┐
│              EXTERNAL API INTEGRATION                                │
└─────────────────────────────────────────────────────────────────────┘

1. GOOGLE MAPS API
   ├─── Places API (REST)
   │    ├─── Endpoint: /maps/api/place/search
   │    ├─── Method: GET
   │    ├─── Auth: API Key (query parameter)
   │    └─── Purpose: Restaurant search, place details
   │
   ├─── Geocoding API (REST)
   │    ├─── Endpoint: /maps/api/geocode/json
   │    ├─── Method: GET
   │    └─── Purpose: Address → Coordinates conversion
   │
   └─── JavaScript API (Client-side)
        ├─── Load: <script src="https://maps.googleapis.com/maps/api/js?key=...">
        └─── Purpose: Interactive map rendering

2. DIALOGFLOW API
   ├─── Sessions API (gRPC)
   │    ├─── Endpoint: projects/{project}/agent/sessions/{session}:detectIntent
   │    ├─── Method: gRPC call
   │    ├─── Auth: Service Account JSON
   │    └─── Purpose: Intent detection, entity extraction
   │
   └─── Protocol: Protocol Buffers (protobuf)

3. GOOGLE OAUTH 2.0
   ├─── Authorization: https://accounts.google.com/o/oauth2/v2/auth
   ├─── Token: https://oauth2.googleapis.com/token
   ├─── User Info: https://www.googleapis.com/oauth2/v2/userinfo
   └─── Flow: Authorization Code Flow

4. FACEBOOK OAUTH 2.0
   ├─── Authorization: https://www.facebook.com/v18.0/dialog/oauth
   ├─── Token: https://graph.facebook.com/v18.0/oauth/access_token
   ├─── User Info: https://graph.facebook.com/me?fields=id,name,email
   └─── Flow: Authorization Code Flow

5. SCRAPINGBEE API
   ├─── Endpoint: https://app.scrapingbee.com/api/v1/
   ├─── Method: GET
   ├─── Auth: API Key (header)
   └─── Purpose: Web scraping for restaurant data
```

## Database Relationships Summary

```
Users (1) ──── (1) UserProfiles
Users (1) ──── (N) UserFoodTypes
Users (1) ──── (N) ChatSessions
Users (1) ──── (N) UserBehaviors
Users (1) ──── (N) UserPreferencePatterns
Users (1) ──── (N) UserDietaryRestrictions
Users (1) ──── (N) UserHealthConditions
Users (1) ──── (N) UserFavoriteRestaurants

FoodTypes (1) ──── (N) UserFoodTypes

ChatSessions (1) ──── (N) ChatMessages

DietaryRestrictions (1) ──── (N) UserDietaryRestrictions

HealthConditions (1) ──── (N) UserHealthConditions
```

---

**Note**: This diagram uses ASCII art format. For visual diagrams, import the `.dbml` files into [DBdiagram.io](https://dbdiagram.io) or use diagramming tools like Draw.io, Lucidchart, or Mermaid.

