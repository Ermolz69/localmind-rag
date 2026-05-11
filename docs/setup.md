# Setup

Install:

- .NET 10 SDK
- Node.js 24
- pnpm 10
- Rust/Cargo for full Tauri packaging
- Docker, optional, for remote sync infrastructure

Then run:

```bash
pnpm install
pnpm check
```

Local environment values belong in `.env`, which is ignored. Keep `.env.example` updated when new settings are introduced.

Do not commit runtime data, SQLite files, local AI model files, generated desktop builds, or portable release archives. See [repository-hygiene.md](repository-hygiene.md).
