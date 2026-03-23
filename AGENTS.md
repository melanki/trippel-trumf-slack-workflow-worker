# Repository Guidelines

## Project Structure & Module Organization
Keep the API code organized by feature and responsibility.

- `src/` contains application code.
- `src/modules/<feature>/` groups route, controller, service, and schema files per domain.
- `src/shared/` stores reusable utilities, middleware, and error helpers.
- `src/config/` contains runtime configuration and environment parsing.
- `tests/` mirrors `src/` for unit and integration coverage.
- `docs/` stores API notes, decision records, and onboarding docs.

Prefer small, focused modules over large cross-cutting files.

## Build, Test, and Development Commands
Use npm scripts as the single entry point for local workflows:

- `npm install` installs dependencies.
- `npm run dev` starts the API in watch mode for development.
- `npm run build` compiles production artifacts.
- `npm test` runs the automated test suite.
- `npm run lint` checks style and static analysis.
- `npm run format` applies code formatting.

If you add or rename scripts, update this guide and `package.json` together.

## Coding Style & Naming Conventions
- Use TypeScript with 2-space indentation and trailing semicolons.
- Use `camelCase` for variables/functions, `PascalCase` for classes/types, and `UPPER_SNAKE_CASE` for constants.
- Use `kebab-case` for file names (example: `order-service.ts`).
- Keep functions single-purpose; extract shared logic into `src/shared/`.
- Run lint and format checks before opening a pull request.

## Testing Guidelines
- Write unit tests for business logic and integration tests for HTTP endpoints.
- Name tests `*.spec.ts` and place them in `tests/` mirroring source paths.
- Target meaningful coverage for changed code paths, including error cases.
- Prefer deterministic tests; mock external services and avoid network calls.

## Commit & Pull Request Guidelines
- Follow Conventional Commits (for example: `feat(auth): add token refresh endpoint`).
- Keep commits focused and atomic; avoid mixing refactors with behavior changes.
- PRs should include: purpose summary, key changes, test evidence, and linked issue IDs.
- Include request/response examples when API behavior changes.

## Security & Configuration Tips
- Never commit secrets; use `.env` locally and keep `.env.example` updated.
- Validate and sanitize all external input at route boundaries.
- Document new configuration keys in `docs/` and the example env file.
