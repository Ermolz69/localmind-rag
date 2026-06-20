# Companion Mode

Companion Mode is an opt-in mode that lets a phone act as a remote interface to
LocalMind running on the computer. The phone is a companion control surface, not
a second copy of LocalMind: the desktop machine remains the only runtime that
holds the database, runs AI models, indexes documents, and answers RAG queries.

```text
Computer = primary runtime
Phone    = remote control surface
```

## Local-first stays the default

The desktop product is local-only by default. `KnowledgeApp.LocalApi` binds to
`127.0.0.1` and is never exposed to the LAN or public network by the desktop app
(see [Local security](local-security.md)). Companion Mode does not change that
default. It is a separate, explicitly user-enabled capability:

- It is **disabled by default**. The user decides when to allow a phone to
  connect.
- Enabling it is a deliberate action in Settings, not an implicit consequence of
  any other feature.
- Turning it off returns LocalMind to its fully local, closed posture.

## Current scope

Companion Mode is being built in stages. It deliberately does **not** yet open a
network listener or move any data off the machine — the phone-facing transport is
a later step.

What exists today:

- A persisted setting, `CompanionMode.Enabled`, stored in `app_settings` under
  the `CompanionMode.Enabled` key and exposed through the standard settings
  endpoints as part of `AppSettingsDto.CompanionMode`.
- A **Companion Mode** section in desktop Settings with an enable toggle and a
  status display.
- A **QR pairing flow**: the desktop can start a short-lived pairing session,
  render its QR code, and manage a list of trusted devices. See
  [Pairing](#pairing-qr-code) below.
- A **mobile companion interface**: a standalone, phone-first shell served from
  the existing frontend at `/companion`, including a working RAG **chat** and
  semantic **search** over the already-indexed knowledge base. See
  [Mobile interface](#mobile-interface) below.

## Settings shape

`CompanionMode` lives in `app_settings` and round-trips through the settings
endpoints like every other settings group. The configurable field is:

- **Enabled** — whether Companion Mode is on. Defaults to `false`.

## Status model

The Settings section shows a derived status so the user can see the mode's state
at a glance:

| Status | Meaning |
| --- | --- |
| `Off` | Companion Mode is disabled. No phone connection is possible. |
| `Waiting for connection` | Companion Mode is enabled but no device is connected. |
| `Connected` | A phone is connected. Reserved for a later stage once a transport and pairing exist. |

Today the status is derived from the persisted `Enabled` flag (`Off` when
disabled, `Waiting for connection` when enabled). `Connected` is part of the
model so later stages can populate it without changing the contract.

## Pairing (QR code)

Pairing makes connecting a phone simple and safe: the desktop shows a QR code, a
phone scans it, and the phone becomes a trusted device that can reconnect later
without re-pairing. Pairing is designed to be temporary and controllable.

- **Time-limited.** A pairing session lasts 5 minutes and is single-use. The QR
  code is never permanent; an expired or used code stops working and the user
  generates a new one.
- **Revocable.** The user can cancel an in-progress pairing, and can disconnect
  any trusted device at any time.
- **Opt-in.** Starting a pairing session requires Companion Mode to be enabled;
  otherwise the request fails with `COMPANION_MODE_DISABLED`.

The pairing session and the trusted-device list are held **in memory** on the
backend for this stage. Sessions are inherently ephemeral, so in-memory is the
correct model for them. Trusted devices will move to durable storage when the
network transport that can actually create them lands.

### Endpoints

All endpoints are on the loopback LocalApi (`/api/v1`):

- `POST /companion/pairing` — start a session; returns the token, QR `pairingUrl`,
  and expiry. Fails when Companion Mode is disabled.
- `GET /companion/pairing` — current session status (active + seconds remaining).
- `DELETE /companion/pairing` — cancel the active session.
- `POST /companion/pairing/confirm` — complete a session and register the calling
  device as trusted. This is the seam the future phone-over-network transport
  will expose; today it is reachable only on loopback.
- `GET /companion/devices` — list trusted devices.
- `DELETE /companion/devices/{deviceId}` — disconnect a trusted device.

The QR `pairingUrl` encodes the machine's detected LAN address and a reserved
companion port, so it is forward-compatible with the transport stage. No service
listens on that port yet; scanning the code cannot complete a connection until
the transport ships.

## Mobile interface

The phone gets its own lightweight interface rather than a copy of the desktop
UI. It is a standalone, mobile-first shell built into the existing frontend and
served at `/companion`, reusing the current React app (a PWA-style client before
any native app is considered).

- **Home** (`/companion`) is a simple control center: a header, "Connected to
  &lt;computer name&gt;" (from `GET /companion/info`), and a grid of quick actions
  — Chat, Search, Documents, Indexing, Folders.
- **Chat** (`/companion/chat`) is a working mobile RAG chat: it lazily creates a
  conversation, streams the answer from the computer's local knowledge base, and
  shows expandable sources. Multi-turn within the screen.
- **Search** (`/companion/search`) runs semantic search over the indexed
  knowledge base and lists matching snippets.
- Chat and Search are **read-only over already-indexed documents** — the phone
  does not manage files yet. They reuse the existing `chatsApi` / `searchApi`
  LocalApi slices, so no new backend endpoints were added for them.
- **Documents, Indexing, Folders** (`/companion/{action}`) are still lightweight
  placeholder screens; their phone experiences arrive in later stages.
- The shell renders without the desktop chrome (no sidebar). On the desktop, the
  Companion Mode settings section links to `/companion?preview=1` so the user can
  preview what the phone sees; the preview adds an "Exit preview" link back to
  the app.

The route exists today and is viewable in a mobile viewport. A phone can only
load it once the local-network transport ships and supplies the LocalApi base
URL (today that comes from the Tauri shell).

## Forward plan

Later stages build on this extension point without weakening the local-only
default:

1. A local-network (Wi-Fi) transport that only listens while Companion Mode is
   enabled, kept separate from the local-only `LocalApi` loopback boundary, so a
   scanned QR code can actually complete the `confirm` handshake.
2. Durable storage for trusted devices (and per-device tokens) so a paired phone
   reconnects without re-pairing across restarts.
3. Filling in the remaining mobile actions (documents, indexing, folders — these
   involve file management) and a companion bootstrap that obtains the LocalApi
   base URL over the network instead of from the Tauri shell. Chat and Search
   are already functional.
4. Capability-scoped access (chat, search, document view, indexing of selected
   files, managing allowed folders) rather than full disk access.

Each stage is intended to be useful on its own and to keep the principle that
security comes before convenience.
