namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Market depth frames recorded from IB Gateway 10.50 at protocol version 187 (2026-10-26),
/// read-only. Quotes and IB's exchange list are public; no account data is in them. See
/// docs/protocol.md, "Market depth".
/// </summary>
public static class RecordedMarketDepth
{
	/// <summary>SMART depth for AAPL, 10 rows; request id 101.</summary>
	public static readonly string[] SmartRequest =
	[
		"10",
		"5",
		"101",
		"265598",
		"AAPL",
		"STK",
		"",
		"",
		"",
		"",
		"SMART",
		"",
		"USD",
		"",
		"",
		"10",
		"1",
		"",
	];

	public static readonly string[] SmartCancel = ["11", "1", "101", "1"];

	/// <summary>Level 2 updates of the SMART request: inserts on the bid side.</summary>
	public static readonly string[][] SmartUpdates =
	[
		["13", "1", "101", "0", "IEX", "0", "1", "343.84", "40", "1"],
		["13", "1", "101", "1", "IEX", "0", "1", "343.80", "40", "1"],
		["13", "1", "101", "2", "DRCTEDGE", "1", "0", "344.05", "80", "1"],
	];

	/// <summary>AAPL on IEX alone, 5 rows; request id 102.</summary>
	public static readonly string[] ExchangeRequest =
	[
		"10",
		"5",
		"102",
		"265598",
		"AAPL",
		"STK",
		"",
		"",
		"",
		"",
		"IEX",
		"",
		"USD",
		"",
		"",
		"5",
		"0",
		"",
	];

	public static readonly string[] ExchangeCancel = ["11", "1", "102", "0"];

	/// <summary>Updates of the IEX request: insert, update, and a delete (price and size 0).</summary>
	public static readonly string[][] ExchangeUpdates =
	[
		["12", "1", "102", "0", "0", "1", "343.84", "40"],
		["12", "1", "102", "1", "1", "1", "343.84", "42"],
		["12", "1", "102", "4", "2", "1", "0", "0"],
	];

	/// <summary>Sent with the first updates of a SMART request; informational.</summary>
	public static readonly string[] PermissionsNotice =
	[
		"4",
		"2",
		"101",
		"2152",
		"Exchanges - Depth: IEX; Top: BYX; PEARL; AMEX; Need additional market data permissions - Depth: BATS; NASDAQ; ARCA; BEX; NYSE; ",
		"",
	];

	/// <summary>A fourth request while three run.</summary>
	public static readonly string[] MaxRequestsError =
	[
		"4",
		"2",
		"203",
		"309",
		"Max number (3) of market depth requests has been reached",
		"",
	];

	/// <summary>VOW on IBIS without the exchange's depth subscription.</summary>
	public static readonly string[] NotSubscribedError =
	[
		"4",
		"2",
		"105",
		"354",
		"Requested market data is not subscribed. Check API status by selecting the Account menu then under Management choose Market Data Subscription Manager and/or availability of delayed data.VOW IBIS/DEEP",
		"",
	];

	/// <summary>Four of the 306 exchanges the reply listed.</summary>
	public static readonly string[] Exchanges =
	[
		"80",
		"4",
		"COMEX",
		"FOP",
		"",
		"Deep",
		"2147483647",
		"ISLAND",
		"STK",
		"",
		"Deep2",
		"2147483647",
		"SMART",
		"STK",
		"PINK",
		"AggDeep",
		"1",
		"IDEALPRO",
		"CASH",
		"",
		"Deep",
		"4",
	];
}
