# Repository Guidelines

## Scope
This repository contains a single .NET worker service:

- `src/melanki.trippeltrumf.service`

The service scrapes Trumf content with Playwright, extracts next Trippel‑Trumf date, stores state in memory, and posts state changes to Slack workflow webhooks.

## Project Structure
Keep code organized by vertical feature slices under `Features/`:

- `Features/Scraping`
- `Features/Polling`
- `Features/Notifying`

Composition and startup:

- `Program.cs`
- `Composition/ServiceCollectionExtensions.cs`

Conventions for feature folders:

- Use simple file/type names inside each feature (`Worker`, `Client`, `Options`, `StateStore`, etc.).
- Avoid repeating `TrippelTrumf` prefix inside feature-internal types unless needed for clarity.

## Build, Run, and Validation
Primary commands:

- `dotnet restore`
- `dotnet build src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj -v minimal`
- `DOTNET_ENVIRONMENT=Development dotnet run --project src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj`

Environment selection:

- Prefer `DOTNET_ENVIRONMENT` (`Development`, `Production`, etc.).
- `Program.cs` also reads `ASPNETCORE_ENVIRONMENT` as fallback.

## Configuration
Config files live in service project root:

- `appsettings.json` (base)
- `appsettings.Development.json`
- `appsettings.Production.json`

Current required key:

- `TrippelTrumfService:SlackWorkflowWebhookUrl`

Rules:

- Missing/empty webhook URL must fail startup.
- Never commit real secrets.
- `appsettings.*.json` is git-ignored for environment-specific files.

## Coding Style
- Use modern C# with nullable enabled.
- Keep classes small and single-purpose.
- Prefer async APIs and cancellation token plumbing end-to-end.
- Keep logs structured (`{PropertyName}` placeholders).
- Keep business logic deterministic and isolated from infrastructure when possible.

## Logging
- Keep `Information` logs for lifecycle and successful major events.
- Use `Debug` logs for diagnostics (extraction details, state transitions, webhook post flow).
- Do not log secrets or full webhook URLs.

## Scraping and Date Logic
- Scrape rendered content via Playwright.
- Extract date from rendered article text using Norwegian month names.
- Year rule:
  - Use `dateModified` year from JSON-LD.
  - Only roll to next year when `dateModified` month is December and extracted month is January.

## Testing Expectations
When adding tests, prefer:

- Unit tests for date extraction/state logic.
- Integration-style tests for worker behavior with mocked external dependencies (Slack/web requests).
- Deterministic tests with fixed clock inputs.

## Git and PR Workflow
- Use Conventional Commits (`feat:`, `fix:`, `chore:`, etc.).
- Commit in logical chunks (feature, refactor, config, docs).
- Keep PRs focused; include what changed, why, and how it was validated.
