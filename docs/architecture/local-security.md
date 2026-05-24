# LocalApi Local Security

LocalApi is intended for the desktop app on the same machine, not for public network access.

## Defaults

- LocalApi binds to `http://127.0.0.1:49321` by default.
- Requests must use a loopback host and loopback remote address when `LocalApi:Security:RequireLoopback` is enabled.
- CORS allows desktop/local origins only: `127.0.0.1`, `localhost`, and `tauri.localhost`.
- Mutating endpoints require `X-LocalMind-Token` when `LocalApi:Security:Token` or `LOCALMIND_LOCAL_API_TOKEN` is configured.
- Health, OpenAPI, and documentation/static assets are exempt where needed for local tooling.

## Upload Guardrails

Document uploads are validated at the application boundary:

- empty files are rejected;
- files larger than 100 MB are rejected;
- unsupported extensions are rejected;
- uploaded names are sanitized with `Path.GetFileName`;
- stored files are written under `runtime/app/files/{documentId}/{fileName}`.

The storage layer does not accept arbitrary import paths. Uploaded streams are copied into the managed runtime folder, and the saved file hash is calculated from that local copy.

## Error Shape

Security and upload failures return the standard API envelope with stable codes such as `LOCAL_ACCESS_DENIED`, `LOCAL_TOKEN_REQUIRED`, `LOCAL_TOKEN_INVALID`, `REQUEST_TOO_LARGE`, and `UNSUPPORTED_MEDIA_TYPE`.
