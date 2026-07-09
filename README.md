# AI Timesheet Generator

An AI-powered timesheet generator that automatically builds weekly employee
timesheets from Git commits, Jira/Azure Boards tickets, pull requests, and
Outlook/Teams calendar meetings — matching the architecture in
`AI TIMESHEET GENERATOR – END TO END ARCHITECTURE`.

```
Employees connect their tools → backend pulls activity for the week →
AI engine turns raw commits/tickets/meetings into professional daily
entries + a weekly summary → employee reviews & submits →
manager approves → data stored for reporting.
```

## Solution structure

```
AITimesheetGenerator/
├── backend/                          ASP.NET Core 8 Web API (C#) — single project
│   ├── AITimesheet.sln               <- open this in Visual Studio / Rider
│   └── AITimesheet.API/
│       ├── Controllers/
│       │   ├── AuthController.cs         Step 1: login / user upsert
│       │   ├── IntegrationController.cs  Step 2: connect/disconnect tools
│       │   ├── TimesheetController.cs    Step 3-6: generate/review/submit
│       │   ├── ApprovalController.cs     Step 7: manager approve/reject
│       │   ├── ActivityController.cs     raw activity feed
│       │   ├── AnalyticsController.cs    Step 8: team dashboards
│       │   └── ChatController.cs         AI chat ("what did I work on...")
│       ├── DTOs/
│       ├── Entities/                  User, Connection, Activity, Timesheet, ...
│       ├── Interfaces/                IIntegrationService, IAiTimesheetService, ...
│       ├── Data/AppDbContext.cs       EF Core + Npgsql (PostgreSQL)
│       ├── Repositories/
│       ├── Integrations/              GitHub, Azure DevOps, Jira, MS Graph Calendar
│       ├── AI/OpenAiTimesheetService.cs   Azure OpenAI / OpenAI prompt + parsing
│       ├── Program.cs                 DI wiring, CORS, EF migration on boot
│       └── appsettings.json           <-- put your PostgreSQL + OpenAI keys here
│
├── frontend/                         React + TypeScript + Vite
│   └── src/
│       ├── pages/                    Login, ConnectAccounts, Dashboard,
│       │                             TimesheetReview, Analytics, ChatAssistant
│       ├── components/               Navbar, TimesheetTable, HourChart
│       ├── api/                      Axios client + typed API calls
│       └── types/                    Shared TS interfaces
│
├── database/
│   └── schema.sql                    Raw PostgreSQL schema (reference / manual setup)
│
└── README.md                         you are here
```

This mirrors the architecture diagram's layers 1:1:
Frontend (React) → API Gateway/Controllers → Application Services →
AI Processing Layer → Data Storage (now **PostgreSQL** instead of SQL Server).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org)
- [PostgreSQL 15+](https://www.postgresql.org/download/) running locally (or a hosted instance)
- (Optional for real AI output) an Azure OpenAI or OpenAI API key — without one, the
  backend automatically falls back to a deterministic rule-based generator, so the
  demo **always works** even with zero external accounts connected.

## 1. Database setup (PostgreSQL)

Create the database:

```bash
createdb ai_timesheet_db
```

Set your connection string in `backend/AITimesheet.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=ai_timesheet_db;Username=postgres;Password=postgres"
}
```

You have two options to create the schema:

**Option A — EF Core migrations (recommended)**

```bash
cd backend/AITimesheet.API
dotnet tool install --global dotnet-ef   # once, if not already installed
dotnet ef migrations add InitialCreate
dotnet ef database update
```

The API also calls `db.Database.Migrate()` on startup, so once a migration exists it
will auto-apply on every `dotnet run`.

**Option B — raw SQL**

```bash
psql -d ai_timesheet_db -f database/schema.sql
```

## 2. Run the backend

```bash
cd backend/AITimesheet.API
dotnet restore
dotnet run
```

API runs at `http://localhost:5080` with Swagger UI at `http://localhost:5080/swagger`.

### Configure integrations (optional but recommended for the demo)

Edit `appsettings.json` (or use `dotnet user-secrets` for real credentials):

```json
"AzureOpenAI": { "Endpoint": "https://<resource>.openai.azure.com", "ApiKey": "...", "Deployment": "gpt-4o" },
"AzureDevOps": { "Organization": "your-org", "Project": "your-project" },
"Jira": { "SiteUrl": "https://your-domain.atlassian.net" },
"AzureAd": { "TenantId": "...", "ClientId": "...", "ClientSecret": "..." }
```

If these are left blank, every integration and the AI engine transparently fall back
to realistic mock data (the same examples from the original spec: "Fix login
authentication issue", "ABC-123 Login Bug", "Sprint Planning", etc.) — so you can demo
the full workflow end-to-end without configuring a single external account.

## 3. Run the frontend

```bash
cd frontend
npm install
cp .env.example .env   # adjust VITE_API_BASE_URL if needed
npm run dev
```

Frontend runs at `http://localhost:5173` and proxies `/api` calls to the backend.

## 4. Demo flow

1. **Login** — enter name + work email (stands in for Microsoft SSO).
2. **Connect Accounts** — toggle on GitHub / Jira / Azure DevOps / Outlook Calendar.
3. **Dashboard** — click "Generate this week's timesheet" → backend fetches
   activity from every connected source, sends it to the AI engine, and returns
   professional daily entries + a weekly summary.
4. **Timesheet Review** — edit any AI-generated entry, then submit for approval.
5. **Analytics** (as a manager) — approve/reject submitted timesheets and view
   weekly team hour charts.
6. **AI Chat** — ask "What did I work on last Thursday?" and get a grounded answer.

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | React 18, TypeScript, Vite, React Router, Axios, Chart.js |
| Backend | ASP.NET Core 8 Web API, single project (Controllers / Entities / Interfaces / Data / Integrations / AI) |
| Database | **PostgreSQL** via EF Core + Npgsql |
| AI | Azure OpenAI / OpenAI GPT-4o (with deterministic fallback) |
| Integrations | GitHub REST API, Azure DevOps REST API, Jira REST API, Microsoft Graph |

## Notes for hackathon judges

- Every integration and the AI service degrade gracefully to realistic mock data,
  so the full user journey works even with zero external accounts wired up —
  useful for a live demo where you don't want to depend on conference WiFi + OAuth.
- Swap `AzureOpenAI` config for a real key to see genuine LLM-generated summaries
  instead of the rule-based fallback text.
- The backend is a single ASP.NET Core project with everything organized into
  folders (Controllers, Entities, Interfaces, Data, Integrations, AI) — simplest
  to open, build, and demo in a hackathon setting. Split it back into separate
  Core/Infrastructure class libraries later if the project grows.
