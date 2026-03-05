# FoodieSaur - System Documentation

**Personalized Location-Based Food Assistant**  
**Tagline: "Mapping Your Next Bite"**

---

## Table of Contents

1. [External Interfaces Requirements](#external-interfaces-requirements)
2. [Tech Stack and Tools](#tech-stack-and-tools)
3. [System Architecture](#system-architecture)
4. [Database Schema](#database-schema)
5. [Data Flow Diagrams](#data-flow-diagrams)

---

## External Interfaces Requirements

### 1. Hardware Interfaces

#### 1.1 Client-Side Hardware Requirements
- **GPS/Location Services**: The application requires access to device GPS for location-based restaurant recommendations
  - **Interface**: HTML5 Geolocation API
  - **Characteristics**: 
    - Real-time location tracking
    - Accuracy: ±10 meters (typical)
    - Permission-based access (user consent required)
    - Fallback to manual address input if GPS unavailable

- **Display Requirements**:
  - **Desktop**: Minimum 1280x720 resolution
  - **Mobile**: Responsive design supporting 320px - 768px width
  - **Tablet**: Optimized for 768px - 1024px width
  - **Touch Interface**: Full support for touch gestures on mobile/tablet devices

- **Network Connectivity**:
  - **Interface**: HTTP/HTTPS protocols
  - **Requirements**: 
    - Minimum 3G connection for basic functionality
    - 4G/WiFi recommended for optimal Google Maps performance
    - Offline mode: Limited (cached favorites only)

### 2. External System Interfaces

#### 2.1 Google Maps Platform API
- **Interface Type**: REST API (HTTP/HTTPS)
- **Protocol**: JSON over HTTP
- **Authentication**: API Key-based authentication
- **Endpoints Used**:
  - **Places API** (`/maps/api/place/search`, `/maps/api/place/details`)
    - Purpose: Restaurant search, place details, photos
    - Request Rate: Standard plan limits apply
    - Response Format: JSON
    - Data Flow: Bidirectional (request → response)
  
  - **Geocoding API** (`/maps/api/geocode/json`)
    - Purpose: Convert addresses to coordinates
    - Request Rate: Standard plan limits
    - Response Format: JSON
  
  - **JavaScript API** (Client-side)
    - Purpose: Interactive map rendering, markers, info windows
    - Load: Via `<script>` tag with API key
    - Data Flow: Client-side rendering

- **Characteristics**:
  - **Request Format**: Query parameters or POST body (JSON)
  - **Response Format**: JSON
  - **Error Handling**: HTTP status codes (200, 400, 403, 404, 500)
  - **Rate Limiting**: Per API key quotas
  - **Caching**: Client-side caching of place details

- **Configuration**:
  ```json
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
  }
  ```

#### 2.2 Google Cloud Dialogflow API
- **Interface Type**: gRPC/REST API
- **Protocol**: gRPC (primary), REST (fallback)
- **Authentication**: Service Account JSON credentials
- **Endpoints Used**:
  - **Sessions API** (`projects/{project}/agent/sessions/{session}:detectIntent`)
    - Purpose: Natural language understanding, intent detection, entity extraction
    - Request Format: Protocol Buffers (protobuf) or JSON
    - Response Format: Protocol Buffers or JSON
    - Data Flow: Bidirectional (user message → Dialogflow → intent/entities → application)

- **Characteristics**:
  - **Language**: English (en-US)
  - **Session Management**: Session-based context tracking
  - **Intent Types**:
    - `FoodRecommendation`: General food recommendations
    - `PersonalizedFoodRecommendation`: User-specific recommendations
    - `RestaurantSearch`: Location-based restaurant search
    - `Greeting`: User greetings
    - `Default Fallback`: Unrecognized intents
  
  - **Entity Extraction**:
    - Food types, cuisine types, location, price range, dietary restrictions
    - Extracted from user messages automatically
  
  - **Error Handling**: gRPC status codes, exception handling in C#

- **Configuration**:
  ```json
  "Dialogflow": {
    "ProjectId": "extreme-braid-457405-r9",
    "CredentialsPath": "secrets/extreme-braid-457405-r9-393a6a0846ca.json"
  }
  ```

#### 2.3 Google OAuth 2.0
- **Interface Type**: OAuth 2.0 Authorization Flow
- **Protocol**: HTTPS
- **Authentication**: Client ID and Client Secret
- **Flow**:
  1. User clicks "Sign in with Google"
  2. Redirect to Google authorization endpoint
  3. User grants permissions
  4. Google redirects back with authorization code
  5. Application exchanges code for access token
  6. Retrieve user profile information
  7. Create/update user account

- **Endpoints**:
  - Authorization: `https://accounts.google.com/o/oauth2/v2/auth`
  - Token: `https://oauth2.googleapis.com/token`
  - User Info: `https://www.googleapis.com/oauth2/v2/userinfo`

- **Scopes**: `profile`, `email`

- **Configuration**:
  ```json
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
  ```

#### 2.4 Facebook OAuth 2.0
- **Interface Type**: OAuth 2.0 Authorization Flow
- **Protocol**: HTTPS
- **Authentication**: App ID and App Secret
- **Flow**: Similar to Google OAuth
- **Endpoints**:
  - Authorization: `https://www.facebook.com/v18.0/dialog/oauth`
  - Token: `https://graph.facebook.com/v18.0/oauth/access_token`
  - User Info: `https://graph.facebook.com/me?fields=id,name,email`

- **Configuration**:
  ```json
  "Authentication": {
    "Facebook": {
      "AppId": "1443899073563716",
      "AppSecret": "YOUR_FACEBOOK_APP_SECRET"
    }
  }
  ```

#### 2.5 ScrapingBee API
- **Interface Type**: REST API
- **Protocol**: HTTPS
- **Authentication**: API Key (header-based)
- **Purpose**: Web scraping for restaurant information when Google Maps data is insufficient
- **Endpoint**: `https://app.scrapingbee.com/api/v1/`
- **Request Format**: Query parameters
- **Response Format**: HTML or JSON
- **Rate Limiting**: Based on subscription plan

- **Configuration**:
  ```json
  "ScrapingBee": {
    "ApiKey": "YOUR_SCRAPINGBEE_API_KEY"
  }
  ```

#### 2.6 SQL Server Database
- **Interface Type**: Database Connection (TDS Protocol)
- **Protocol**: Tabular Data Stream (TDS) over TCP/IP
- **Authentication**: SQL Server Authentication (Username/Password)
- **Connection String Format**:
  ```
  Server={server};Database={database};User Id={user};Password={password};
  Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
  ```
- **Data Flow**: Bidirectional (CRUD operations)
- **ORM**: Entity Framework Core
- **Migrations**: Code-first migrations for schema management

- **Configuration**:
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER; Database=YOUR_DATABASE; User Id=YOUR_USER; Password=YOUR_PASSWORD; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;"
  }
  ```

### 3. Communication Interfaces

#### 3.1 HTTP/HTTPS Communication
- **Protocol**: HTTP/1.1, HTTPS (TLS 1.2+)
- **Methods**: GET, POST, PUT, DELETE
- **Content Types**: 
  - `application/json` (API responses)
  - `application/x-www-form-urlencoded` (form submissions)
  - `text/html` (view rendering)
  - `multipart/form-data` (file uploads, if applicable)

#### 3.2 WebSocket Communication (Future Enhancement)
- **Status**: Not currently implemented
- **Potential Use**: Real-time chat updates, live location tracking

#### 3.3 AJAX/Fetch API
- **Purpose**: Asynchronous client-server communication
- **Libraries**: jQuery AJAX, native Fetch API
- **Use Cases**: 
  - Chat message sending/receiving
  - Restaurant search without page reload
  - Favorites management
  - Proactive food suggestions

### 4. User Interface (GUI) Characteristics

#### 4.1 Design Principles
- **Style**: Glassmorphism with backdrop-blur effects
- **Color Scheme**:
  - **Primary Brand**: Green (#47682C)
  - **Accents**: Gold (#F7B32B)
  - **AI/Chat Elements**: Blue (#196EF3)
  - **User Messages**: Cream (#FFF5E1)
  - **Favorites/Highlights**: Red-Pink (#FE395C)

- **Typography**:
  - Font Family: Clean sans-serif (system fonts)
  - Headings: #47682C (Green)
  - Body Text: Dark gray/black
  - Chat Interface: Monospace for code/data display

#### 4.2 Layout Structure
- **Full-Screen Google Map Background**: Primary visual element
- **Three Main Panels**:
  1. **Left Panel**: Restaurant search results and favorites
     - Glassmorphism design with backdrop-blur
     - Rounded corners (rounded-2xl)
     - Scrollable content
     - Responsive: Collapses on mobile
  
  2. **Right Panel**: AI chat interface with FoodieSaur assistant
     - Chat bubbles: User (cream background) and AI (green-tinted)
     - Typing indicators with three-dot pulse animation
     - Floating input bar at bottom
     - Message history scrollable
  
  3. **Top-Center Card**: Restaurant details overlay
     - Appears when restaurant marker clicked
     - Shows name, rating, address, photos
     - "View Details" button with hover effects

#### 4.3 UI Components

**Chat Interface**:
- **User Messages**: 
  - Background: `bg-[#FFF5E1]` (Cream)
  - Text Color: `text-[#47682C]` (Green)
  - Alignment: Right side
  - Border Radius: `rounded-2xl`
  
- **AI Messages**:
  - Background: `bg-[#47682C]/10` (Green with 10% opacity)
  - Text Color: Dark gray/black
  - Alignment: Left side
  - Border Radius: `rounded-2xl`
  - FoodieSaur mascot icon (dinosaur with map pin head)

- **Input Bar**:
  - Floating design at bottom
  - Glassmorphism effect
  - Send button with hover effects
  - Health/Diet mode toggle

**Map Interface**:
- **Markers**: Custom styled restaurant markers
- **Info Windows**: Custom styled (removed default white bubble)
- **Controls**: Zoom, pan, fullscreen
- **Search Bar**: Integrated in left panel

**Restaurant Cards**:
- **Hover Effects**: `hover:scale-105` (slight scale up)
- **Shadows**: Soft shadows for depth
- **Animations**: `slideDown` animation for top card
- **Buttons**: Blue background on hover (`#2563eb`)

#### 4.4 Responsive Design
- **Mobile Breakpoints**:
  - `< 768px`: Mobile layout
  - Separate `ChatMobile.cshtml` view
  - Collapsible panels
  - Touch-optimized buttons
  
- **Tablet Breakpoints**:
  - `768px - 1024px`: Tablet layout
  - Adjusted panel widths
  - Optimized map controls

- **Desktop Breakpoints**:
  - `> 1024px`: Full desktop layout
  - Three-panel layout
  - Hover effects enabled

#### 4.5 Accessibility Features
- **Contrast Ratios**: WCAG AA compliant
- **Keyboard Navigation**: Full keyboard support
- **Screen Reader Support**: Semantic HTML, ARIA labels
- **Focus Indicators**: Visible focus states
- **Alt Text**: Images include descriptive alt text

#### 4.6 Animations & Interactions
- **Smooth Transitions**: CSS transitions for state changes
- **Loading States**: Typing indicators, skeleton loaders
- **Hover Effects**: Scale transforms, color changes
- **Click Feedback**: Visual feedback on button clicks
- **Scroll Animations**: Smooth scrolling in chat and search results

---

## Tech Stack and Tools

### Backend Technologies

#### 1. Framework & Runtime
- **ASP.NET Core 8.0** (MVC Pattern)
  - **Purpose**: Web application framework
  - **Features Used**:
    - MVC (Model-View-Controller) architecture
    - Dependency Injection
    - Middleware pipeline
    - Session management
    - Authentication & Authorization
  
- **.NET 8.0 Runtime**
  - **Language**: C# 12.0
  - **Target Framework**: `net8.0`

#### 2. Database & ORM
- **SQL Server**
  - **Version**: SQL Server (cloud-hosted)
  - **Purpose**: Primary data storage
  - **Connection**: Entity Framework Core
  
- **Entity Framework Core 9.0.5**
  - **Purpose**: Object-Relational Mapping (ORM)
  - **Features Used**:
    - Code-first migrations
    - LINQ queries
    - Change tracking
    - Relationship configuration
    - Decimal precision configuration

#### 3. Authentication & Security
- **BCrypt.Net-Next 4.0.3**
  - **Purpose**: Password hashing
  - **Algorithm**: BCrypt
  
- **Microsoft.AspNetCore.Authentication.Cookies**
  - **Purpose**: Cookie-based session authentication
  - **Configuration**: 30-minute sliding expiration
  
- **Microsoft.AspNetCore.Authentication.Google 8.0.0**
  - **Purpose**: Google OAuth integration
  
- **Microsoft.AspNetCore.Authentication.Facebook 8.0.0**
  - **Purpose**: Facebook OAuth integration

#### 4. External API Integration
- **Google.Cloud.Dialogflow.V2 4.26.0**
  - **Purpose**: Dialogflow API client
  - **Protocol**: gRPC/Protocol Buffers
  
- **Microsoft.Extensions.Http 8.0.0**
  - **Purpose**: HTTP client factory for external API calls

#### 5. Data Processing
- **Newtonsoft.Json 13.0.3**
  - **Purpose**: JSON serialization/deserialization
  - **Use Cases**: API responses, configuration data
  
- **HtmlAgilityPack 1.11.54**
  - **Purpose**: HTML parsing for web scraping
  - **Use Cases**: Restaurant data extraction

#### 6. Development Tools
- **Microsoft.EntityFrameworkCore.Tools 9.0.5**
  - **Purpose**: EF Core migrations and scaffolding
  - **Commands**: `dotnet ef migrations add`, `dotnet ef database update`

- **Ngrok.AspNetCore 1.0.6**
  - **Purpose**: Local development tunneling (for OAuth callbacks)

### Frontend Technologies

#### 1. Styling Framework
- **TailwindCSS 4.1.7**
  - **Purpose**: Utility-first CSS framework
  - **Configuration**: `tailwind.config.js`
  - **Build Process**: NPM script (`build:css`)
  - **Output**: `wwwroot/css/output.css` (minified)
  
- **Custom CSS** (`wwwroot/css/site.css`)
  - **Purpose**: Base styles, custom animations
  - **Integration**: TailwindCSS processes this file

#### 2. JavaScript Libraries
- **jQuery 3.x** (via lib/jquery)
  - **Purpose**: DOM manipulation, AJAX requests
  - **Use Cases**: Chat message sending, dynamic content updates
  
- **jQuery Validation** (via lib/jquery-validation)
  - **Purpose**: Form validation
  - **Use Cases**: Login/registration forms
  
- **jQuery Validation Unobtrusive**
  - **Purpose**: Server-side validation integration

#### 3. Google Maps Integration
- **Google Maps JavaScript API** (Client-side)
  - **Purpose**: Interactive map rendering
  - **Load Method**: `<script>` tag with API key
  - **Features Used**:
    - Map rendering
    - Markers
    - Info windows
    - Places API integration
    - Geocoding

#### 4. Frontend Build Tools
- **Node.js & NPM**
  - **Purpose**: Package management and build scripts
  - **Configuration**: `package.json`
  - **Scripts**: `build:css` for TailwindCSS compilation

### Development Environment

#### 1. IDE & Editors
- **Visual Studio / Visual Studio Code**
  - **Extensions**: C# Dev Kit, EF Core tools
  
#### 2. Version Control
- **Git**
  - **Repository**: Local/Remote Git repository

#### 3. Package Management
- **NuGet** (Backend)
  - **Configuration**: `Capstone.csproj`
  - **Package Sources**: nuget.org
  
- **NPM** (Frontend)
  - **Configuration**: `package.json`
  - **Registry**: npmjs.com

### Deployment & Hosting

#### 1. Web Server
- **IIS (Internet Information Services)** or **Kestrel**
  - **Purpose**: ASP.NET Core hosting
  
#### 2. Database Hosting
- **SQL Server** (Cloud-hosted)
  - **Provider**: DatabaseASP.net
  - **Connection**: Encrypted (TLS)

#### 3. Configuration Files
- **appsettings.json**: Production configuration
- **appsettings.Development.json**: Development overrides
- **web.config**: IIS-specific configuration

### Testing & Quality Assurance

#### 1. Logging
- **Microsoft.Extensions.Logging**
  - **Purpose**: Application logging
  - **Providers**: Console, Debug
  - **Levels**: Information, Warning, Error

#### 2. Error Handling
- **ASP.NET Core Exception Handling Middleware**
  - **Purpose**: Global error handling
  - **Configuration**: Development vs Production error pages

### Additional Tools & Services

#### 1. Caching
- **Microsoft.Extensions.Caching.Memory**
  - **Purpose**: In-memory caching for performance
  - **Use Cases**: Large dataset caching (FoodTypes, etc.)

#### 2. Session Management
- **ASP.NET Core Session**
  - **Purpose**: User session state
  - **Storage**: In-memory (default)
  - **Timeout**: 30 minutes idle timeout

#### 3. HTTP Context Access
- **Microsoft.AspNetCore.Http**
  - **Purpose**: Access to HTTP context, session, user claims

---

## System Architecture

### Architecture Pattern
**MVC (Model-View-Controller)** with Service Layer

### Layers

1. **Presentation Layer**
   - Razor Views (.cshtml)
   - JavaScript (site.js)
   - CSS (TailwindCSS + custom)

2. **Controller Layer**
   - MVC Controllers (HomeController, ChatController, etc.)
   - API Controllers (ChatController API endpoints)

3. **Service Layer**
   - DialogflowService
   - UserBehaviorService
   - ScrapingBeeService

4. **Data Access Layer**
   - AppDbContext (Entity Framework Core)
   - Repository pattern (implicit via DbContext)

5. **External Services Layer**
   - Google Maps API
   - Dialogflow API
   - OAuth Providers
   - ScrapingBee API

---

## Database Schema

See `database_erd.dbml` for complete Entity Relationship Diagram.

### Key Entities

1. **Users**: User accounts and authentication
2. **UserProfiles**: User demographic and preference data
3. **FoodTypes**: Master catalog of food types with nutritional properties
4. **UserFoodTypes**: User food preferences (onboarding selections)
5. **ChatSessions & ChatMessages**: Chat history
6. **UserBehaviors**: User interaction tracking
7. **UserPreferencePatterns**: Learned behavioral patterns
8. **DietaryRestrictions & UserDietaryRestrictions**: Dietary constraints
9. **HealthConditions & UserHealthConditions**: Health-related data
10. **UserFavoriteRestaurants**: User's favorite places from Google Maps

---

## Data Flow Diagrams

See `system_data_flow_diagram.dbml` for detailed data flow patterns.

### Key Data Flows

1. **User Authentication Flow**: OAuth/Login → Session → Redirect
2. **Chat Message Flow**: UI → Controller → Dialogflow → Recommendation Engine → Response
3. **Food Recommendation Flow**: Query → User Data → Prescriptive Algorithm → Google Maps → Results
4. **Behavior Learning Flow**: Interaction → Tracking → Pattern Analysis → Pattern Updates
5. **Restaurant Search Flow**: Location → Google Maps API → Filtering → Display

---

## Diagrams

### Entity Relationship Diagram
**File**: `database_erd.dbml`  
**Tool**: DBdiagram.io  
**Description**: Complete database schema with all tables, relationships, and constraints.

### System Data Flow Diagram
**File**: `system_data_flow_diagram.dbml`  
**Tool**: DBdiagram.io (adapted format)  
**Description**: System architecture, external interfaces, and data flow patterns.

---

## Additional Resources

- **Design Reference**: See `DESIGN_REFERENCE_GUIDE.md`
- **Developer Quick Reference**: See `DEVELOPER_QUICK_REFERENCE.md`
- **Dialogflow Setup**: See `DIALOGFLOW_SETUP_GUIDE.md`
- **OAuth Setup**: See `OAUTH_SETUP_GUIDE.md`

---

**Document Version**: 1.0  
**Last Updated**: December 2024  
**Maintained By**: FoodieSaur Development Team

