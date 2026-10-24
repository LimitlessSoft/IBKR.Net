namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Frames recorded from IB Gateway 10.50 at protocol version 187 (2026-10-24), scrubbed: account
/// ids, positions and balances are fake; public market data (SPY's conId, prices) is as recorded;
/// long comma lists are shortened. See docs/protocol.md.
/// </summary>
public static class Recorded
{
	public static readonly string[] ContractDataStock =
	[
		"10", "2", "SPY", "STK", "", "", "0", "", "SMART", "USD", "SPY", "SPY", "SPY", "756733",
		"0.01", "", "LMT,MKT,STP", "SMART,AMEX,ARCA", "1", "0", "SS SPDR S&P 500 ETF TRUST-US",
		"ARCA", "", "", "", "", "US/Eastern",
		"20261024:CLOSED;20261025:CLOSED;20261026:0400-20261026:2000",
		"20261024:CLOSED;20261025:CLOSED;20261026:0930-20261026:1600", "", "", "1", "ISIN",
		"US78462F1030", "1", "", "", "4563,4563,4563", "", "ETF", "0.0001", "0.0001", "40", "0",
	];

	public static readonly string[] ContractDataOption =
	[
		"10", "5", "SPY", "OPT", "20261026 16:15:00 US/Eastern", "20261026", "705", "C", "SMART",
		"USD", "SPY   261026C00705000", "SPY", "SPY", "933461775", "0.01", "100", "LMT,MKT",
		"SMART,AMEX,CBOE", "1", "756733", "SS SPDR S&P 500 ETF TRUST-US", "", "202610", "", "", "",
		"US/Eastern", "20261024:CLOSED;20261026:0730-20261026:0925;20261026:0930-20261026:1615",
		"20261024:CLOSED;20261026:0930-20261026:1615", "", "", "0", "2", "SPY", "STK", "32,32,32",
		"20261026", "", "1", "1", "1", "0",
	];

	public static readonly string[] SecDefOptParams =
	[
		"75", "4", "SMART", "756733", "SPY", "100", "3", "20261027", "20261026", "20261028", "4",
		"700.0", "705.0", "690.5", "710.0",
	];

	public static readonly string[] HistoricalData =
	[
		"17", "7", "20261023 17:51:23 US/Eastern", "20261024 17:51:23 US/Eastern", "2",
		"1792762200", "759.9000", "760.7300", "759.7200", "759.9900", "998384", "760.033", "5488",
		"1792762500", "759.9900", "760.0000", "759.1400", "759.2700", "433038", "759.532", "4576",
	];

	public static readonly string[][] MarketData =
	[
		["81", "6", "0.01", "a60001", "0"],
		["58", "1", "6", "1"],
		["1", "6", "6", "1", "-1.00", "0", "1"],
		["1", "6", "6", "9", "763.960022", "0", "0"],
		["2", "6", "6", "8", "0"],
		["45", "6", "6", "49", "0.0"],
	];

	/// <summary>BTC on PAXOS, recorded 2026-10-24 (crypto trades on weekends).</summary>
	public static readonly string[] TickStringLastTime = ["46", "6", "3", "45", "1792879706"];

	public static readonly string[] RealTimeBarTrades =
	[
		"50", "3", "5", "1792879760", "80364.5", "80364.5", "80364.5", "80364.5", "6.200000000E-7",
		"80364.5000000000000000", "1",
	];

	public static readonly string[] RealTimeBarMidpoint =
	[
		"50", "3", "4", "1792879725", "80355.0", "80355.0", "80354.5", "80354.625", "", "", "-1",
	];

	public static readonly string[] Position =
	[
		"61", "3", "DU0000001", "920268479", "SPY", "OPT", "20261023", "759.0", "C", "100", "",
		"USD", "SPY   261023C00759000", "SPY", "2", "150.5",
	];

	public static readonly string[] AccountSummary =
	[
		"63", "1", "10", "DU0000001", "NetLiquidation", "100000.00", "USD",
	];

	public static readonly string[] Pnl = ["94", "11", "8.5936", "0.0", "35.3089492"];

	public static readonly string[] PnlSingle =
	[
		"95", "12", "0", "6.8042", "1.7976931348623157E308", "6.8042216", "1.7976931348623157E308",
	];

	public static readonly string[] NoSecurityDefinition =
	[
		"4", "2", "3", "200", "No security definition has been found for the request", "",
	];
}
