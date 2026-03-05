# Test Case Document - FoodieSaur System

## Header Information
- **Project Name:** FoodieSaur System
- **Testers:** [Test Engineer Name(s)]

---

## Test Case TC_001: Account Registration

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_001 |
| **Test Case Scenario** | Account Registration |
| **Description** | Test if a new user can successfully create an account by providing valid registration information |
| **Pre-Conditions** | The user must have a valid email address and access to the registration page |
| **Test Steps** | 1. Navigate to the registration page<br>2. Enter valid name<br>3. Enter valid email address<br>4. Enter valid password (meeting requirements)<br>5. Confirm password<br>6. Click the "Register" button |
| **Test Data** | Name: John Doe<br>Email: test.user@example.com<br>Password: SecurePass123!<br>Confirm Password: SecurePass123! |
| **Post-Conditions** | A new user account is created and user can log in |
| **Expected Result** | The user account is successfully created, and the user is redirected to the login page or homepage |
| **Actual Result** | The user account is successfully created, and the user is redirected to the login page |
| **Pass/Fail** | Pass |

---

## Test Case TC_002: Account Log In

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_002 |
| **Test Case Scenario** | Account Log In |
| **Description** | Test if the system allows the user to log in successfully when a valid email and password are provided |
| **Pre-Conditions** | The user must have an active account with valid login credentials |
| **Test Steps** | 1. Open the login page<br>2. Enter a valid email address<br>3. Enter the corresponding valid password<br>4. Click the "Login" button |
| **Test Data** | Email: l.villapaz.539636@umindanao.edu.ph<br>Password: onetwothreefour5.! |
| **Post-Conditions** | A session is created for the user |
| **Expected Result** | The user logs in successfully and is redirected to the homepage |
| **Actual Result** | The user logs in successfully and is redirected to the homepage |
| **Pass/Fail** | Pass |

---

## Test Case TC_003: Food Recommendation

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_003 |
| **Test Case Scenario** | Food Recommendation |
| **Description** | Input a prompt from the chatbot such as "Can you recommend me something?" or "I'm Hungry" |
| **Pre-Conditions** | The user must be logged in and have input food preferences such as food types, dietary restrictions, and health conditions |
| **Test Steps** | 1. Login<br>2. Navigate to Chat Interface<br>3. Type "I'm hungry" or "Can you recommend something?" |
| **Test Data** | Prompt: "I'm Hungry" or "Can you recommend something?" A prompt can be asked from chatbot related to food |
| **Post-Conditions** | Displays the result of restaurant based on the user's prompt |
| **Expected Result** | The system should display personalized food recommendations based on user preferences, location, and behavioral patterns |
| **Actual Result** | The system successfully displays personalized food recommendations based on user preferences, location, and behavioral patterns |
| **Pass/Fail** | Pass |

---

## Test Case TC_004: Dietary Restriction Recognition

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_004 |
| **Test Case Scenario** | Dietary Restriction Recognition |
| **Description** | Test if the chatbot recognizes dietary restrictions mentioned in natural language and filters recommendations accordingly |
| **Pre-Conditions** | The user must be logged in and location services are enabled |
| **Test Steps** | 1. Login<br>2. Navigate to Chat Interface<br>3. Type "I'm vegan, find me nearby restaurants"<br>4. Submit the query |
| **Test Data** | Query: "I'm vegan, find me nearby restaurants" |
| **Post-Conditions** | System processes the dietary restriction and displays filtered recommendations |
| **Expected Result** | The chatbot recognizes "vegan" as a dietary restriction and returns only vegan-friendly restaurant recommendations |
| **Actual Result** | The chatbot successfully recognizes "vegan" as a dietary restriction and returns only vegan-friendly restaurant recommendations |
| **Pass/Fail** | Pass |

---

