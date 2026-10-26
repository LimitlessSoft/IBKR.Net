# IBKR.Net

A .NET 10 client for the Interactive Brokers **TWS / IB Gateway** socket API — written from
scratch, with **no Interactive Brokers code inside**, under the MIT license.

> Early development (`0.x`): the API is not stable yet.

## Why

IB's own API client is licensed by IB (non-commercial in older releases, GPL-3.0 from 10.50), and
every .NET wrapper we know of bundles it. IBKR.Net talks to TWS / IB Gateway over its local
socket itself, so an app built on it is free to choose its own license.

You still need **TWS or IB Gateway** running and logged in: it is the program that connects to
IB's servers, and IBKR.Net connects to it on `127.0.0.1` (Gateway: 4001 live / 4002 paper).

## Packages

| Package | Purpose |
|---|---|
| `IBKR.Net.Contracts` | Models and options; no dependencies |
| `IBKR.Net` | The client |

## Quick start

```csharp
await using var client = await IbkrClient.ConnectAsync(
	new IbkrClientOptions { Host = "127.0.0.1", Port = 4002, ClientId = 1 }
);

var spy = (await client.GetContractDetailsAsync(
	new Contract { Symbol = "SPY", SecurityType = "STK", Exchange = "SMART", Currency = "USD" }
))[0].Contract;

await client.SetMarketDataTypeAsync(MarketDataType.Delayed);
await foreach (var tick in client.SubscribeMarketDataAsync(spy))
	Console.WriteLine($"{tick.Field}: {tick.Price} {tick.Size}");
```

Streams cancel their IB request when you stop reading; errors IB reports for a request throw
`IbkrException`, informational messages arrive on `client.Notice`.

## What it covers (0.8.0)

| `IbkrClient` | IB request |
|---|---|
| `ConnectAsync`, `ManagedAccounts`, `Disconnected`, `Notice` | session, `managedAccounts`, errors |
| `GetServerTimeAsync` | `reqCurrentTime` |
| `GetContractDetailsAsync` | `reqContractDetails` |
| `GetOptionChainParametersAsync` | `reqSecDefOptParams` |
| `SetMarketDataTypeAsync`, `SubscribeMarketDataAsync` | `reqMarketDataType`, `reqMktData` |
| `GetHistoricalBarsAsync` | `reqHistoricalData` |
| `SubscribeRealtimeBarsAsync` | `reqRealTimeBars` |
| `GetPositionsAsync`, `SubscribePositionsAsync` | `reqPositions` |
| `SubscribeAccountSummaryAsync` | `reqAccountSummary` |
| `SubscribeAccountPnlAsync`, `SubscribePositionPnlAsync` | `reqPnL`, `reqPnLSingle` |
| `PlaceOrderAsync`, `CancelOrderAsync` (version 178 only) | `placeOrder`, `cancelOrder` |
| `GetOpenOrdersAsync` | `reqAllOpenOrders` |
| `GetExecutionsAsync` | `reqExecutions` with commission reports |
| `GetNewsProvidersAsync`, `GetHistoricalNewsAsync`, `GetNewsArticleAsync` | `reqNewsProviders`, `reqHistoricalNews`, `reqNewsArticle` |
| `SubscribeNewsAsync`, `GetBroadTapeContractsAsync` | `reqMktData` news ticks (292), broad tape contracts |
| `SubscribeMarketDepthAsync`, `GetMarketDepthExchangesAsync` | `reqMktDepth` (one exchange, Level 2, SMART depth), `reqMktDepthExchanges` |
| `SubscribeTickByTickAsync` | `reqTickByTickData` (Last, AllLast, BidAsk, MidPoint; optional history first) |
| `GetWshMetaDataAsync`, `GetWshEventDataAsync` | `reqWshMetaData`, `reqWshEventData` (Wall Street Horizon corporate events as JSON; needs IB's WSH subscription, else error 10276) |

It speaks protocol version 187 (text messages), which current TWS / IB Gateway still accept.

Orders trade for real on a live account. They need a connection capped at
`IbkrClient.OrderProtocolVersion` (178); modifying an order and what-if orders are not built.

**Not yet:** option greeks,
scanners.

## Building

```bash
dotnet build IBKR.Net.slnx
dotnet test                 # offline; never connects to TWS / IB Gateway
csharpier format .
```

## Contributing

Work happens on the current `advance/X.Y.Z` branch; `main` only receives releases, and a push to
`main` publishes the packages. See [CLAUDE.md](CLAUDE.md) for the rules — especially the
clean-room rule: **never copy or translate Interactive Brokers' source code**.

## License

[MIT](LICENSE). Not affiliated with or endorsed by Interactive Brokers. "Interactive Brokers",
"IBKR", "TWS" and "IB Gateway" are trademarks of their owners.
