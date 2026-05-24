# ADR 0009: LocalApi Local Security

## Status
Accepted

## Context
LocalApi is a browser-accessible HTTP service running on the user's machine. It should serve the desktop app without exposing unsafe network access, arbitrary file access, or unauthenticated mutation when a local token is configured.

## Decision
LocalApi binds to loopback by default, restricts CORS to local desktop origins, rejects non-loopback access, optionally requires `X-LocalMind-Token` for mutating endpoints, validates upload size and type, sanitizes uploaded filenames, and stores files only under managed runtime directories.

## Consequences
Desktop mode works without public network exposure. Security failures return API envelopes with stable error codes. Operators can configure a local token through configuration or `LOCALMIND_LOCAL_API_TOKEN`.

## Related
- [LocalApi local security](../local-security.md)