## Test Case TC_005: Multiple Dietary Restrictions Processing

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_005 |
| **Test Case Scenario** | Multiple Dietary Restrictions Processing |
| **Description** | Test if the system correctly handles multiple dietary restrictions (e.g., vegan and gluten-free) mentioned in a single query |
| **Pre-Conditions** | The user must be logged in and location services are enabled |
| **Test Steps** | 1. Login<br>2. Navigate to Chat Interface<br>3. Enter query: "I'm vegan and gluten-free, show me restaurants nearby"<br>4. Submit the query<br>5. Verify recommendations match both restrictions |
| **Test Data** | Query: "I'm vegan and gluten-free, show me restaurants nearby" |
| **Post-Conditions** | System has extracted and applied both dietary restrictions to filter recommendations |
| **Expected Result** | The chatbot extracts both "vegan" and "gluten-free" restrictions, and returns restaurants that satisfy both criteria |
| **Actual Result** | The chatbot successfully extracts both "vegan" and "gluten-free" restrictions, and returns restaurants that satisfy both criteria |
| **Pass/Fail** | Pass |

---

## Test Case TC_006: Search Function

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_006 |
| **Test Case Scenario** | Search Function |
| **Description** | Searching in chatbot assists the user to find the preferred food establishment and specific cuisine |
| **Pre-Conditions** | The user must be logged in and can search for a cuisine or restaurant type such as sweets, soups, vegetarian food, or food without nuts |
| **Test Steps** | 1. Login<br>2. Navigate to Chat Interface<br>3. Search for specific cuisine or restaurant type (e.g., "Italian restaurants" or "pizza places") |
| **Test Data** | Search: "Can you provide a ramen nearby?" |
| **Post-Conditions** | Display a food establishment based on the searched food cuisine |
| **Expected Result** | Restaurants matching the searched criteria should appear in the search results and on the map |
| **Actual Result** | Restaurants successfully matching the searched criteria appear in the search results and on the map |
| **Pass/Fail** | Pass |

---

## Test Case TC_007: Adding to Favorites

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_007 |
| **Test Case Scenario** | Adding to Favorites |
| **Description** | A user can successfully add a recommended restaurant to their favorites list |
| **Pre-Conditions** | Recommended restaurants are displayed in the result panel |
| **Test Steps** | 1. Login<br>2. Navigate to Chat Interface<br>3. Click a recommended restaurant<br>4. Click heart icon to add favorites |
| **Test Data** | Selected recommended restaurant |
| **Post-Conditions** | Restaurant is added to the favorites panel |
| **Expected Result** | The restaurant should be added to favorites, heart icon should fill, and it should appear in the Favorites Panel |
| **Actual Result** | The restaurant successfully is added to favorites, heart icon fills, and it appears in the Favorites Panel |
| **Pass/Fail** | Pass |

---

## Test Case TC_008: Favorites Viewing

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_008 |
| **Test Case Scenario** | Favorites Viewing |
| **Description** | Users can view their saved favorite restaurants and access detailed information, and that the selected restaurant is visually indicated on the map |
| **Pre-Conditions** | At least one food establishment is marked as favorite |
| **Test Steps** | 1. Login<br>2. View favorites Panel<br>3. Select a favorite restaurant<br>4. View Details |
| **Test Data** | Favorite Restaurant: Name, Location, and Status if marked as favorite |
| **Post-Conditions** | The marked favorite restaurant remains accessible |
| **Expected Result** | The favorite restaurant details should appear, and the restaurant should be marked with a heart icon on the map |
| **Actual Result** | The favorite restaurant details successfully appear, and the restaurant is marked with a heart icon on the map |
| **Pass/Fail** | Pass |

---

## Test Case TC_009: Location Detection

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_009 |
| **Test Case Scenario** | Location Detection |
| **Description** | Test if the system accurately detects the user's current location using Google Maps API |
| **Pre-Conditions** | The user's device has GPS/location services enabled and browser has granted location permissions |
| **Test Steps** | 1. Navigate to the Chat or Home page<br>2. Grant location permissions when prompted<br>3. Verify location is detected<br>4. Check that nearby restaurants are displayed based on location |
| **Test Data** | User Location: [To be captured during test] |
| **Post-Conditions** | User's current coordinates are stored and used for recommendations |
| **Expected Result** | The system successfully detects and uses the user's real-time location coordinates to show nearby restaurants |
| **Actual Result** | The system successfully detects and uses the user's real-time location coordinates to show nearby restaurants |
| **Pass/Fail** | Pass |

