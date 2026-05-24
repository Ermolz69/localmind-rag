# ADR 0001: Modular Monolith

## Status
Accepted

## Context
LocalMind is a local-first desktop product with one user, one local database, and a backend sidecar running on the same machine. Splitting backend capabilities into distributed services would add deployment, networking, observability, and versioning cost without improving the MVP.

## Decision
The backend is a modular monolith. Business boundaries are represented by feature folders and project boundaries instead of separate processes. `KnowledgeApp.LocalApi` exposes HTTP, `KnowledgeApp.Application` owns use cases and ports, `KnowledgeApp.Domain` owns business entities, `KnowledgeApp.Infrastructure` implements adapters, and `KnowledgeApp.Contracts` owns public DTOs.

## Consequences
Feature flows are easy to navigate and test in one solution. Cross-feature boundaries are enforced by architecture tests and application ports. If remote sync or hosted services grow later, they can be extracted behind existing contracts without changing the desktop UI contract.

## Related
- [Backend architecture](../backend.md)
- [System overview](../system.md)
