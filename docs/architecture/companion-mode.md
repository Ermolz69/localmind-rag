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

Companion Mode is built in stages and stays local-only: it opens a LAN listener
only while the user enables it, and no data leaves the machine.

What exists today:

- A persisted setting, `CompanionMode.Enabled`, stored in `app_settings` under
  the `CompanionMode.Enabled` key and exposed through the standard settings
  endpoints as part of `AppSettingsDto.CompanionMode`.
- A **Companion Mode** section in desktop Settings with an enable toggle and a
  status display.
- A **LAN gateway** that, while Companion Mode is enabled, lets a phone connect
  over Wi-Fi, loads the mobile interface, and uses an authorized slice of the API.
  `LocalApi` stays loopback. See [LAN gateway](#lan-gateway) below.
- A **QR pairing flow**: the desktop shows a short-lived QR code; the phone scans
  it, confirms, and becomes a trusted device with a per-device token. See
  [Pairing](#pairing-qr-code) below.
- **Per-device permissions**: each trusted device has its own grantable set of
  safe capabilities, with dangerous actions never available. See
  [Device permissions](#device-permissions) below.
- A **mobile companion interface**: a standalone, phone-first shell served from
  the existing frontend at `/companion`, including a working RAG **chat**,
  semantic **search**, a read-only **documents** indexing-status view,
  **watched folders** management (rescan, cleanup), **file picking** from
  allowed folders, and a near-real-time **activity feed**. See
  [Mobile interface](#mobile-interface) below.

## Settings shape

`CompanionMode` lives in `app_settings` and round-trips through the settings
endpoints like every other settings group. The configurable fields are:

- **Enabled** — whether Companion Mode is on. Defaults to `false`.
- **AllowedFolders** — absolute folder paths the phone may browse and pick files
  from (see [Allowed folders](#allowed-folders-and-file-picking)). Defaults to
  empty.

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

The pairing **session** is held in memory — sessions are inherently ephemeral
(single-use, 5-minute TTL), so that is the correct model for them. **Trusted
devices are persisted** in the database (`companion_devices` table), so a paired
phone survives an app restart and reconnects without re-pairing. Only the hash of
each device's token is stored (SHA-256), never the token itself: a leaked database
cannot be used to impersonate a device. Disconnecting a device deletes its row, so
its token stops authenticating immediately.

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

## Device permissions

Not every connected device needs the same abilities, so each trusted device has
its own permission set. Only safe capabilities are grantable; dangerous actions
are never available to a phone.

- Grantable (and on by default for a newly paired device): **Chat**, **Search**,
  **View documents**, **View indexing status**, **Rescan folders**, **Add files**
  (from allowed folders).
- **Never** grantable to a phone: deleting documents, changing the AI runtime,
  changing system paths, managing app settings, or browsing the whole disk. These
  are not fields in the permission model at all, and their loopback routes are off
  the gateway allowlist — so they are denied two ways over, not merely toggled off.
  The desktop trusted-devices view lists them under **Never allowed** so the hard
  boundary is visible, not just implied.

`CompanionDeviceDto` carries a `Permissions` record; `PUT
/companion/devices/{id}/permissions` updates it, and the desktop trusted-devices
list exposes per-capability toggles. Permissions are persisted with the device, so
a trusted phone stays exactly as restricted (or as capable) as the user left it
across restarts.

### Enforcement is real

Permissions are not a cosmetic setting — the [LAN gateway](#lan-gateway) enforces
them on **every** phone request:

- `CompanionRoutePolicy` maps each request (path + method) to either a required
  capability, the pairing bootstrap, or *blocked*. No matching token → **401**; a
  trusted device that lacks the route's capability → **403**; a route that is not
  on the allowlist (e.g. document delete, settings, runtime) → **404**.
- The check reads the device's permissions **fresh from the database per request**,
  so toggling a capability off — or disconnecting the device — takes effect on the
  phone's very next request, with no need to re-pair or restart.
- The desktop talks to the loopback LocalApi directly, so these limits apply only
  to phones, never to the computer itself.

This is covered by `CompanionRoutePolicyTests` (every route → capability, and every
dangerous route blocked) and the gateway pipeline tests (401/403/404 wiring).

## Mobile interface

The phone gets its own lightweight interface rather than a copy of the desktop
UI. It is a standalone, mobile-first shell built into the existing frontend and
served at `/companion`, reusing the current React app (a PWA-style client before
any native app is considered).

- **Home** (`/companion`) is a simple control center: a header, "Connected to
  &lt;computer name&gt;" (from `GET /companion/info`), and a grid of quick actions
  — Chat, Search, Documents, Files, Folders, Activity, Indexing.
- **Chat** (`/companion/chat`) is a working mobile RAG chat: it lazily creates a
  conversation, streams the answer from the computer's local knowledge base, and
  shows expandable sources. Multi-turn within the screen.
- **Search** (`/companion/search`) runs semantic search over the indexed
  knowledge base and lists matching snippets.
- **Documents** (`/companion/documents`) is a read-only view of the knowledge
  base: each document's lifecycle status, live indexing progress (e.g.
  "Processing · 75%"), and failure reasons, plus a counts summary (ready /
  processing / waiting / failed). It polls while work is in flight. Actions
  (retry, cancel, reindex) are intentionally deferred to a later stage. Both this
  view and the Files "Recently added" strip share one lifecycle vocabulary
  (`resolveDocumentPhase` in `entities/document`): **accepted → waiting to
  process → processing → ready to search / couldn't process**, so the many
  internal status names map to five states the user can understand.
- **Files** (`/companion/files`) lets the phone browse the **allowed folders**
  the user shared on the computer and add a chosen file into LocalMind for
  indexing. The file is added by path — it is **not** downloaded to the phone —
  and browsing is strictly confined to allowed roots (see
  [Allowed folders](#allowed-folders-and-file-picking)). Adding goes through the
  normal `UploadDocumentHandler` pipeline (same as the desktop), and the added
  file then appears in a **Recently added** strip on the same screen with its
  **live lifecycle status**, so the loop closes without leaving Files.
- **Activity** (`/companion/activity`) is a near-real-time feed of what LocalMind
  is doing — files added, text extraction, embeddings, indexed/failed, watched
  folder finds, and device connect/disconnect (see
  [Activity feed](#activity-feed)).
- **Watched folders** (`/companion/folders`) lets the phone manage the folders
  already allowed on the computer: view each folder's health, document count,
  and access errors; **rescan** a folder (or all) to pick up new files; and
  **clean up** records of deleted files. By design the phone **cannot add new
  folders from disk** — it only acts on what the computer permits.
- Chat, Search, and Documents are read-only; Files and Watched folders add
  state-changing actions (add file, rescan, cleanup). They reuse the existing
  `chatsApi` / `searchApi` / `documentsApi` / `ingestionApi` / `watchedFoldersApi`
  / `companion` file slices over LocalApi.
- **Indexing** (`/companion/{action}`) is still a lightweight placeholder
  screen; its phone experience arrives in a later stage.
- The shell renders without the desktop chrome (no sidebar). On the desktop, the
  Companion Mode settings section links to `/companion?preview=1` so the user can
  preview what the phone sees; the preview adds an "Exit preview" link back to
  the app.

A phone loads this interface from the **LAN gateway** over Wi-Fi (see
[LAN gateway](#lan-gateway) below). When the SPA runs outside Tauri it points its
API client at its own origin (the gateway) instead of the Tauri shell, completes
pairing from the QR `?token=`, and stores a per-device token for later requests.

## LAN gateway

A phone reaches LocalMind over the local Wi-Fi through the **Companion Gateway** —
a second HTTP listener hosted inside the LocalApi process but separate from the
loopback LocalApi pipeline. `LocalApi` itself stays loopback
([ADR 0009](decisions/0009-localapi-local-security.md)); the gateway is the only
LAN surface, and it runs **only while Companion Mode is enabled** (an
`IHostedService` starts/stops it on the settings-change signal).

```
Phone browser ──HTTP──> Companion Gateway (0.0.0.0:49322, in the LocalApi process)
                          • serves the built SPA
                          • device-token auth + per-device permission allowlist
                          • reverse-proxies allowed /api/v1/* ──> 127.0.0.1 (LocalApi)
```

- **Connect flow:** enable Companion Mode → Connect phone → the QR encodes
  `http://<lan-ip>:49322/companion?token=<pairing-token>`. The phone opens it,
  loads the SPA from the gateway, calls `POST /companion/pairing/confirm` with the
  pairing token, and receives a durable **per-device token** it stores and sends
  as `Authorization: Bearer …` thereafter.
- **Authorization:** every API request except the pairing bootstrap requires a
  valid device token; each route maps to a capability and is rejected when the
  device lacks it. Only an allowlist of safe routes is exposed (chat, search, read
  documents, read ingestion status, watched-folder status/rescan/cleanup, file
  picking, activity). Anything else returns 404 from the gateway.
- The gateway strips the device token and forwards the configured loopback token,
  so the loopback LocalApi keeps its existing security. See
  [ADR 0012](decisions/0012-companion-lan-gateway.md).

Transport is plain HTTP on the trusted local network for now. Device tokens are
persisted (hashed) so a paired phone reconnects across restarts; HTTPS on the LAN
remains a later step.

## Activity feed

So the phone feels live rather than a set of static pages, LocalMind keeps a
small in-memory feed of recent activity that the phone polls (~4s):

- Document lifecycle: **added**, **extracting text**, **creating embeddings**,
  **indexed successfully**, **failed** (with the reason).
- **Watched folder found** a new file.
- **Device connected / disconnected.**

The feed is an in-memory ring buffer (newest first, capped) behind
`ICompanionActivityFeed`. It is populated best-effort from the points that
already drive these flows — the upload handler, the ingestion processor's step
transitions, the watched-file ingestion service, and the pairing service — so
publishing never disrupts the main work. `GET /companion/activity` returns the
recent events. Because it is in memory, the feed resets on restart; persisting it
is not required for a live view.

## Allowed folders and file picking

A core idea of Companion Mode is picking files on the computer from the phone
without downloading them. The security rule is strict: **the phone never sees
the whole disk — only the folders the user explicitly allowed on the computer.**

- The user maintains an **allowed folders** list in the desktop Companion Mode
  settings (`CompanionMode.AllowedFolders`, persisted in `app_settings`). Folders
  are added with the native folder picker; the phone can never add folders.
- `GET /companion/files/roots` returns the allowed roots. `GET
  /companion/files/browse?path=…` lists subfolders and supported files inside a
  root. `POST /companion/files/add` adds a chosen file.
- Every browse/add path is canonicalized and checked to be inside an allowed root
  (rejecting `..` traversal). Paths outside the allowed roots return a generic
  not-found so the phone cannot probe the disk. Adding reuses the normal upload
  pipeline (`UploadDocumentHandler`), so file-type and size limits still apply.
- Companion Mode is deliberately not a full remote file explorer; access is
  bounded to allowed roots.
- The phone presents this as a simple "Files on this PC" screen: it lists the
  allowed folders by name, navigates with a breadcrumb shown **relative to the
  allowed root** (e.g. `Study / AI / Lectures`, never the full disk path), and each
  supported file has an **Add to LocalMind** action. The desktop allowed-folders
  list mirrors this, showing each folder's name with its full path beneath.

## Forward plan

The local-network transport — a phone actually connecting over Wi-Fi with
device-token auth and enforced permissions — now exists via the
[LAN gateway](#lan-gateway), and trusted devices persist (with hashed tokens) so a
paired phone reconnects across restarts without re-pairing. Later stages build on
it without weakening the local-only default:

1. Document actions from the phone (retry, cancel, reindex) on top of the
   read-only Documents view, and the remaining Indexing action.
2. Pushing the activity feed (SSE/WebSocket) over the gateway instead of polling,
   and HTTPS on the LAN.

Each stage is intended to be useful on its own and to keep the principle that
security comes before convenience.

## Remote access (future direction)

Everything above works only on the local Wi-Fi network. Reaching LocalMind from
outside the home is a future direction, **not** part of the MVP — it should be
pursued only after local companion mode is stable and useful.

LocalMind must **not** be exposed directly to the internet (no port-forwarding,
no public bind). The intended direction is an intermediate relay:

```text
Phone
   ↓ (secure channel)
Relay / Sync service
   ↓ (outbound connection initiated by the computer)
Computer running LocalMind
   ↓
Local database and files
```

The computer dials out to the relay (no inbound ports), the phone reaches the
relay over a secure channel, and the relay only brokers an encrypted
command/response stream — the documents and database stay on the computer. The
relay is opt-in and never required for local-network use. This preserves the
local-first architecture while opening a path to convenient remote use. See
[ADR 0011](decisions/0011-companion-remote-access-via-relay.md).
