# AppaRently

Short-stay rental platform built with ASP.NET Core on .NET 10, with three MVC frontends and a shared infrastructure layer for persistence, seeding, and business rules.

## Prerequisites

- Docker and Docker Compose
- .NET 10 SDK, only if you want to run the solution outside containers
- PostgreSQL installed locally and running on `localhost:5433`

## Start the environment

```bash
docker compose up --build
```

That command starts:

- PostgreSQL database
- Client portal at `http://localhost:5001`
- Owner portal at `http://localhost:5002`
- SuperAdmin portal at `http://localhost:5003`

When running through Docker Compose, the database is exposed on `localhost:5433` for tools like DBeaver. If you run the app outside containers, keep using your local PostgreSQL on `localhost:5432`.

For real Gmail delivery, configure a valid Gmail app password in `Smtp:Password` or through `Smtp__Password`. Gmail will reject normal account passwords for SMTP in most setups. The SMTP settings used by the app are `Host`, `Port`, `EnableSsl`, `UserName`, `Password`, `FromEmail`, and `FromName`.

The reminder worker is enabled through `Smtp:EnableReminderWorker`. When it is `true`, the infrastructure layer starts a background service that sends reservation reminder emails and creates reminder notifications.

## Configuration and authentication

Each frontend has its own `appsettings.json` because it is a separate ASP.NET Core application with its own host and deployment boundary.

- `AppaRently.Web`: client portal
- `AppaRently.Web.Owner`: owner portal
- `AppaRently.Web.SuperAdmin`: administrative portal

Shared values such as database connection, JWT signing settings, and SMTP defaults are repeated across those files for convenience. Portal-specific settings stay separate, especially the authentication cookie name, so the browser does not reuse a session from one portal in another.

Authentication is handled by ASP.NET Core Identity plus JWT bearer validation. Authorization is enforced with `[Authorize]` attributes and role checks in each portal.

The browser session is isolated per portal with different authentication cookie names, so logging into owner does not automatically sign you into client or superadmin.

## Technology Stack

- .NET 10 / ASP.NET Core MVC: web applications, controllers, views, routing, model binding, and server-side rendering.
- ASP.NET Core Identity: user accounts, passwords, roles, sign-in, sign-out, and authentication cookies.
- JWT bearer authentication: token validation for API-style auth flows and shared identity across entry points.
- Entity Framework Core: ORM for the domain model, queries, migrations, and data seeding.
- PostgreSQL with Npgsql: relational database and .NET provider used by EF Core.
- Docker and Docker Compose: local orchestration for the database and the three web apps.
- SMTP via `System.Net.Mail`: outbound email delivery, including reservation emails and reminders.
- Bootstrap: layout, responsive grid, forms, buttons, and basic UI primitives.
- jQuery: legacy client-side helper scripts where needed by the UI.
- Excel generation via `AppaRently.Web.Owner.Services.XlsxReportWriter`: custom `.xlsx` creation for owner dashboard and apartment reports.
- Data Protection: persistent protection keys for cookies and other ASP.NET Core protected payloads.

## Seed accounts

On startup, the app runs migrations and seeding automatically. If the database is empty, it creates:

- Super admin: `superadmin@apparently.local`
- Owner 1: `lukreroll1@gmail.com`
- Owner 2: `lukreroll2@gmail.com`
- Default password: `AppaRently123!`

Each owner also receives 3 sample apartments.

## Architecture

- `AppaRently.Domain`: domain entities and soft-delete rules
- `AppaRently.App`: contracts, DTOs, and service interfaces
- `AppaRently.Infrastructure`: EF Core, migrations, services, seeders, and notifications
- `AppaRently.Web`: client portal
- `AppaRently.Web.Owner`: owner portal
- `AppaRently.Web.SuperAdmin`: administrative portal

The app uses one shared data model and three separate UIs, with common startup initialization through `SeedAppaRentlyAsync()`.

## Summary

AppaRently is split into a shared core and three role-based portals. The shared core keeps the domain model, service contracts, persistence, migrations, seeding, notifications, and SMTP/JWT plumbing in one place so the business rules stay consistent.

The three frontends are intentionally separate because each one serves a different audience and needs its own session boundary, navigation, and authorization rules. Client, owner, and superadmin all authenticate through ASP.NET Core Identity, but each portal uses a different authentication cookie so a login in one app does not spill into the others.

The client portal is intentionally narrower: it focuses on catalog, favorites, reservations, and notifications tied to favorite apartments. The owner portal focuses on listings, portfolio metrics, exports, and reservation notifications for apartments they own. The superadmin portal covers users, apartments, metrics, and system-wide operations.

The main design choices were:

- shared infrastructure instead of duplicated business logic
- separate MVC apps instead of one mixed UI
- Identity plus JWT for authentication
- role-based authorization with explicit portal checks
- Docker-first startup with automatic migration and seeding
- Gmail SMTP with app passwords for outbound mail

## Key technical decisions

- Strict availability: reservations are validated against overlaps, and date-range apartment searches exclude occupied apartments.
- Standardized times: every reservation is normalized to `2:00 PM` check-in and `12:00 PM` check-out.
- Soft delete with cascade: deleting a client, apartment, or owner archives dependent records to preserve history.
- In-app notifications: each portal exposes an inbox, unread counter, and mark-as-read actions.
- Client notifications are limited to notifications tied to favorite apartments.
- Owner notifications are limited to reservation created and cancelled events on apartments they own.
- Business metrics: owner and superadmin portals expose revenue, potential revenue, occupancy, profitability, average reservation value, average stay, and unique tenants, with selectable reporting periods.
- Docker-first startup: the full stack starts with a single command and uses your local PostgreSQL instance as the shared database.

## Useful areas

- Client: catalog, favorites, reservations, and notifications
- Owner: dashboard, inventory, Excel export, and reservation notifications
- Owner exports are assembled by `OwnerPortalService` and written to `.xlsx` by `XlsxReportWriter`.
- SuperAdmin: users, apartments, metrics, and notifications

Made by:
Andrés Hidrobo
