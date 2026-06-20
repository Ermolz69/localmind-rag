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

This is the first stage of Companion Mode: a managed, persisted mode plus a safe
extension point for a future phone client. It deliberately does **not** open a
network listener, pair a device, or move any data off the machine yet.

What exists today:

- A persisted setting, `CompanionMode.Enabled`, stored in `app_settings` under
  the `CompanionMode.Enabled` key and exposed through the standard settings
  endpoints as part of `AppSettingsDto.CompanionMode`.
- A **Companion Mode** section in desktop Settings with an enable toggle and a
  status display.

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

## Forward plan

Later stages build on this extension point without weakening the local-only
default:

1. A local-network (Wi-Fi) transport that only listens while Companion Mode is
   enabled, kept separate from the local-only `LocalApi` loopback boundary.
2. Device pairing and authorization so only an approved phone can connect.
3. A mobile web (PWA) client that reuses the existing frontend before any native
   app is considered.
4. Capability-scoped access (chat, search, document view, indexing of selected
   files, managing allowed folders) rather than full disk access.

Each stage is intended to be useful on its own and to keep the principle that
security comes before convenience.
