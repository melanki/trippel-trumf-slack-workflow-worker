# melanki.trippeltrumf.service

Worker service that monitors Trumf's Trippel-Trumf page, extracts the next campaign date, keeps the latest state in memory, and posts state changes to a Slack workflow webhook.

## What the service does

1. On startup, the polling worker checks whether cached state must be refreshed.
2. If refresh is needed, it scrapes rendered content from `https://www.trumf.no/trippel-trumf` using Playwright.
3. It extracts date candidates from article text using Norwegian month names.
4. It stores the latest result and metadata in memory.
5. It publishes state changes to a channel that the notifying worker forwards to Slack.
6. It evaluates day-before reminders in `Europe/Oslo` time and keeps reminder state in memory for the current process lifetime.
7. It schedules the next check at UTC midnight and repeats.

Slack payload shape:

```json
{
  "trippel_trumf_date": "yyyy-MM-dd"
}
```

## Date extraction rules

- Dates are parsed from rendered article text (`article.innerText`) using Norwegian month names.
- The reference year comes from JSON-LD `dateModified`.
- Year rollover only happens when `dateModified` month is December and extracted month is January.
- If no date is found, `nextTrippelTrumfDate` is `null` in internal state.

## Project structure

```text
src/melanki.trippeltrumf.service
├── Composition/
│   └── ServiceCollectionExtensions.cs
├── Features/
│   ├── Notifying/
│   │   ├── Client.cs
│   │   ├── Options.cs
│   │   ├── Reminder.cs
│   │   └── Worker.cs
│   ├── Polling/
│   │   ├── ChangeFeed.cs
│   │   ├── DateExtractor.cs
│   │   ├── Result.cs
│   │   ├── StateStore.cs
│   │   └── Worker.cs
│   └── Scraping/
│       ├── RenderedArticleSnapshot.cs
│       ├── Scraper.cs
│       └── StructuredTrippelTrumfPage.cs
└── Program.cs
```

## Requirements

- .NET SDK `10.0.x`
- Network access to:
  - `https://www.trumf.no/trippel-trumf`
  - Your Slack workflow webhook endpoint
- Playwright browser runtime (service tries `chrome` channel first, then Chromium fallback)

## Configuration

Configuration files:

- `src/melanki.trippeltrumf.service/appsettings.json`
- `src/melanki.trippeltrumf.service/appsettings.Development.json`
- `src/melanki.trippeltrumf.service/appsettings.Production.json`

Required key:

- `TrippelTrumfService:SlackWorkflowWebhookUrl`

Startup fails if this value is missing or empty.

Example:

```json
{
  "TrippelTrumfService": {
    "SlackWorkflowWebhookUrl": "https://hooks.slack.com/triggers/..."
  }
}
```

Environment selection:

- Prefer `DOTNET_ENVIRONMENT` (`Development`, `Production`, etc.)
- `ASPNETCORE_ENVIRONMENT` is used as fallback

Never commit real secrets.

## Build and run

From repository root:

```bash
dotnet restore
dotnet build src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj -v minimal
DOTNET_ENVIRONMENT=Development dotnet run --project src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj
```

If Playwright browser dependencies are missing on a fresh machine, run after build:

```bash
pwsh src/melanki.trippeltrumf.service/bin/Debug/net10.0/playwright.ps1 install
```

## Container image

Build a deployable image from repository root:

```bash
docker build -t ghcr.io/<owner>/trippel-trumf-slack-workflow-worker:<tag> .
```

The image uses a multi-stage `Dockerfile` and runs the published worker inside the official Playwright .NET container so browser dependencies are present at runtime.

## k3s deployment

Kubernetes manifests are under `deploy/k8s/`.

The deployment references a Kubernetes secret for Slack webhook configuration:

- Secret name: `trippel-trumf-worker-secrets`
- Secret key: `TrippelTrumfService__SlackWorkflowWebhookUrl`

Quick start:

```bash
kubectl apply -f deploy/k8s/namespace.yaml
kubectl -n trippel-trumf create secret generic trippel-trumf-worker-secrets \
  --from-literal=TrippelTrumfService__SlackWorkflowWebhookUrl='https://hooks.slack.com/triggers/...' \
  --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -k deploy/k8s
kubectl -n trippel-trumf set image deployment/trippel-trumf-worker \
  worker=ghcr.io/melanki/trippel-trumf-slack-workflow-worker:<commit-sha>
kubectl -n trippel-trumf rollout status deployment/trippel-trumf-worker --timeout=300s
```

See `deploy/k8s/README.md` for validation and rollback procedures.

## Logging

- Logging is handled by Serilog with structured JSON output to console.
- Configure levels via `Serilog:MinimumLevel` in `appsettings*.json`.
- `Information`: lifecycle and successful major events
- `Debug`: extraction details, cache decisions, state transitions, webhook flow

Do not log secrets or full webhook URLs.