---

## Test Case TC_010: Distance-based Filtering

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_010 |
| **Test Case Scenario** | Distance-based Filtering |
| **Description** | Test if the system filters restaurant recommendations based on distance from user location within a specified radius using Haversine formula |
| **Pre-Conditions** | User location is detected and multiple restaurants exist at varying distances |
| **Test Steps** | 1. Set search radius (e.g., 5 km)<br>2. Search for nearby restaurants<br>3. Verify all displayed restaurants are within the specified radius<br>4. Confirm distance values displayed are accurate |
| **Test Data** | Search Radius: 5 km<br>User Location: Detected automatically |
| **Post-Conditions** | Only restaurants within the specified radius are displayed |
| **Expected Result** | The system correctly filters and displays only restaurants within the specified distance radius from the user's location. Distance values are accurate |
| **Actual Result** | The system correctly filters and displays only restaurants within the specified distance radius from the user's location. Distance values are accurate |
| **Pass/Fail** | Pass |

---

## Test Case TC_011: Customizable Radius Parameter

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_011 |
| **Test Case Scenario** | Customizable Radius Parameter |
| **Description** | Test if users can modify the search radius (e.g., 1 km, 5 km, 10 km) and the system correctly applies this parameter to filter restaurant recommendations |
| **Pre-Conditions** | User location is available and multiple restaurants exist at varying distances |
| **Test Steps** | 1. Set search radius to 1 km<br>2. Search for nearby restaurants<br>3. Note the number of results<br>4. Change radius to 10 km<br>5. Search again and verify more results appear<br>6. Confirm all results are within the specified radius |
| **Test Data** | Test 1: Radius = 1 km<br>Test 2: Radius = 5 km<br>Test 3: Radius = 10 km |
| **Post-Conditions** | Recommendations are filtered according to the selected radius parameter |
| **Expected Result** | The system correctly filters restaurants based on the selected radius. A 1 km search returns fewer results than a 10 km search, and all displayed restaurants are within the specified distance |
| **Actual Result** | The system correctly filters restaurants based on the selected radius. A 1 km search returns fewer results than a 10 km search, and all displayed restaurants are within the specified distance |
| **Pass/Fail** | Pass |

---

## Test Case TC_012: Profile and Preferences

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_012 |
| **Test Case Scenario** | Profile and Preferences |
| **Description** | Users can update their profile information and set dietary preferences, restrictions, and health conditions |
| **Pre-Conditions** | The user must be logged in |
| **Test Steps** | 1. Login<br>2. Navigate to Preferences page<br>3. Select dietary restrictions (e.g., Vegetarian, Vegan, Gluten-Free)<br>4. Select health conditions if applicable<br>5. Save preferences |
| **Test Data** | Dietary Restrictions: Vegetarian, Dairy-Free<br>Health Conditions: Diabetes |
| **Post-Conditions** | User preferences are saved and used to filter recommendations |
| **Expected Result** | The preferences are successfully saved, and future recommendations will consider these preferences |
| **Actual Result** | The preferences are successfully saved, and future recommendations consider these preferences |
| **Pass/Fail** | Pass |

---

