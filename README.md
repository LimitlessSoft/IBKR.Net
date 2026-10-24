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
var serverTime = await client.GetServerTimeAsync();
```

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
