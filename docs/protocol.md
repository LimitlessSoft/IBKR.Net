# Wire protocol (observed)

What IBKR.Net knows about the TWS / IB Gateway socket protocol, **learned only from recorded
traffic** (`tools/IbkrWireRecorder`, see CLAUDE.md). Every message below was seen on the wire at
**protocol version 187** against IB Gateway 10.50 on 2026-10-24. Field meanings are read from the
values; example values are fake.

Notation: `[n]` is the field index in the frame, `[0]` is the message id. `""` is an empty field.
Unless noted, the second field is a per-message version number the server sends and ignores.

## Encoding rules

| Rule | Observed |
|---|---|
| Framing | 4-byte big-endian length, then NUL-terminated ASCII fields. |
| Unset number | Empty field (`""`) in requests, e.g. a contract without a strike. `0` means zero. |
| Unset number from the server | `1.7976931348623157E308` (double max) — e.g. P&L not available yet. |
| Booleans | `0` / `1`. |
| Doubles | Invariant culture, `.` decimal separator, as many decimals as needed (`759.9000`, `0.01`). |
| Lists | A count field, then that many fields (secdef expirations / strikes), or a comma-separated field (valid exchanges, order types). |
| Times | Unix seconds (`currentTime`, bar times with format 2), or `yyyyMMdd HH:mm:ss Zone`. |

## Session

| Direction | Frame |
|---|---|
| C → S | bytes `API\0`, then one frame whose payload is the text `v100..187` (no NUL). |
| S → C | `187` (agreed version), `20261024 12:00:00 EST` (server time at connect). |
| C → S | `71` StartApi: `[1]` 2, `[2]` clientId, `[3]` optional capabilities (`""`). |
| S → C | `15` ManagedAccounts: `[1]` 1, `[2]` `DU0000001,DU0000002,` (trailing comma). |
| S → C | `9` NextValidId: `[1]` 1, `[2]` next order id. Marks the session as ready. |
| S → C | `4` Error with id `-1` and codes 2104 / 2106 / 2158 (farm status notices), see Errors. |

## Errors — `4` (S → C)

`[1]` 2, `[2]` request id (`-1` = not tied to a request), `[3]` code, `[4]` message,
`[5]` advanced reject JSON (`""`).

| Code | Seen when |
|---|---|
| 200 | No security definition (unknown contract, or a contract sent with strike 0). |
| 2104, 2106, 2158 | Informational: market data / HMDS / sec-def farm connection OK. |
| 2150 | Informational on reqPnLSingle: "Invalid position trade derived value"; data still follows. |

## Server time

- C → S `49`: `[1]` 1.
- S → C `49`: `[1]` 1, `[2]` unix seconds. No request id: replies come in request order.

## Market data type

- C → S `59`: `[1]` 1, `[2]` type (1 live, 2 frozen, 3 delayed, 4 delayed-frozen).

## Contract fields in requests

Requests that carry a contract use the same block of 12 fields, here starting at `[c]`:

| Offset | Field | Example |
|---|---|---|
| c+0 | conId (`0` or `""` = unset) | `756733` |
| c+1 | symbol | `SPY` |
| c+2 | security type | `STK`, `OPT`, `IND`, `FUT`, `CASH` |
| c+3 | last trade date or contract month | `20261026`, `202610` |
| c+4 | strike (`""` = unset — **never `0`**) | `705` |
| c+5 | right | `C`, `P` |
| c+6 | multiplier | `100` |
| c+7 | exchange | `SMART` |
| c+8 | primary exchange | `ARCA` |
| c+9 | currency | `USD` |
| c+10 | local symbol | `SPY   261026C00705000` |
| c+11 | trading class | `SPY` |

## Contract details

- C → S `9` (v8): `[1]` 8, `[2]` reqId, `[3..14]` contract block (c = 3), `[15]` include expired
  (`0`), `[16]` sec id type, `[17]` sec id, `[18]` issuer id.
- S → C `10`, one per matching contract (no version field: `[1]` is the reqId):

| Index | Field | STK example | OPT example |
|---|---|---|---|
| 1 | reqId | `2` | `5` |
| 2 | symbol | `SPY` | `SPY` |
| 3 | security type | `STK` | `OPT` |
| 4 | last trade date + time | `""` | `20261026 16:15:00 US/Eastern` |
| 5 | last trade date | `""` | `20261026` |
| 6 | strike | `0` | `705` |
| 7 | right | `""` | `C` |
| 8 | exchange | `SMART` | `SMART` |
| 9 | currency | `USD` | `USD` |
| 10 | local symbol | `SPY` | `SPY   261026C00705000` |
| 11 | market name | `SPY` | `SPY` |
| 12 | trading class | `SPY` | `SPY` |
| 13 | conId | `756733` | `933461775` |
| 14 | min tick | `0.01` | `0.01` |
| 15 | multiplier | `""` | `100` |
| 16 | order types (comma list) | … | … |
| 17 | valid exchanges (comma list) | … | … |
| 18 | price magnifier | `1` | `1` |
| 19 | underlying conId | `0` | `756733` |
| 20 | long name | `SS SPDR S&P 500 ETF TRUST-US` | same |
| 21 | primary exchange | `ARCA` | `""` |
| 22 | contract month | `""` | `202610` |
| 23–25 | industry, category, subcategory | `""` | `""` |
| 26 | time zone | `US/Eastern` | `US/Eastern` |
| 27 | trading hours | `20261026:0400-20261026:2000;…` | … |
| 28 | liquid hours | `20261026:0930-20261026:1600;…` | … |
| 29–30 | ev rule, ev multiplier | `""` | `""` |
| 31 | sec id count `n` | `1` | `0` |
| 32… | n pairs: sec id type, sec id | `ISIN`, `US0000000000` | — |

