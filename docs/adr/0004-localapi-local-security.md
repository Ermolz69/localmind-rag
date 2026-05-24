# ADR 0004: LocalApi Local Security

## Status
Accepted

## Context
LocalApi is intended for the desktop app on the same machine. Browser-accessible local HTTP services should reject remote access and optionally protect mutating endpoints with a local secret.

## Decision
LocalApi enforces loopback host/remote checks by default. When a local token is configured, mutating requests must include `X-LocalMind-Token`. Health, OpenAPI, static documentation, and explicitly exempt assets stay accessible without the token.

## Consequences
Desktop builds can enable token protection without changing endpoint URLs. Security failures return the standard API envelope with stable security error codes.
