# Scoped interaction contracts

`AethericContracts.Interactions` defines a small presentation vocabulary for server-side feature
packages. It has no dependency on the membership package, storage providers, Blazor, or ADR Campus.
The first consumer is ADR Campus proposal review.

## Ownership

- A feature package owns action availability, field descriptions, read models, validation messages,
  confirmation content, execution and result semantics.
- The host supplies authenticated context and navigation, selects a provider, and renders the shared
  vocabulary. It must not infer business authority from whether an action is displayed.
- The shared contract defines the vocabulary. It does not implement a workflow engine or enforce a
  feature's policies.

## Flow

1. Resolve `IInteractionProvider` in the authenticated request/circuit scope.
2. Use `GetActionsAsync(subjectId)` to obtain the available action IDs, labels, enabled states and reasons.
3. Use `Open(subjectId, actionId)` to create a new session for that subject/action.
4. `LoadAsync` supplies the initial description. The first vocabulary supports plain-text sections and
   text fields with display hints for required input and maximum length.
5. `PrepareAsync(inputs)` authorizes and validates inputs. It returns either field messages or a
   confirmation containing an opaque token and the exact values to be confirmed.
6. `ConfirmAsync(token)` authorizes again and executes the immutable prepared operation. It returns a
   structured outcome and feature-owned messages.

Display all strings as encoded text. Field hints support the UI; operations remain authoritative for
validation. A client must not construct providers, sessions, identity adapters or trusted context from
submitted payloads. These interfaces are in-process contracts, not an HTTP API.

## Confirmation and retry semantics

The session binds each confirmation token to one immutable command and operation identity. Repeated
confirmation of that token retries the same command. Neither edits to the form nor a transport failure
may silently replace it with a new operation.

If the commit result is uncertain, retain the token and permit retry while disabling edits. A provider
must also enforce this server-side. Preparing a replacement operation is allowed only after the previous
result is known. Successful preparation of a new snapshot invalidates the previous token.

Tokens are scoped to their session and do not grant authority. Every execution checks the current caller.
Implementations may serialize calls to one session to prevent edits racing with confirmation.

Outcome handling:

| Result | Host behavior |
| --- | --- |
| `Completed` | Clear editable state and show completion or follow the host's completion navigation. |
| `Invalid` | Display messages and allow correction/review. |
| `Conflict` | Display the feature's explanation; a fresh review may be required. |
| `Unavailable` | Clear protected read models and input state. |
| `RetryableFailure` | Keep the exact confirmation token and disable edits until its result is resolved. |

## Current limits

This first version supports a text form followed by confirmation. It does not define rich editors,
file uploads, dynamic layout code, persistent sessions, cross-process transport, or recovery after a
browser session is discarded. Consumers must pin a compatible contract version. Changes to this
vocabulary require coordinated consumer updates until a version-negotiation scheme exists.

The reference renderer lives in ADR Campus's independent `AdrCampus.Interactions.Blazor` library.
It depends only on this contract and ASP.NET Core, so its behavior can be tested with an unrelated
feature. Moving that renderer into a shared distribution is a separate packaging decision.
