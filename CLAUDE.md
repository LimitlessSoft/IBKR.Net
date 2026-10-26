# IBKR.Net

.NET 10 client for the Interactive Brokers TWS / IB Gateway socket API, written from scratch.
Pre-1.0 (`0.x`). Consumed by LimitlessTrading (`LT.Ibkr`).

# Rules

## ⚠️ Clean-room: no Interactive Brokers code

IB's API client is licensed by IB (GPL-3.0 from 10.50, non-commercial before). This library must
contain none of it, so apps built on it can pick their own license.

- **Never copy, translate or paraphrase IB source code** (any language, any version), and don't
  decompile IB binaries to learn the protocol.
- Learn the protocol from IB's public documentation and from **observed wire traffic**: record a
  session through `tools/IbkrWireRecorder` (a pass-through proxy) and read the frames.
- Recorded frames committed as test fixtures must be **scrubbed**: no real account ids, balances,
  positions, order ids or usernames. Replace them with obvious fakes (`DU0000001`, `100000`).

## ⚠️ Live accounts

The maintainer is usually logged in to a **real, funded IBKR account**. **Read-only** sessions
against it are pre-approved — live test, sample app, recorder, socket probe — as long as no order
frame (`3` place, `4` cancel) is ever sent: check the recording for client frames `3` / `4`
afterwards. Use a client id the app doesn't (the app uses 1 and 101). `dotnet test` is offline
and always safe.

## Orders

Placing and cancelling orders exists (0.2.0, asked for by the maintainer) at protocol version 178
only; see docs/protocol.md, "Orders". Never send an order to a live account from a test, sample or
probe without the maintainer's explicit yes for that order. Order modification is not built.

## Git

- `main` holds releases only; **a push to `main` publishes every package to nuget.org**.
- Work on the current `advance/X.Y.Z` branch (or a `feature/*` branch cut from it) and merge into
  it. Commit only when asked.
- Commits and PRs carry **no attribution trailers**.
- Publishing uses nuget.org **Trusted Publishing** (`NuGet/login`, GitHub OIDC); there is no API
  key secret.

## Code

- Layering: `IBKR.Net.Contracts` (public models, options; references nothing) → `IBKR.Net`.
- Inside `IBKR.Net`: `Transport/` (socket, handshake, framing), `Protocol/` (message ids, encoders,
  decoders), `Client/` (`IbkrClient`, request correlation, streams). Only `Client` and the
  Contracts are public; everything else is `internal` (tests see it via `InternalsVisibleTo`).
- Public types that would otherwise be ambiguous carry the `Ibkr` prefix (`IbkrClient`,
  `IbkrClientOptions`).
- One version for every package, in the root `Directory.Build.props`.
- Tests never touch the network beyond loopback: `tests/IBKR.Net.Tests/Fakes/FakeTwsServer`
  plays recorded frames.
- Format with CSharpier (`csharpier format .`): tabs, width 100. Warnings are errors.
- Every `await` in library code ends with `.ConfigureAwait(false)` (also `await foreach` /
  `await using`): an app blocking on the client from a UI thread must not deadlock. CA2007 is an
  error under `src/` (`src/.editorconfig`); `SynchronizationContextTests` cover it.

# Reference

## Protocol spec

[docs/protocol.md](docs/protocol.md) is the spec every encoder / decoder is written from: each
message as observed on the wire, field by field. Extend it from new recordings before coding a
message; mark what has not been seen yet.

## Protocol facts (observed on the wire)

- Connect, send the bytes `API\0`, then one frame `v{min}..{max}` (the client's supported version
  range). The server answers with one frame: `serverVersion`, `connectionTime`.
- Every frame after that: a 4-byte big-endian length, then NUL-terminated ASCII fields. The first
  field is the message id.
- We cap the version range below the server versions that switch messages to protobuf
  (`ProtocolVersions`), so every message stays in the text format.
- Start the API session: `71`, `2`, `clientId`, `optionalCapabilities`.
- Server time: send `49`, `1`; receive `49`, `1`, `unixSeconds`.

## Commands

```bash
dotnet build IBKR.Net.slnx
dotnet test
csharpier format .
```
