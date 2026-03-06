# FoodieSaur (Nomsaur)

Personalized Location-Based Food Assistant for recommending meals and restaurants based on user profile, behavior, location, and dietary needs.

## Project Purpose

FoodieSaur is a capstone web application that combines:

- conversational AI (Dialogflow),
- location intelligence (Google Maps/Places),
- user preference modeling, and
- health and dietary constraints

to provide food recommendations that are practical and personalized in real time.

## What You Should Know

- **System type:** ASP.NET Core MVC web application (`.NET 8`).
- **Core output:** Personalized food and nearby restaurant recommendations.
- **Personalization inputs:** onboarding preferences, dietary restrictions, health conditions, search/chat behavior, and favorites.
- **Main interface:** map + chat assistant + recommendation cards.
- **Authentication options:** local account, Google OAuth, and Facebook OAuth.

## Key Features

- AI-powered chat assistant for food recommendation flow.
- Google Maps-based restaurant lookup and display.
- User onboarding for preferences and profile context.
- Dietary restriction and health-condition-aware suggestions.
- Favorites system for saved restaurants.
- Session-based authentication and profile progression.
- Real-time direct messaging support through SignalR hub.
- Database export command for panel/demo submission.

## Technology Stack

- **Backend:** ASP.NET Core MVC, C# (`net8.0`)
- **Database:** SQL Server + Entity Framework Core
- **Frontend:** Razor Views, JavaScript/jQuery, Tailwind CSS
- **External services:** Google Maps Platform, Google Dialogflow, Google/Facebook OAuth, ScrapingBee (optional fallback scraping)

## High-Level Architecture

1. **Presentation Layer:** Razor views and client-side scripts (`Views`, `wwwroot`)
2. **Application Layer:** Controllers and service logic (`Controllers`, `Data`, `Models`)
3. **Data Layer:** SQL Server via EF Core (`AppDbContext`, `Migrations`)
4. **External Integrations:** Dialogflow, Google Maps APIs, OAuth providers

## Prerequisites

Install the following on the evaluation machine:

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [Node.js + npm](https://nodejs.org/)
- SQL Server (local or remote)
- Google Maps API key
- Dialogflow project + service account JSON
- (Optional) Google OAuth / Facebook OAuth app credentials

## Required Configuration

Update `appsettings.json` (or environment-specific config) with valid values:

- `ConnectionStrings:DefaultConnection`
- `Dialogflow:ProjectId`
- `Dialogflow:CredentialsPath`
- `GoogleMaps:ApiKey`
- `ScrapingBee:ApiKey` (optional)
- `Authentication:Google:ClientId`
- `Authentication:Google:ClientSecret`
- `Authentication:Facebook:AppId`
- `Authentication:Facebook:AppSecret`
- `Recaptcha:SiteKey`
- `Recaptcha:SecretKey`

## Web Url Link

```bash
foodiesaur.runasp.net
```

## Local Setup (Step-by-Step)

From the project root (`Capstone`):

1. Restore backend dependencies:

```bash
dotnet restore
```

2. Install frontend dependencies:

```bash
npm install
```

3. Apply database migrations:

```bash
dotnet ef database update
```

4. Build Tailwind CSS (watch mode for development):

```bash
npm run build:css
```

5. Run the web app:

```bash
dotnet run
```

By default, development launch settings include:

- `https://localhost:7217`
- `http://localhost:5081`

## Suggested Demo Flow

1. Open the app and create/sign in to an account.
2. Complete onboarding (preferences, dietary/health profile).
3. Open chat and request recommendations (example: "What should I eat nearby?").
4. Verify map markers and recommendation cards appear.
5. Save a few restaurants to favorites.
6. Repeat with a different preference context to show personalization changes.
7. (Optional) demonstrate OAuth login (Google/Facebook).

## Useful Operational Command

Export database content to JSON (for panel review or submission evidence):

```bash
dotnet run -- --export-database DatabaseExport.json
```

## Project Structure (Important Paths)

- `Program.cs` - app startup, middleware, auth, migration/seeding flow
- `Controllers/` - MVC/API request handling
- `Models/` - domain models and entities
- `Data/` - data services, DB context, data export logic
- `Views/` - Razor UI pages
- `wwwroot/` - static assets and compiled CSS
- `Migrations/` - EF Core migration history
- `SYSTEM_DOCUMENTATION.md` - full technical documentation
- `OAUTH_SETUP_GUIDE.md` - OAuth setup details
- `DIALOGFLOW_SETUP_GUIDE.md` - Dialogflow intent/setup guide

## Notes and Limitations

- External API availability and credentials directly affect recommendation quality.
- OAuth callbacks require properly registered redirect URLs.
- Location-based features depend on browser geolocation permission.
- Initial startup may seed baseline food and profile metadata if tables are empty.

## Maintainers

FoodieSaur / Nomsaur Capstone Development Team
