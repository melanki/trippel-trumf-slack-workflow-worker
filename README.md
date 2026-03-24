# Trippel-Trumf Slack Workflow Worker

`melanki.trippeltrumf.service` is a .NET worker that watches Trumf's Trippel-Trumf page and pushes updates to a Slack workflow webhook.

## What problem it solves

Trippel-Trumf dates are published on web content. This service automates:

- Detecting the next announced Trippel-Trumf date from rendered page content.
- Keeping a current in-memory state of the latest known date and scrape status.
- Sending Slack workflow notifications when state changes.
- Sending a day-before reminder for the known date.

## How it is solved

The service is split into three feature slices:

1. `Scraping`: Uses Playwright to load rendered HTML from `https://www.trumf.no/trippel-trumf` and extracts article text plus JSON-LD `dateModified`.
2. `Polling`: Runs at startup and then once per UTC day, extracts Norwegian date expressions, computes the next date, and publishes change events when state changes.
3. `Notifying`: Subscribes to change events, posts webhook payloads to Slack, and runs day-before reminder checks every 12 hours in `Europe/Oslo` time.

## Date extraction model

- Date text is parsed from rendered content using Norwegian month names.
- Reference year comes from JSON-LD `dateModified` when available.
- Year rollover is only applied when `dateModified` is in December and extracted month is January.
- If no date can be extracted, the internal next date is `null`.

## Slack payload

```json
{
  "trippel_trumf_date": "yyyy-MM-dd"
}
```

## Runtime characteristics

- Stateless process model with in-memory caches/state stores.
- Graceful behavior on scrape/post failures (state captures last error, loops continue).
- Structured JSON logging via Serilog.
- Webhook URL is required at startup.

## Developer documentation

Developer setup and operational details are documented in [DEVELOPERS.md](./DEVELOPERS.md):

- local setup and run commands
- required configuration
- repository layout and coding conventions
- tests and validation
- CI/CD and k3s deployment details
