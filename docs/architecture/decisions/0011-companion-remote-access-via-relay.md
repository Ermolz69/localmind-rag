# ADR 0011: Companion Remote Access via Outbound Relay

## Status
Accepted — future direction (not yet implemented)

## Context
Companion Mode (Stages 0–9) works only on the local Wi-Fi network: the desktop
keeps `KnowledgeApp.LocalApi` on loopback, and the planned companion transport
listens on the LAN only while the mode is enabled. Users will eventually want to
reach LocalMind from outside the home.

The tempting shortcut — exposing LocalApi (or the companion LAN listener)
directly to the internet via port-forwarding or a public bind — is unsafe and
breaks the project's principles: it opens inbound ports on a machine holding the
user's documents and database, enlarges the attack surface, forces NAT/firewall
configuration on users, and contradicts local-first and "security before
convenience." This must not happen.

Remote access is also explicitly **not** part of the MVP. It should only be
pursued after local companion mode is stable and useful.

## Decision
Remote access will use an intermediate **relay/sync service** rather than direct
internet exposure:

```text
Phone
   ↓ (secure channel)
Relay / Sync service
   ↓ (outbound connection initiated by the computer)
Computer running LocalMind
   ↓
Local database and files
```

- The computer running LocalMind establishes an **outbound** connection to the
  relay. No inbound ports are opened on the user's machine.
- The phone reaches the relay over a secure channel; the relay brokers an
  encrypted command/response stream to the computer. It is a transport broker,
  not a data store — the knowledge base never lives on the relay.
- The computer stays the authoritative runtime: documents, database, indexing,
  search, and AI all remain local. The relay only forwards companion commands.
- Access stays gated by the existing model: Companion Mode must be enabled, and
  the connecting device's [permissions](../companion-mode.md#device-permissions)
  still apply. `LocalApi` continues to bind loopback and is never exposed to the
  LAN or internet ([ADR 0009](./0009-localapi-local-security.md) is unchanged).
- The relay is **opt-in** infrastructure and must never become required for the
  local-first, local-network workflows.

## Consequences
- The project has a clear path to remote use that preserves local-first: the
  data never leaves the computer, and the machine never accepts inbound
  connections.
- It adds a dependency on a relay service. The repository already isolates
  remote/online concerns (`KnowledgeApp.SyncApi`, `services/`, `infra/`), which
  gives this direction a foundation, but those services must stay optional.
- Channel security must be designed so the relay cannot read user content
  (end-to-end encryption between phone and computer, plus relay authentication
  and per-device authorization).
- This is post-MVP: implement only after local companion mode is stable. Until
  then, Companion Mode remains local-network only.

## Related
- [ADR 0009: LocalApi Local Security](./0009-localapi-local-security.md)
- [ADR 0010: Offline-first sync outbox](./0010-offline-first-sync-outbox.md)
- [Companion Mode](../companion-mode.md)
- [Security and sync](../security-and-sync.md)