After the sec ids the STK frame has 10 more fields (aggregated group, underlying symbol / type,
market rule ids, real expiration date, stock type `ETF`, size increments …) and the OPT frame 9;
they are not needed yet and are skipped.

- S → C `52` ContractDetailsEnd: `[1]` 1, `[2]` reqId.

## Option chain parameters

- C → S `78`: `[1]` reqId, `[2]` underlying symbol, `[3]` futures exchange (`""` for stocks),
  `[4]` underlying security type, `[5]` underlying conId. (No version field.)
- S → C `75`, one per exchange and trading class: `[1]` reqId, `[2]` exchange, `[3]` underlying
  conId, `[4]` trading class, `[5]` multiplier, `[6]` expiration count `n`, then n expirations
  (`yyyyMMdd`), then a strike count `m` and m strikes.
- S → C `76` End: `[1]` reqId.

## Market data

- C → S `1` (v11): `[1]` 11, `[2]` reqId, `[3..14]` contract block (c = 3), `[15]` delta-neutral
  combo flag (`0`), `[16]` generic tick list (`""`), `[17]` snapshot (`0`), `[18]` regulatory
  snapshot (`0`), `[19]` options (`""`).
- C → S `2` Cancel: `[1]` 1, `[2]` reqId.
- S → C, in this order after a request:
  - `81` TickReqParams: `[1]` reqId, `[2]` min tick, `[3]` BBO exchange, `[4]` snapshot
    permissions.
  - `58` MarketDataType: `[1]` 1, `[2]` reqId, `[3]` type actually served.
  - `1` TickPrice: `[1]` 6, `[2]` reqId, `[3]` tick type, `[4]` price (`-1.00` = none),
    `[5]` size, `[6]` attribute bitmask.
  - `2` TickSize: `[1]` 6, `[2]` reqId, `[3]` tick type, `[4]` size.
  - `45` TickGeneric: `[1]` 6, `[2]` reqId, `[3]` tick type, `[4]` value.
  - `46` TickString: `[1]` 6, `[2]` reqId, `[3]` tick type, `[4]` value (seen 2026-10-24 on
    crypto: type 45, last trade time in unix seconds).

Tick types seen: 0 bid size, 1 bid, 2 ask, 3 ask size, 4 last, 5 last size, 6 high, 7 low,
8 volume, 9 close, 45 last timestamp, 49 halted. Sizes may be fractional (crypto: `0.00039253`).

## Historical bars

- C → S `20`: `[1]` reqId, `[2..13]` contract block (c = 2), `[14]` include expired (`0`),
  `[15]` end date-time (`""` = now), `[16]` bar size (`5 mins`), `[17]` duration (`1 D`),
  `[18]` regular trading hours only (`1`), `[19]` what to show (`TRADES`), `[20]` date format
  (`2` = unix seconds), `[21]` keep up to date (`0`), `[22]` chart options (`""`).
- S → C `17`: `[1]` reqId, `[2]` start, `[3]` end, `[4]` bar count `n`, then n bars of 8 fields:
  time, open, high, low, close, volume, WAP, trade count. With date format 2 the time is unix
  seconds for intraday bars and `yyyyMMdd` for daily bars (`1 day`, seen 2026-10-24).

## Real-time bars

- C → S `50` (v3): `[1]` 3, `[2]` reqId, `[3..14]` contract block (c = 3), `[15]` bar size (`5`),
  `[16]` what to show, `[17]` regular trading hours only, `[18]` options (`""`).
- C → S `51` Cancel: `[1]` 1, `[2]` reqId.
- S → C `50` bar, every 5 seconds: `[1]` 3, `[2]` reqId, `[3]` time (unix seconds), `[4]` open,
  `[5]` high, `[6]` low, `[7]` close, `[8]` volume, `[9]` WAP, `[10]` trade count. `MIDPOINT`
  bars have `""` volume and WAP and count `-1`. Volume may be in exponent form (`6.2E-7`).
  Seen 2026-10-24 on BTC (PAXOS, trades 24/7).

## Positions

- C → S `61` Request: `[1]` 1. C → S `64` Cancel: `[1]` 1.
- S → C `61` Position: `[1]` 3, `[2]` account, `[3]` conId, `[4]` symbol, `[5]` security type,
  `[6]` last trade date, `[7]` strike, `[8]` right, `[9]` multiplier, `[10]` exchange,
  `[11]` currency, `[12]` local symbol, `[13]` trading class, `[14]` position, `[15]` average cost.
