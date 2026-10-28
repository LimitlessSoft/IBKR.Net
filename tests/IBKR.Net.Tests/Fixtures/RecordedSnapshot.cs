namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Market data snapshot frames recorded read-only from IB Gateway (live) at protocol version 187
/// on 2026-10-28, US pre-market: AAPL served live, the generic tick refusal and an unsubscribed
/// listing. Market data only, no account data. Request ids are replaced per test. See
/// docs/protocol.md, "Market data snapshot".
/// </summary>
public static class RecordedSnapshot
{
	/// <summary>
	/// AAPL by conId via SMART (NASDAQ), snapshot set, no generic ticks. The strike is IBKR.Net's
	/// unset value (empty); the recording's probe sent <c>0.0</c>, which IB read the same for a stock.
	/// </summary>
	public static readonly string[] Request =
	[
		"1",
		"11",
		"101",
		"265598",
		"AAPL",
		"STK",
		"",
		"",
		"",
		"",
		"SMART",
		"NASDAQ",
		"USD",
		"",
		"",
		"0",
		"",
		"1",
		"0",
		"",
	];

	/// <summary>
	/// The replies, in order: within 5 ms of the request all of them but the last; the end
	/// (<c>57</c>) came 11 s later.
	/// </summary>
	public static readonly string[][] Replies =
	[
		["81", "101", "0.01", "9c0001", "3"],
		["58", "1", "101", "1"],
		["1", "6", "101", "1", "345.35", "80", "1"],
		["1", "6", "101", "2", "345.47", "160", "1"],
		["1", "6", "101", "4", "345.32", "57", "0"],
		["2", "6", "101", "8", "174925.427226"],
		["1", "6", "101", "9", "347.100006", "0", "0"],
		["46", "6", "101", "32", "Q"],
		["46", "6", "101", "33", "PQZ"],
		["46", "6", "101", "84", "D"],
		["46", "6", "101", "45", "1793186836"],
		["45", "6", "101", "49", "0.0"],
		["57", "1", "101"],
	];

	/// <summary>A snapshot asking for generic tick 106, refused at once.</summary>
	public static readonly string[] GenericTicksRefused =
	[
		"4",
		"2",
		"107",
		"321",
		"Error validating request.-'bL' : cause - Snapshot market data subscription is not applicable to generic ticks",
		"",
	];

	/// <summary>VOW3 on IBIS without the subscription: 10090 then 354, no end follows.</summary>
	public static readonly string[][] NotSubscribed =
	[
		[
			"4",
			"2",
			"109",
			"10090",
			"Part of requested market data is not subscribed. Subscription-independent ticks are still active.VOW3 IBIS/TOP/BID_ASK",
			"",
		],
		[
			"4",
			"2",
			"109",
			"354",
			"Requested market data is not subscribed. Check API status by selecting the Account menu then under Management choose Market Data Subscription Manager and/or availability of delayed data.Error&IBIS/STK/Top",
			"",
		],
	];
}
