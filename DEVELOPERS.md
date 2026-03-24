# Developers

This document contains contributor-focused, need-to-know details for `melanki.trippeltrumf.service`.

## Repository layout

- Service project: `src/melanki.trippeltrumf.service`
- Tests: `tests/melanki.trippeltrumf.service.tests`
- Kubernetes manifests: `deploy/k8s`
- Main composition/startup: `src/melanki.trippeltrumf.service/Program.cs`
- Service wiring: `src/melanki.trippeltrumf.service/ServiceCollectionExtensions.cs`

Feature slices:

- `Features/Scraping`
- `Features/Polling`
- `Features/Notifying`

## Prerequisites

- .NET SDK `10.0.x`
- Network access to `https://www.trumf.no/trippel-trumf`
- Network access to your Slack workflow webhook endpoint
- Playwright browser runtime (service tries Chrome channel first, then Chromium fallback)

## Local setup and run

From repository root:

```bash
dotnet restore
dotnet build src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj -v minimal
DOTNET_ENVIRONMENT=Development dotnet run --project src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj
```

Tests:

```bash
dotnet test tests/melanki.trippeltrumf.service.tests/melanki.trippeltrumf.service.tests.csproj -c Release --no-restore --verbosity normal
```

If Playwright dependencies are missing on a new machine, install after build:

```bash
pwsh src/melanki.trippeltrumf.service/bin/Debug/net10.0/playwright.ps1 install
```

## Configuration

Config files are in the service project root:

- `src/melanki.trippeltrumf.service/appsettings.json`
- `src/melanki.trippeltrumf.service/appsettings.Development.json`
- `src/melanki.trippeltrumf.service/appsettings.Production.json`

Required key:

- `TrippelTrumfService:SlackWorkflowWebhookUrl`

Rules:

- Startup fails if webhook URL is missing/empty.
- Never commit real secrets.
- In Kubernetes/GitHub Actions, inject the value through secret `TrippelTrumfService__SlackWorkflowWebhookUrl`.

Environment resolution:

- Prefer `DOTNET_ENVIRONMENT`.
- `ASPNETCORE_ENVIRONMENT` is used as fallback.

## Runtime behavior details

Polling:

- Startup refresh attempt runs immediately.
- Then refresh checks run at next UTC midnight (daily).
- Refresh is skipped if cached next date is still in the future.
- State change events are published when date/error/reference metadata changed.

Notifying:

- Consumes state-change events and POSTs webhook payload to Slack.
- Day-before reminder checks run on startup and every 12 hours.
- Reminder timezone is `Europe/Oslo`, with UTC fallback if unavailable.
- Reminder is sent once per target date (in-memory marker).

Date extraction:

- Regex-based extraction from rendered article text using Norwegian month names.
- Uses JSON-LD `dateModified` year when present.
- Only rolls year when `dateModified` month is December and extracted month is January.

## Logging and safety

- Serilog structured JSON logs to console.
- `Information` for lifecycle/successful major events.
- `Debug` for diagnostics (extraction, transitions, webhook flow).
- Do not log secrets or full webhook URLs.

## CI/CD and deployment

GitHub workflows:

- `CI` (`.github/workflows/ci.yml`): restore, build, test.
- `Container` (`.github/workflows/container.yml`): build/push GHCR image tags and release.
- `Deploy` (`.github/workflows/deploy.yml`): apply k8s manifests, set image, verify rollout.

Image versioning:

- Release tag format: `sha-<shortsha>`.
- `main` tag is also pushed.

Kubernetes details and rollback commands:

- See `deploy/k8s/README.md`.

## Contribution expectations

- Use Conventional Commits (`feat:`, `fix:`, `chore:`, ...).
- Keep PRs focused and explain what changed, why, and how it was validated.
- Include `Resolves #<issue number>` in PR descriptions when relevant.
