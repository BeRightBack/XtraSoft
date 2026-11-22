# XtraSoft

A modular ASP.NET Core web platform I wrote from scratch on **.NET 10**. It is my
working demonstration of how I structure a real application: layered projects with
dependencies pointing inward, a hand-built localisation layer, Identity wired to
custom user and role entities, and the cross-cutting concerns (logging, email,
real-time) pulled out into services rather than scattered through controllers.

This repository is published for portfolio review. It is not packaged as a NuGet
template and it is not a product — treat it as source to read.

## Layout

| Project | Responsibility |
| --- | --- |
| `XtraSoft.Contracts` | Interfaces and shared abstractions the rest of the solution depends on |
| `XtraSoft.Core` | Domain entities and common types |
| `XtraSoft.Infrastructure` | EF Core `ApplicationDbContext`, configuration, migrations |
| `XtraSoft.Identity` | ASP.NET Identity: custom `UserProfile` / `UserRole`, role policy setup, seeder |
| `XtraSoft.Localization` | The localisation layer (middleware + DB-backed string resources) |
| `XtraSoft.Services` | Email via MailKit, secure verification, translation service registration |
| `XtraSoft.Razor` | Shared Razor helper library |
| `XtraSoft.Web` | MVC front end: controllers, Razor views, SignalR hub |
| `XtraSoft.WebGL` | Experimental Blazor front end rendering 3D scenes with Babylon.js |

141 C# sources and 92 Razor views.

## Things worth reading

**Localisation without resource files.** `XtraSoft.Localization` resolves the
visitor's language per request in `LocalizationMiddleware`, registered in
`Program.cs`. UI strings come from a `StringResources` table read through Entity
Framework Core, so a translation can be corrected at runtime instead of
recompiling and redeploying. This is the part of the codebase I am most interested
in discussing — the trade-offs against satellite assemblies are real and I made
these choices knowingly.

**Identity.** Custom `UserProfile` and `UserRole` types with a policy-based
authorisation setup (`AddPolicy("Admin", policy => policy.RequireRole("Admin"))`),
plus a seeder that creates the initial role and account from configuration.

**Real-time.** A SignalR `ChatHub` mapped at `/chathub`, used by the MVC front end.

**Logging.** Serilog writing to console, rolling files and a MySQL sink,
configured at startup before the host is built.

**Two front ends on one backend.** `XtraSoft.Web` (MVC) and `XtraSoft.WebGL`
(Blazor + Babylon.js) both sit on the same service layer, which is what motivated
the contracts/core split.

## Running it

Requires the .NET 10 SDK and a MySQL server.

1. Copy `appsettings - demo.json` to `appsettings.json`.
2. Fill in the connection strings and SMTP settings with your own values — every
   credential in this repository is a placeholder (`CHANGE_ME_*`, `password`,
   `your-…-key`). There are no working secrets here and none should be committed.
3. Apply migrations from `XtraSoft.Infrastructure` (initial `Init` migration).
4. `dotnet run --project XtraSoft.Web` — it listens on `http://localhost:9001`
   and expects a reverse proxy to terminate TLS.

## Known rough edges

I would rather list these than have you find them:

- The DeepL-backed `ITranslationService` in `XtraSoft.Services` is a stub that
  returns the input unchanged. The wiring and configuration shape are there; the
  translation call is not implemented.
- The `Volo.Abp.EntityFrameworkCore.MySQL` package reference is a leftover; the
  code does not use ABP.
- There is no solution file in the repository, no CI workflow, and the test
  coverage I would want is not here.
- Some front-end experiments in `XtraSoft.WebGL` (the driving-game scene) are
  demos of a technique rather than finished features.

## Licence

Public domain (Unlicense) — see `LICENSE.txt`.