- S → C `62` End: `[1]` 1.

## Account summary

- C → S `62` Request: `[1]` 1, `[2]` reqId, `[3]` group (`All`), `[4]` comma-separated tags.
- C → S `63` Cancel: `[1]` 1, `[2]` reqId.
- S → C `63` Value: `[1]` 1, `[2]` reqId, `[3]` account, `[4]` tag, `[5]` value, `[6]` currency.
- S → C `64` End: `[1]` 1, `[2]` reqId. Values keep coming after End until cancelled.

## P&L

- C → S `92` Account P&L: `[1]` reqId, `[2]` account, `[3]` model code (`""`). Cancel `93`:
  `[1]` reqId.
- S → C `94`: `[1]` reqId, `[2]` daily, `[3]` unrealized, `[4]` realized.
- C → S `94` Position P&L: `[1]` reqId, `[2]` account, `[3]` model code, `[4]` conId. Cancel
  `95`: `[1]` reqId.
- S → C `95`: `[1]` reqId, `[2]` position, `[3]` daily, `[4]` unrealized, `[5]` realized,
  `[6]` value; unset = double max.

## Orders (version 178)

Recorded 2026-10-25 at protocol version **178**, the highest an independent client (ib_async,
`v157..178`) offers. placeOrder / cancelOrder frames come from that client: sent to IB Gateway
(live, orders rejected or cancelled before they worked, nothing filled) and to a local stand-in
that only did the handshake, so variants could be diffed without trading. Versions 179–187 may add
order fields that have not been seen, so the library places orders only on a connection capped at
178 (`IbkrClientOptions.MaxServerVersion = IbkrClient.OrderProtocolVersion`).

- C → S `3` Place order, 114 fields:
  - `[1]` order id (≥ the server's next valid id, see below)
  - `[2..13]` contract: conId, symbol, security type, last trade date, strike (`0.0` when unset),
    right, multiplier, exchange, primary exchange, currency, local symbol, trading class
  - `[14]` security id type, `[15]` security id (both `""`)
  - `[16]` action `BUY` / `SELL`, `[17]` quantity (`0` for a cash quantity order)
  - `[18]` order type `LMT` / `MKT` / `STP`, `[19]` limit price, `[20]` aux (stop) price; unset
    prices are `""`
  - `[21]` time in force `DAY` / `GTC` / `IOC`; `[22]` not located (`""`); `[23]` account
  - `[33]` outside regular hours `0` / `1`; `[100]` cash quantity (`10` = $10, `""` unset)
  - every other field of `[24..113]` was identical in every variant and is sent as seen:
    `[24]` `O`, `[27]` `1`, `[36]` `0.0`, `[45]` `-1`, the rest `0` or `""` (see `OrderEncoder`).
  - A what-if order sets one of the unlocated flags to `1`; IB Gateway did not answer what-if
    crypto orders at all, so what-if is not offered.
- C → S `4` Cancel order: `[1]` 1, `[2]` order id, `[3]` `""`.
- S → C `3` Order status: `[1]` order id, `[2]` status, `[3]` filled, `[4]` remaining, `[5]`
  average fill price, `[6]` perm id, `[7]` parent id, `[8]` last fill price, `[9]` client id,
  `[10]` why held, `[11]` market cap price; empty numbers are unset. Seen: `ApiCancelled` (cancel
  before IB accepted the order). Other statuses (`PreSubmitted`, `Submitted`, `Filled`,
  `Cancelled`, `Inactive`) are from IB's documentation, not seen yet.
- A rejection is only an error `4` for the order id, without a status: seen 10052 "Invalid time in
  force" (crypto needs GTC or IOC), 435 "You must specify an account" (login with several
  accounts), 460 "No trading permissions", and 10147 for a cancel of an unknown order.
- Error 202 "Order Canceled" and 399 "Order Message" (a warning) are treated as notices for a
  working order, after IB's documentation; not seen yet.
- S → C `9` NextValidId `[2]` is the first usable order id; the client continues its request
  id counter from it, so request and order ids never collide.
- S → C `102` (no fields) arrives after the session starts at version 178; meaning unknown,
  ignored.
- Before the first crypto order, IB Gateway shows a "Cryptocurrency order confirmation" dialog
  and holds the order (no reply) until someone clicks it.
- Open order (`5`), execution and commission frames: **not seen yet** (no order was accepted).

## Orders and executions (read-only)

- C → S `5` Open orders: `[1]` 1. S → C `53` OpenOrderEnd: `[1]` 1. Order frames themselves:
  **not seen yet** (no open orders when recorded).
- C → S `7` Executions: `[1]` 3, `[2]` reqId, `[3]` client id filter (`0`), `[4..9]` account,
  time, symbol, security type, exchange, side filters (all `""`). S → C `55` End: `[1]` 1,
  `[2]` reqId. Execution / commission frames: **not seen yet** (no fills that day).