## Test Case TC_013: Preference-based Filtering

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_013 |
| **Test Case Scenario** | Preference-based Filtering |
| **Description** | Test if the chatbot automatically uses stored dietary preferences from user profile to filter recommendations without requiring the user to mention them |
| **Pre-Conditions** | User has stored dietary preferences in their profile (e.g., Vegan) |
| **Test Steps** | 1. Login as user with stored "Vegan" preference<br>2. Navigate to Chat Interface<br>3. Enter query: "Find me nearby restaurants" (without mentioning dietary restrictions)<br>4. Verify recommendations |
| **Test Data** | Stored Preference: Vegan<br>Query: "Find me nearby restaurants" |
| **Post-Conditions** | Recommendations are filtered based on stored preferences |
| **Expected Result** | The chatbot automatically retrieves and applies the "Vegan" preference, and all recommended restaurants comply with vegan dietary requirements |
| **Actual Result** | The chatbot automatically retrieves and applies the "Vegan" preference, and all recommended restaurants comply with vegan dietary requirements |
| **Pass/Fail** | Pass |

---

## Test Case TC_014: Health Condition Recognition

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_014 |
| **Test Case Scenario** | Health Condition Recognition |
| **Description** | Test if the system recognizes health conditions mentioned in chat and adjusts meal recommendations accordingly |
| **Pre-Conditions** | The user must be logged in and location services are enabled |
| **Test Steps** | 1. Login<br>2. Navigate to Chat Interface<br>3. Enter query: "I have diabetes, recommend suitable meals nearby"<br>4. Submit the query |
| **Test Data** | Query: "I have diabetes, recommend suitable meals nearby" |
| **Post-Conditions** | Health condition is recognized and used to filter recommendations |
| **Expected Result** | The chatbot recognizes "diabetes" as a health condition and filters restaurant recommendations to exclude high-sugar options or suggest diabetic-friendly meals |
| **Actual Result** | The chatbot successfully recognizes "diabetes" as a health condition and filters restaurant recommendations to exclude high-sugar options or suggest diabetic-friendly meals |
| **Pass/Fail** | Pass |

---

## Test Case TC_015: Personalized Recommendations

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_015 |
| **Test Case Scenario** | Personalized Recommendations |
| **Description** | Test if the system generates personalized recommendations based on user's past interactions and preferences learned over time |
| **Pre-Conditions** | User has performed multiple interactions and selections over time, building a preference history |
| **Test Steps** | 1. Login as user with interaction history<br>2. Navigate to Chat Interface<br>3. Enter vague query: "What should I eat?"<br>4. Review recommendations |
| **Test Data** | Query: "What should I eat?"<br>User History: Previous selections of Italian cuisine and mid-range price restaurants |
| **Post-Conditions** | System generates recommendations based on learned preferences |
| **Expected Result** | The chatbot analyzes user's past behavior and proactively recommends restaurants matching learned preferences (e.g., Italian cuisine, preferred price range) even without explicit mention |
| **Actual Result** | The chatbot successfully analyzes user's past behavior and proactively recommends restaurants matching learned preferences (e.g., Italian cuisine, preferred price range) even without explicit mention |
| **Pass/Fail** | Pass |

---

## Test Case TC_016: Page Navigation

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_016 |
| **Test Case Scenario** | Page Navigation |
| **Description** | Verify that users can smoothly navigate between all main application pages without encountering errors |
| **Pre-Conditions** | Website is available and fully loaded |
| **Test Steps** | Users can navigate between pages (Home, Chat, Preferences, Account, etc.) smoothly |
| **Test Data** | Pages: Home, Chat, Account, and Preferences<br>Navigation: Buttons and Menu |
| **Post-Conditions** | Session remains active throughout navigation |
| **Expected Result** | There shouldn't be any errors or "page not found" errors when navigating between pages |
| **Actual Result** | Successfully checked, there are no errors or "page not found" errors when navigating between pages |
| **Pass/Fail** | Pass |

---

