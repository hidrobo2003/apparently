# AppaRently

Short-stay rental platform built with ASP.NET Core on .NET 10, with three MVC frontends and a shared infrastructure layer for persistence, seeding, and business rules.

## Prerequisites

- Docker and Docker Compose
- .NET 10 SDK, only if you want to run the solution outside containers
- PostgreSQL installed locally and running on `localhost:5432`

## Start the environment

```bash
docker compose up --build
```

That command starts:

- PostgreSQL database
- Client portal at `http://localhost:5001`
- Owner portal at `http://localhost:5002`
- SuperAdmin portal at `http://localhost:5003`

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

## Key technical decisions

- Strict availability: reservations are validated against overlaps, and date-range apartment searches exclude occupied apartments.
- Standardized times: every reservation is normalized to `2:00 PM` check-in and `12:00 PM` check-out.
- Soft delete with cascade: deleting a client, apartment, or owner archives dependent records to preserve history.
- In-app notifications: each portal exposes an inbox, unread counter, and mark-as-read actions.
- Business metrics: owner and superadmin portals expose revenue, potential revenue, occupancy, profitability, average reservation value, average stay, and unique tenants, with selectable reporting periods.
- Docker-first startup: the full stack starts with a single command and uses your local PostgreSQL instance as the shared database.

## Useful areas

- Client: catalog, favorites, reservations, and notifications
- Owner: dashboard, inventory, Excel export, and notifications
- SuperAdmin: users, apartments, metrics, and notifications

Made by:
Andrés Hidrobo
