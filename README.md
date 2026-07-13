# EqualFutures

**A family financial planning hub that balances retirement security with children's education funding — and shows you the tradeoffs.**

EqualFutures treats a household's finances as one long-term planning problem. Instead of optimizing a single goal, it helps families answer questions like:

- Am I saving enough for retirement?
- How much can I afford to contribute toward each child's college?
- Are all of my children being treated *fairly*?
- If I increase retirement savings, how does that affect college funding?
- Can I retire on time while still helping my children?

The guiding principle: **retirement comes first, because you cannot borrow for it** — while still distributing educational support fairly and transparently across children.

---

## Table of contents

- [Features](#features)
- [Architecture](#architecture)
- [Technology stack](#technology-stack)
- [Project structure](#project-structure)
- [Getting started](#getting-started)
- [Running the app](#running-the-app)
- [Running the tests](#running-the-tests)
- [Database & migrations](#database--migrations)
- [The calculation engine](#the-calculation-engine)
- [The Fairness Engine](#the-fairness-engine)
- [Family collaboration](#family-collaboration)
- [Configuration](#configuration)
- [Security notes](#security-notes)
- [Roadmap](#roadmap)

---

## Features

| Module | What it does |
|--------|--------------|
| **Dashboard** | High-level snapshot: retirement readiness, education funding, net worth, investment allocation, cash flow, and explained recommendations. |
| **Family Profile** | Edit parents (ages, retirement age, income, retirement spending, Social Security, pension) and children (birth date, college start year, college type, funding target, scholarships). |
| **Assets & Liabilities** | Track investment accounts (401(k), IRA, Roth, Brokerage, HSA), education accounts (529, UTMA/UGMA, trust), other assets (bank, real estate, business), and debts (mortgage, student loan, other). |
| **Retirement Planning** | Projections, safe-withdrawal estimates, portfolio growth, inflation adjustment, and the retirement funding gap — with **interactive assumption sliders** that recompute instantly. |
| **Education Planning** | Per-child inflation-adjusted college cost, projected 529/savings, scholarships, family contribution, student responsibility, and remaining funding gap. |
| **Fairness Engine** | The differentiator — compares how much support each child receives across **all** sources under five selectable fairness metrics, making intentional-versus-accidental inequality visible. |
| **Recommendations** | Actionable, *explained* insights (increase savings, close a 529 gap, pay down high-rate debt, Roth-conversion opportunities), prioritizing retirement first. |
| **Family collaboration** | Invite a spouse or children to their own logins on the shared plan, with roles (Owner / Adult / Child) controlling edit and management rights. |

Each household is private to the signed-in user. New accounts start empty; a **sample household ("The Rivera Family")** can be loaded on demand from the Dashboard or Family Profile and cleared at any time. A plan can be **shared with other logins** (spouse, children) via invitations — see [Family collaboration](#family-collaboration).

---

## Architecture

EqualFutures follows a clean, layered architecture with a strict dependency direction. UI, business logic, and financial calculations are cleanly separated, and the calculation formulas are unit-tested in isolation.

```
┌─────────────────────────────────────────────┐
│  EqualFutures.Web  (Blazor Server + Identity) │  ← presentation only
└───────────────┬─────────────────┬─────────────┘
                │                 │
        ┌───────▼──────┐   ┌──────▼──────────────┐
        │    Core      │   │   Infrastructure     │
        │ calculations │   │  EF Core / SQLite    │
        └───────┬──────┘   └──────┬──────────────┘
                │                 │
             ┌──▼─────────────────▼──┐
             │        Domain          │  ← entities & enums, no dependencies
             └────────────────────────┘
```

- **Domain** has no dependencies.
- **Core** depends only on Domain and contains pure, side-effect-free calculation services.
- **Infrastructure** depends on Domain + Core (data access, seeding).
- **Web** depends on all three and contains only presentation concerns; a per-circuit `PlanState` service keeps components free of data-access and calculation logic.

---

## Technology stack

- **.NET 10**
- **Blazor Server** (Blazor Web App, Interactive Server render mode)
- **Entity Framework Core 10** with **SQLite**
- **ASP.NET Core Identity** (individual accounts)
- **xUnit** for unit testing
- Bootstrap + a custom lightweight theme (no heavy front-end dependencies)

---

## Project structure

```
EqualFutures.slnx
├── src/
│   ├── EqualFutures.Domain/          # Entities & enums (FinancialPlan, Parent, Child, Account, Liability, PlanAssumptions)
│   ├── EqualFutures.Core/            # Calculation services
│   │   ├── Financials/               #   FinancialMath (FV/PV/annuities)
│   │   ├── Retirement/               #   RetirementCalculator
│   │   ├── Education/                #   EducationCalculator
│   │   ├── Fairness/                 #   FairnessEngine
│   │   ├── Recommendations/          #   RecommendationEngine
│   │   └── Analysis/                 #   PlanAnalysisService (dashboard orchestration)
│   ├── EqualFutures.Infrastructure/  # FinancialDbContext, PlanService, SamplePlanFactory, migrations
│   └── EqualFutures.Web/             # Blazor Server UI + Identity
│       ├── Components/Pages/         #   Dashboard, Family, Assets, Retirement, Education, Fairness
│       ├── Components/Shared/        #   StatCard, MiniProgress
│       └── Services/PlanState.cs     #   Per-circuit plan + analysis holder
└── tests/
    └── EqualFutures.Core.Tests/      # Unit tests for the financial formulas
```

---

## Getting started

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later
- Any OS supported by .NET (Windows, macOS, Linux)

Verify your SDK:

```powershell
dotnet --version
```

### Restore & build

```powershell
git clone <your-repo-url> EqualFutures
cd EqualFutures
dotnet build EqualFutures.slnx
```

The build should complete with **0 warnings and 0 errors**.

---

## Running the app

```powershell
dotnet run --project src/EqualFutures.Web
```

Then open the URL printed in the console (e.g. `https://localhost:xxxx`).

> **Run in the Development environment.** Blazor static web assets (including `blazor.web.js`, which powers interactivity) are served from the development manifest. Running against the raw build output in the Production environment disables them. The default `dotnet run` (which uses `Properties/launchSettings.json`) already selects Development.

### First run

1. The app applies database migrations automatically on startup (both the Identity and financial contexts).
2. Click **Register** and create an account, then confirm the email address from the message sent by the configured Logic App.
3. You'll land on an empty dashboard. Choose **Set up my family** to enter your own data, or **Load sample data** to explore a fully worked example you can edit or clear.

---

## Running the tests

```powershell
dotnet test tests/EqualFutures.Core.Tests
```

The suite covers the core financial math and each calculation service:

- `FinancialMathTests` — future/present value, annuities, compounding, edge cases
- `RetirementCalculatorTests` — nest-egg projection, funding gap, guaranteed-income offset, shortfall detection
- `EducationCalculatorTests` — default costs, funding gaps, scholarships, earmarked savings
- `FairnessEngineTests` — equal vs. unequal distribution scoring, ratio metrics, single-child case

---

## Database & migrations

EqualFutures uses a single SQLite database file (`app.db`) shared by two `DbContext`s:

- **`ApplicationDbContext`** — ASP.NET Core Identity (users, logins).
- **`FinancialDbContext`** — the planning domain. It uses a separate migrations-history table (`__EFMigrationsHistory_Financial`) so the two contexts evolve independently in the same file.

Both contexts run `Database.Migrate()` on application startup, so no manual step is required.

### Creating a new migration for the financial model

```powershell
dotnet ef migrations add <Name> `
  --project src/EqualFutures.Infrastructure `
  --startup-project src/EqualFutures.Web `
  --context FinancialDbContext `
  --output-dir Data/Migrations
```

A design-time factory (`FinancialDbContextFactory`) is included so EF tooling works without a running host.

---

## The calculation engine

All projections are built from pure functions in `FinancialMath`, which use `decimal` throughout to avoid floating-point drift in monetary figures:

- `FutureValue` / `PresentValue` — single-amount compounding and discounting
- `FutureValueOfAnnuity` — a stream of level contributions
- `ProjectBalance` — starting balance + annual contributions compounded forward
- `InflateValue`, `RealRate`, `PresentValueOfAnnuityDue` — inflation and withdrawal modeling

Higher-level services compose these:

- **`RetirementCalculator`** grows retirement accounts to the earliest parent's retirement date, computes the portfolio portion of spending not covered by Social Security/pensions, and sizes the required nest egg via the safe-withdrawal rule.
- **`EducationCalculator`** inflates each child's college cost, projects earmarked education savings, and computes the family's funding gap.
- **`PlanAnalysisService`** orchestrates every module into a single dashboard-ready `PlanSummary`, keeping the UI free of financial logic.

Assumptions (inflation, expected returns, safe-withdrawal rate, planning horizon) live on the plan so they can be varied without touching calculation code — the foundation for future scenario planning.

---

## The Fairness Engine

The Fairness Engine reduces each child to a single comparable value under a chosen lens, then measures how far the household is from treating every child identically. Deliberately unequal choices remain visible via per-child deviations rather than being hidden.

Supported metrics:

- **Equal dollar amount** — same raw family contribution
- **Equal inflation-adjusted value** — same value in today's dollars
- **Equal percent of tuition** — same share of each child's cost covered
- **Equal lifetime gifts** — same cumulative real value
- **Equal after-tax benefit** — adjusted for the tax efficiency of the funding accounts

The same family can look "unfair" by dollars yet "fair" by percent-of-tuition (e.g. when one child attends a more expensive school) — the engine surfaces exactly that tradeoff so families decide intentionally.

---

## Family collaboration

A plan can be shared with the whole family, each with their own login.

### Roles

| Role | Rights |
|------|--------|
| **Owner** | Full control: edit the plan, invite/remove members, manage the family. The plan creator. |
| **Adult** | Can view and edit the plan (e.g. a spouse), but cannot manage members. |
| **Child** | Read-only access to the plan. |

### Inviting someone

1. The owner opens **Family Members** and enters the invitee's email and role.
2. A secure invite link (`/join/{token}`) is generated and emailed through the configured Logic App. The link is also shown in the UI so it can be copied if needed.
3. The invitee registers (or signs in) **with the invited email**, then opens the link to join.
4. They immediately see the shared household on their next visit.

Access is resolved by membership, never by a client-supplied id, and new accounts still start with their own private plan until they accept an invitation.

### Security

- Invitation tokens are 256-bit cryptographically random values (`RandomNumberGenerator`), stored with a unique index.
- A link is **bound to the invited email** — accepting requires the signed-in user's email to match, so a leaked link can't be used by someone else.
- Only the **owner** can invite, revoke, or remove members; owners can't be removed or remove themselves.
- Accepting is **idempotent** (re-opening a used link reports "already a member" rather than erroring).

---

## Configuration

Connection strings and settings live in `src/EqualFutures.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data/app.db;Cache=Shared"
  }
}
```

Point `DefaultConnection` at any SQLite path you prefer.

### Email (Gmail via Azure Logic App)

Transactional email (confirmation links, password resets, family invitations) is sent by
POSTing `{ "to", "subject", "body" }` to a Gmail-backed Azure Logic App HTTP trigger.
The trigger URL contains a SAS signature, so **store it as a secret — never commit it**:

```powershell
dotnet user-secrets set "Email:LogicAppUrl" "<your-logic-app-trigger-url>" --project src/EqualFutures.Web
```

If no URL is configured, the app runs normally but logs a warning instead of sending
(so email failures never break registration). Because email confirmation is required,
set this secret before registering real users.

---

## Azure App Service deployment

The app targets **.NET 10**. If your App Service runtime stack does not include .NET 10 yet, publish self-contained instead of framework-dependent:

```powershell
dotnet publish src/EqualFutures.Web -c Release -r win-x64 --self-contained true
```

Use `linux-x64` instead of `win-x64` for a Linux App Service.

SQLite writes must go to App Service's writable home directory. When the app detects Azure App Service, the default relative SQLite path (`Data/app.db`) is automatically resolved to `$HOME/data/app.db` before startup migrations run. You can also override it explicitly with an Azure App Setting:

| Setting | Value |
|---------|-------|
| `ConnectionStrings__DefaultConnection` | `Data Source=D:\home\data\app.db;Cache=Shared` on Windows App Service, or `Data Source=/home/data/app.db;Cache=Shared` on Linux App Service |
| `Email__LogicAppUrl` | Your Logic App HTTP trigger URL |
| `Email__ToField` | `to` |
| `Email__SubjectField` | `subject` |
| `Email__BodyField` | `body` |

If Azure shows **HTTP Error 500.30 - ASP.NET Core app failed to start**, check App Service **Log stream** first. The most common causes for this app are a framework-dependent deploy to an App Service without the .NET 10 runtime, or a startup migration failure caused by SQLite pointing at a read-only path.

---

## Security notes

- The transitive `SQLitePCLRaw.lib.e_sqlite3` dependency is pinned to the patched `SQLitePCLRaw.bundle_e_sqlite3` **3.0.3** to resolve advisory GHSA-2m69-gcr7-jv3q.
- Each plan is scoped to the owning Identity user; the app always resolves the household from the authenticated user's id and never from a client-supplied plan id, so users only ever see their own data (no IDOR).
- Email confirmation is **required** before login. Email is delivered through an Azure Logic App whose trigger URL is a secret kept in user-secrets/environment (`Email:LogicAppUrl`), never committed. User-supplied values in emails are HTML-encoded, and the Logic App URL is never logged.

---

## Roadmap

Planned next iterations:

- **Scenario Planning** — save and compare unlimited what-if scenarios (retire at 60 vs 65, market underperformance, private vs public college, early retirement).
- **Monte Carlo** retirement simulations.
- Social Security claiming strategies and Required Minimum Distributions.
- Estate/trust planning, insurance and tax optimization, healthcare and long-term-care projections.
- Goal tracking, document vault, and an AI financial advisor.

---

*Every recommendation supports sustainable financial security while making the tradeoffs between competing goals clear and understandable.*
