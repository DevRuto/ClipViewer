# ClipViewer

[![Build & Test](https://github.com/DevRuto/ClipViewer/actions/workflows/build-test.yml/badge.svg)](https://github.com/DevRuto/ClipViewer/actions/workflows/build-test.yml)

A self-hosted video clip sharing service, like a minimal Streamable or YouTube. Upload a clip, it gets
transcoded to HLS in the background, and it's playable and shareable via a link for as long as the
server stays up.

This is a personal side project with no long-term maintenance commitment — changes are kept minimal
and pragmatic rather than over-engineered.

## Tech stack

**Backend**
- .NET 10.0
- Entity Framework Core
- PostgreSQL
- FFmpeg (via Xabe.FFmpeg)
- HLS (HTTP Live Streaming)

**Frontend**
- Vue 3
- Tailwind v4

## Architecture

Three .NET projects plus one Vue SPA, backed by PostgreSQL:

- **ClipViewer.API** — ASP.NET Core web API that also hosts the built Vue SPA as static files. Handles
  auth, video CRUD, and file uploads. Does not transcode video itself.
- **ClipViewer.Worker** — a separate background-service process that polls the database for pending
  conversion jobs and does the actual FFmpeg work.
- **ClipViewer.Data** — shared EF Core `ApplicationDbContext` and entity models, referenced by both the
  API and the Worker.
- **clipviewer.vue** — the Vue 3 + Tailwind frontend, built to `clipviewer.vue/dist` and served by the
  API in production (proxied to the Vite dev server on `:5173` in development).

The API and Worker are decoupled entirely through the Postgres database — there's no in-process queue
or message broker. An upload writes a `VideoClip` row and a `VideoConversionJob` row (`Pending`) to the
DB; the Worker polls for the oldest pending job, transcodes it to HLS and generates a thumbnail, then
updates the clip and marks the job complete. Job progress is written back to the DB as FFmpeg reports
it, which is how the frontend polls for conversion progress.

## Quick start with Docker

1. **Clone the repository**
   ```bash
   git clone https://github.com/DevRuto/ClipViewer.git
   cd ClipViewer
   ```

2. **Configure secrets**
   ```bash
   cp .env.example .env
   ```
   Edit `.env` and set `POSTGRES_PASSWORD` and `JWT_SECRET` to real values (e.g.
   `openssl rand -base64 48` for the JWT secret). The API refuses to start with a missing or
   placeholder `JWT_SECRET`.

3. **Start the application**
   ```bash
   docker compose up --build
   ```

4. **Access the application**
   - App: http://localhost:5000
   - Database: PostgreSQL on port 5432

5. **Add a user**
   - `scripts/create_user.sh` connects to the compose Postgres container and creates a user with a
     given username, printing an API key.
   - `scripts/update_user.sh` rotates the API key for an existing username.
   - `scripts/set_user_role.sh` sets a user's role to `Admin` or `User`.
   - There's no signup endpoint — users are provisioned out-of-band via these scripts.

## Manual setup (development)

### Backend

1. **Set up the database**
   - Create a PostgreSQL database.
   - Update the connection string in `ClipViewer.API/appsettings.Development.json` (and
     `ClipViewer.Worker/appsettings.Development.json` if you're also running the worker locally).

2. **Run the backend** (from the repo root)
   ```bash
   dotnet run --project ClipViewer.API
   dotnet run --project ClipViewer.Worker   # needs FFmpeg on PATH
   ```
   Migrations are applied automatically on startup (`context.Database.Migrate()` in `Program.cs`), so
   there's no separate migration step for local development.

### Frontend

```bash
cd clipviewer.vue
npm install
npm run dev
```

### Tests

```bash
dotnet test ClipViewer.UnitTests         # fully isolated, all collaborators mocked
dotnet test ClipViewer.IntegrationTests  # real EF Core InMemory DbContext, file I/O, live worker loop
```

```bash
cd clipviewer.vue
npm run test
```

## Known limitations

- If the API crashes after accepting an upload but before the worker claims the job, or a conversion
  job errors out partway through, the temp/partial output files aren't automatically garbage-collected.
  Use the retry button on a failed clip to reprocess it from the saved temp file — there's no scheduled
  cleanup of orphaned files beyond that (an acceptable tradeoff for a personal-scale service).

## EF Core migrations

Run from the repo root:
```bash
dotnet ef migrations add <Name> --project ClipViewer.API --startup-project ClipViewer.API --context ApplicationDbContext
dotnet ef database update --project ClipViewer.API --startup-project ClipViewer.API --context ApplicationDbContext
```
The API auto-applies pending migrations on startup, so `database update` is mainly useful for local
inspection or rollback.

## Docs

See `docs/ENDPOINTS.md` for example request/response payloads for the video endpoints.

## Ramble

I wanted an app I could self-host to hold video clips and share them. The various online solutions
tend to expire videos after some time on their free tier, or take a while to process — which is fair
given the compute cost, but neither is unreasonable, and together they gave me an excuse for a side
project.

This project also doubled as a testbed for how far AI coding tools have come, and how I could fold
them into my normal workflow rather than treating them as a novelty. Two spots in particular were weak
points for me that AI tooling covered well. The Vue frontend uses Tailwind, and having a model that
knows Tailwind's utility classes well meant I didn't have to spend much brainpower getting the UI to
look reasonable and behave responsively. Testing was the other one — less a skill gap, more laziness.
Early on I prompted Windsurf to generate tests, and it did a decent job using its understanding of the
project through Cascade; not perfect, and some needed manual fixes (partly due to outdated framework
knowledge), but still net useful.

Later work on this project shifted to Claude Code, mostly as a way to build up practice working with an
agentic CLI tool day to day rather than a one-off prompt-and-paste workflow — driving real changes
through a terminal agent, reviewing its diffs, and figuring out where to trust it versus where to step
in.

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
