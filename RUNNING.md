# Running Last Call Motor Auctions (local setup)

This guide is for someone cloning the repo for the first time. The app is a single ASP.NET Core project that serves both MVC pages and JSON APIs.

---

## What you need installed

| Requirement | Notes |
|-------------|--------|
| **.NET SDK** | **9.0** (see `LastCallMotorAuctions.API.csproj` — `net9.0`). Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download). |
| **SQL Server** | Any edition that works with EF Core’s SQL Server provider. **LocalDB** is the default in `appsettings.json` / Development: `(localdb)\MSSQLLocalDB`. Full SQL Server is fine if you change the connection string. |
| **Git** | To clone the repository. |
| **(Optional)** **EF Core tools** | Only if you apply migrations from the CLI: `dotnet tool install --global dotnet-ef` (once per machine). |

There is **no separate Node/npm frontend** for the main app; static assets live under `wwwroot/`.

---

## 1. Clone and restore

```bash
git clone <repository-url>
cd lastcall-auctions
dotnet restore
dotnet build
```

---

## 2. Configuration files (what they are)

| File | Purpose |
|------|---------|
| `appsettings.json` | Base settings. Empty placeholders for secrets; default LocalDB connection string. |
| `appsettings.Development.json` | Used when `ASPNETCORE_ENVIRONMENT=Development`. Example CORS origins for local ports. |
| `appsettings.Production.json` | Production-style settings. **Do not commit real secrets**; rotate any keys/passwords that were ever shared. |

The app **requires a non-empty JWT signing key** at startup (`Program.cs` throws if `JWT:Key` is missing).

Optional sections in config (reserved for future or partial features):

- **`VINAudit:ApiKey`** — Referenced in docs / TODO in code; not a hard requirement for startup.
- **`KellyBlueBook:ApiKey`** — Same as above.
- **Payments** — `PaymentService.cs` notes Stripe (or similar) as TODO; not configured via a dedicated key in `appsettings.json` today.

---

## 3. Database connection string

**Key:** `ConnectionStrings:DefaultConnection`

**Default (LocalDB)** in repo config:

`Server=(localdb)\MSSQLLocalDB;Database=LastCallMotorAuctions;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`

If you use a named SQL Server instance, replace the server part (see also `User_Secrets_Setup.md` in the repo).

---

## 4. Secrets and local overrides (recommended)

The project has a **`UserSecretsId`** in the `.csproj`, so you can store secrets outside of JSON files:

```bash
cd <folder containing the .csproj>

# JWT signing key — must be a long random string (e.g. 32+ characters)
dotnet user-secrets set "JWT:Key" "<your-long-random-secret>"

# If you do not use LocalDB:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>"
```

Optional (only if you integrate these APIs later):

```bash
dotnet user-secrets set "VINAudit:ApiKey" "<key>"
dotnet user-secrets set "KellyBlueBook:ApiKey" "<key>"
```

If you **do not** set user secrets, ensure `appsettings.Development.json` (or the environment you use) contains a real **`JWT:Key`** value, or the app will fail on startup.

---

## 5. Apply database migrations

The app does **not** auto-run migrations on startup. After SQL Server is reachable and the connection string is correct:

```bash
dotnet ef database update
```

Run this from the project directory (where the `.csproj` file is). If `dotnet ef` is not found, install the global tool (see prerequisites).

---

## 6. Run the application

```bash
dotnet run
```

Or use Visual Studio / Rider with the launch profile **`https`** or **`http`** (`Properties/launchSettings.json`).

**Typical local URLs**

- HTTPS: `https://localhost:7194`
- HTTP: `http://localhost:5186`

**Environment:** Profiles set `ASPNETCORE_ENVIRONMENT` to **Development**.

---

## 7. CORS

**Key:** `CORS:AllowedOrigins` (array of origins)

In Development, example origins include `https://localhost:7194` and `http://localhost:5186`. If you add another front-end origin (different port or Live Server), add it to `appsettings.Development.json` or user secrets under the same section.

---

## 8. Useful endpoints (after running)

| URL | Purpose |
|-----|---------|
| `/` | Home (MVC). |
| `/health` | Health check (includes SQL Server check). |
| `/docs` (Development) | Redirects to OpenAPI JSON (`/openapi/v1.json`). |
| `/hubs/bidding` | SignalR endpoint for live bidding. |

---

## 9. First-time login / seeded admin

On startup, the app seeds **roles** and a default **admin** user (see `Data/AdminSeeder.cs`):

- **Email:** `admin@gmail.com`  
- **Password:** `Adminaccount1`  

**Security note:** These credentials are for **local development only**. Change or disable this pattern before any real deployment.

New users typically register through the app; **Buyer** / **Seller** roles may need to be assigned by an admin depending on your workflow.

---

## 10. Troubleshooting (quick)

| Problem | What to check |
|---------|----------------|
| Startup error about **JWT Key** | Set `JWT:Key` in user secrets or `appsettings.Development.json`. |
| **Cannot open database** / login failed | SQL Server or LocalDB running; connection string correct; run `dotnet ef database update`. |
| **401/403** from API from another origin | Add your front-end origin to `CORS:AllowedOrigins`. |
| **HTTPS certificate** warnings (dev) | Run `dotnet dev-certs https --trust` (Windows/macOS) once. |

---

## 11. Related files in this repo

- `README.md` — Project overview and high-level stack (if it mentions an older .NET version, the `.csproj` is authoritative).
- `User_Secrets_Setup.md` — Short cheat sheet for `dotnet user-secrets` commands.
