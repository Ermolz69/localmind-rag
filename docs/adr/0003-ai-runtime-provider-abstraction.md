# ADR 0003: AI Runtime Provider Abstraction

## Status
Accepted

## Context
LocalMind currently uses llama.cpp for local embedding/runtime operations, but runtime status and capabilities should not be hard-coded into endpoint logic.

## Decision
Runtime providers advertise a stable provider id, display name, status, capabilities, setup guidance, paths, and model listing support through application-level provider contracts. The first provider is llama.cpp.

## Consequences
Runtime API responses can describe capabilities consistently, and future providers can be added behind the same application contract.
