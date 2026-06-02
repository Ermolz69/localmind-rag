# Setup

Install:

- .NET 10 SDK
- Node.js 24
- pnpm 10
- Rust/Cargo for full Tauri packaging
- Docker, optional, for remote sync infrastructure
- Task (https://taskfile.dev) for running predefined tasks

Then run:

```bash
task -t .config/task/Taskfile.yml setup
task -t .config/task/Taskfile.yml check
```

To run the application locally:

```bash
task -t .config/task/Taskfile.yml build
pnpm dev
```

Local environment values belong in `.env`, which is ignored. Keep `.env.example` updated when new settings are introduced.

Do not commit runtime data, SQLite files, local AI model files, generated desktop builds, generated documentation, or portable release archives. See [repository-hygiene.md](repository-hygiene.md).
