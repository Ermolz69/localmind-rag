# ADR 0012: Companion LAN Gateway

## Status
Accepted

## Context
Companion Mode v1 built the entire phone-facing UI, but it only ran as an
on-computer preview: the SPA is loaded via `tauri://` and the frontend's API base
URL comes from Tauri, so a phone on the same Wi-Fi could not reach it.
`KnowledgeApp.LocalApi` binds to loopback and must never be exposed to the LAN
([ADR 0009](./0009-localapi-local-security.md)). v2 needs a phone to really
connect over the local network, securely, without weakening that boundary.

## Decision
Introduce a **Companion Gateway**: a second Kestrel listener, hosted inside the
LocalApi process but separate from the loopback LocalApi pipeline, bound to the
LAN (`0.0.0.0:49322`). It runs **only while Companion Mode is enabled** — an
`IHostedService` starts/stops it in response to the settings-change signal.

- It serves the built SPA to the phone and reverse-proxies an **allowlist** of
  safe API routes to the loopback LocalApi. Routes outside the allowlist (settings,
  runtime, document upload/delete/reindex, buckets, notes, device/pairing
  management) are not reachable from the gateway.
- Requests are authenticated by a **per-device token** issued when a phone
  completes pairing, and the device's **permissions** (ADR-less feature from v1)
  are enforced per route. Pairing confirmation and a small info endpoint are the
  only unauthenticated routes.
- The proxy strips the device token and attaches the configured loopback token, so
  the loopback LocalApi keeps its existing security. `LocalApi` stays loopback.
- Transport is plain **HTTP on the local Wi-Fi** for this stage; the phone reaches
  the gateway by its own origin (no CORS). HTTPS is a future hardening.

## Consequences
- A phone can connect over Wi-Fi without exposing LocalApi or asking users to
  open inbound ports/port-forwarding. The gateway is opt-in and closed by default.
- Plaintext HTTP on the LAN is an accepted trade-off on a trusted home network.
- Device tokens (and the trusted-device list) are in memory for now; persisting
  them across restart is a later v2 stage.
- The gateway must serve the built SPA from disk, so packaging needs to place the
  built `dist` where the gateway can read it (`CompanionGateway:StaticPath`).
- The internet relay direction ([ADR 0011](./0011-companion-remote-access-via-relay.md))
  is unchanged and still future.

## Related
- [ADR 0009: LocalApi Local Security](./0009-localapi-local-security.md)
- [ADR 0011: Companion Remote Access via Outbound Relay](./0011-companion-remote-access-via-relay.md)
- [Companion Mode](../companion-mode.md)
