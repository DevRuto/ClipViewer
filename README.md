# ClipViewer

[![Build & Test](https://github.com/DevRuto/ClipViewer/actions/workflows/build-test.yml/badge.svg)](https://github.com/DevRuto/ClipViewer/actions/workflows/build-test.yml)

I wanted something like streamable/youtube where I upload my video clips and
it would last as long as the server stays up.

No plans to maintain this project as long as it fits my minimal needs

## 🚀 Tech Stack

### Backend
- **.NET 10.0**
- **Entity Framework Core**
- **PostgreSQL**
- **FFmpeg**
- **HLS (HTTP Live Streaming)**

### Frontend
- **Vue.js 3**
- **Tailwind v4**

## 🚀 Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone https://github.com/DevRuto/ClipViewer.git
   cd ClipViewer
   ```

2. **Configure secrets**
   ```bash
   cp .env.example .env
   ```
   Then edit `.env` and set `POSTGRES_PASSWORD` and `JWT_SECRET` to real values
   (e.g. `openssl rand -base64 48` for the JWT secret). The API refuses to start
   with a missing/placeholder `JWT_SECRET`.

3. **Start the application**
   ```bash
   docker compose up --build
   ```

4. **Access the application**
   - http://localhost:5000
   - Database: PostgreSQL on port 5432

5. Adding users
   - There's a `create_user.sh` script that would connect to the docker postgres db and create a user with the given username
   - There's also `update_user.sh` to change the API key for the given username if needed for any reason

## 🛠️ Manual Setup (Development)

### Backend

1. **Set up the database**
   - Create a new PostgreSQL database
   - Update the connection string in `ClipViewer.API/appsettings.Development.json` (and
     `ClipViewer.Worker/appsettings.Development.json` if you're also running the worker locally)

2. **Start the backend** (run from the repo root)
   ```bash
   dotnet run --project ClipViewer.API
   ```
   Migrations are applied automatically on startup (`context.Database.Migrate()` in `Program.cs`), so
   there's no separate migration step for local development.

### Frontend

1. **Install dependencies**
   ```bash
   cd clipviewer.vue
   npm install
   ```

2. **Start the development server**
   ```bash
   npm run dev
   ```


## Known limitations

- If the API crashes after accepting an upload but before the worker claims the job, or a conversion
  job errors out partway through, the temp/partial output files aren't automatically garbage-collected.
  Use the retry button on a failed clip to reprocess it from the saved temp file; there's no scheduled
  cleanup of orphaned files beyond that (acceptable tradeoff for a personal-scale service).

## EF Stuff
Run from the repo root:
```shell
dotnet ef database update \
  --project ClipViewer.API \
  --startup-project ClipViewer.API \
  --context ApplicationDbContext
```

## Ramble?
I wanted to create an app that I can self-host to hold videos to share with.
The various online solutions tend to expire videos after some time from their free tier, or take too long to process (which is fair due to computing power).
None of these are unreasonable but it provided an idea for a side project to work on.

Additionally with the rise of AI tools in modern softwares, it gave me a chance to see how far these
AI tools can go. There were 2 particular weak spots I had that AI was able to help compensate for the most part.
The vue app in this project used Tailwind CSS framework. AI was able to help format the app in a responsive way with
it's knowledge of tailwind's utility classes, so I didn't really need to exert much
brainpower into the UI/UX portion of the app.

Testing was another point, but moreso on the laziness side rather than weak.
I gave a prompt to create tests and (Windsurf) was able to generate these tests
based on it's deep understanding of the project through cascade. These tests weren't
perfect, but it was still mostly usable. Some parts needed manual intervention.
Part of the reason is due to outdated knowledge of the framework.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.


