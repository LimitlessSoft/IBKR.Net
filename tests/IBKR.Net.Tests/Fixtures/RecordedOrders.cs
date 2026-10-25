namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Order frames at protocol version 178 (2026-10-25), scrubbed (fake account and order ids).
/// placeOrder / cancelOrder were sent by an independent client (ib_async) to IB Gateway and to a
/// local stand-in that only did the handshake; the replies are IB Gateway's. See docs/protocol.md.
/// </summary>
public static class RecordedOrders
{
	/// <summary>BUY 0.0002 BTC.USD on PAXOS, limit 20000, GTC (order id 200).</summary>
	public static readonly string[] PlaceLimitGtc =
	[
		"3",
		"200",
		"479624278",
		"BTC",
		"CRYPTO",
		"",
		"0.0",
		"",
		"",
		"PAXOS",
		"",
		"USD",
		"BTC.USD",
		"BTC",
		"",
		"",
		"BUY",
		"0.0002",
		"LMT",
		"20000",
		"",
		"GTC",
		"",
		"DU0000001",
		"O",
		"0",
		"",
		"1",
		"0",
		"0",
		"0",
		"0",
		"0",
		"0",
		"0",
		"",
		"0.0",
		"",
		"",
		"",
		"",
		"",
		"",
		"0",
		"",
		"-1",
		"0",
		"",
		"",
		"0",
		"",
		"",
		"0",
		"0",
		"",
		"0",
		"",
		"",
		"",
		"",
		"",
		"0",
		"",
		"",
		"",
		"",
		"0",
		"",
		"",
		"",
		"",
		"",
		"",
		"",
		"",
		"",
		"",
		"0",
		"",
		"",
		"0",
		"0",
		"",
		"",
		"0",
		"",
		"0",
		"0",
		"0",
		"0",
		"",
		"",
		"",
		"",
		"",
		"",
		"0",
		"",
		"",
		"",
		"",
		"",
		"",
		"",
		"",
		"0",
		"0",
		"0",
		"0",
		"",
		"",
		"0",
		"",
		"",
	];

	/// <summary>The same order as a $10 market buy, IOC (order id 201): differs in [17..21], [100].</summary>
	public static readonly string[] PlaceMarketCashIoc = With(
		PlaceLimitGtc,
		(1, "201"),
		(17, "0"),
		(18, "MKT"),
		(19, ""),
		(21, "IOC"),
		(100, "10")
	);

	/// <summary>Variants of <see cref="PlaceLimitGtc"/> that located single fields.</summary>
	public static readonly string[] PlaceSell = With(PlaceLimitGtc, (1, "202"), (16, "SELL"));

	public static readonly string[] PlaceOutsideRth = With(PlaceLimitGtc, (1, "203"), (33, "1"));

	public static readonly string[] PlaceStopDayNoAccount = With(
		PlaceLimitGtc,
		(1, "205"),
		(17, "3"),
		(18, "STP"),
		(19, ""),
		(20, "2.5"),
		(21, "DAY"),
		(23, "")
	);

	public static readonly string[] CancelOrder = ["4", "1", "200", ""];

	/// <summary>IB Gateway's reply to a cancel sent before the order was accepted.</summary>
	public static readonly string[] OrderStatusApiCancelled =
	[
		"3",
		"10",
		"ApiCancelled",
		"0",
		"0",
		"",
		"0",
		"0",
		"",
		"91",
		"",
		"",
	];

	/// <summary>IB Gateway's rejection of an order on an account without crypto permissions.</summary>
	public static readonly string[] NoTradingPermissions =
	[
		"4",
		"2",
		"25",
		"460",
		"No trading permissions.",
		"",
	];

	private static string[] With(string[] frame, params (int Index, string Value)[] changes)
	{
		var copy = (string[])frame.Clone();
		foreach (var (index, value) in changes)
			copy[index] = value;
		return copy;
	}
}