## Test Case TC_017: Responsiveness (Desktop/Mobile)

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_017 |
| **Test Case Scenario** | Responsiveness (Desktop/Mobile) |
| **Description** | Verify that the website is responsive and functions correctly across desktop and mobile devices, ensuring smooth performance, proper layout adjustment, and accessibility of all features |
| **Pre-Conditions** | The user can access a desktop device and mobile device |
| **Test Steps** | Access the website on desktop and mobile devices, navigate through different pages and features |
| **Test Data** | Device: Mobile and Desktop Device<br>Browsers: Chrome, Brave, OperaGX, MozillaFox, and MSEdge |
| **Post-Conditions** | The website remains functional and responsive after navigation |
| **Expected Result** | The website should run smoothly without lags or delays during loading times, and all features should be accessible on both desktop and mobile |
| **Actual Result** | The website successfully runs smoothly without lags or delays during loading times, and all features are accessible on both desktop and mobile |
| **Pass/Fail** | Pass |

---

## Test Case TC_018: Browser Compatibility

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_018 |
| **Test Case Scenario** | Browser Compatibility |
| **Description** | Verify that the website works consistently across different browsers without performance issues or feature failures |
| **Pre-Conditions** | The website is available in any browser platform |
| **Test Steps** | Test the website on different browsers (Chrome, Firefox, Edge) |
| **Test Data** | Browsers: Google Chrome, Mozilla Fox, MSEdge |
| **Post-Conditions** | No browser specific errors are observed |
| **Expected Result** | No lags, and no delays - all features should work consistently across all browsers |
| **Actual Result** | Successfully no lags, and no delays - all features work consistently across all browsers |
| **Pass/Fail** | Pass |

---

## Test Case TC_019: Performance (Speed / Stability)

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_019 |
| **Test Case Scenario** | Performance (Speed / Stability) |
| **Description** | Evaluate the website's loading speed and stability while navigating through different pages and features |
| **Pre-Conditions** | User is connected to a stable internet connection |
| **Test Steps** | Navigate through different pages and features, measure loading times |
| **Test Data** | Pages and Features: Home pages, chat interfaces, restaurant recommendations, favorites panel |
| **Post-Conditions** | Website remains stable during navigation |
| **Expected Result** | No lags, and no delays |
| **Actual Result** | Successfully no lags, and no delays |
| **Pass/Fail** | Pass |

---

## Test Case TC_020: Internet Connectivity

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_020 |
| **Test Case Scenario** | Internet Connectivity |
| **Description** | Verify that the website functions properly under different internet connection speeds (slow, normal, and fast) without interruptions, lags, or delays while loading content or interacting with features |
| **Pre-Conditions** | Website is deployed and accessible |
| **Test Steps** | Test the website functionality with different internet connection speeds (slow, normal, fast) |
| **Test Data** | Internet Speed: Slow 2G, Normal 4G, Fast 5G |
| **Post-Conditions** | Website loads successfully under all speeds |
| **Expected Result** | No interruptions, no lags, and no delays in loading content or interacting with features |
| **Actual Result** | Loads successfully without interruptions, lags, or delays in loading content or interacting with features |
| **Pass/Fail** | Pass |

---

## Test Case TC_021: Integration Test - Complete Recommendation Flow

| Field | Value |
|-------|-------|
| **Test Case ID** | TC_021 |
| **Test Case Scenario** | Integration Test - Complete Recommendation Flow |
| **Description** | End-to-end test verifying all system components work together: chatbot processes natural language, location is detected, preferences are applied, and personalized recommendations are displayed |
| **Pre-Conditions** | User has account with stored dietary preferences and location services enabled |
| **Test Steps** | 1. User logs in<br>2. System detects location<br>3. User queries: "Find me Italian food"<br>4. System processes query and applies stored preferences<br>5. Recommendations display on map and results panel |
| **Test Data** | Stored Preferences: Vegan<br>Query: "Find me Italian food"<br>Location: Detected automatically |
| **Post-Conditions** | Integrated system delivers personalized, location-based recommendations |
| **Expected Result** | All components work seamlessly: chatbot extracts intent, location is detected, preferences are applied, and recommendations are displayed on the map with all relevant details |
| **Actual Result** | All components work seamlessly: chatbot extracts intent, location is detected, preferences are applied, and recommendations are displayed on the map with all relevant details |
| **Pass/Fail** | Pass |

---