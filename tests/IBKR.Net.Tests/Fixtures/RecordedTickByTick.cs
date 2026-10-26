namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Tick-by-tick frames recorded from IB Gateway at protocol versions 178 and 187 (2026-10-26,
/// US regular hours), read-only; identical at both. Trades and quotes are public; no account data
/// is in them. Request ids are replaced per test. See docs/protocol.md, "Tick-by-tick".
/// </summary>
public static class RecordedTickByTick
{
	/// <summary>AAPL trades ("Last"), no history, sizes kept; request id 101.</summary>
	public static readonly string[] LastRequest =
	[
		"97",
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
		"Last",
		"0",
		"0",
	];

	public static readonly string[] Cancel = ["98", "101"];

	/// <summary>Live trades: no conditions, then a trade with condition <c>F</c>.</summary>
	public static readonly string[][] Last =
	[
		["99", "101", "1", "1793043156", "343.85", "58", "0", "NYSE", ""],
		["99", "101", "1", "1793043157", "343.83", "40", "0", "NASDAQ", "F"],
	];

	/// <summary>AllLast: an odd lot (attribute bit 1, condition <c>I</c>), and BTC on PAXOS
	/// (no exchange, no conditions).</summary>
	public static readonly string[][] AllLast =
	[
		["99", "101", "2", "1793043161", "343.87", "1", "2", "FINRA", "I"],
		["99", "101", "2", "1793043187", "79853.50", "1", "0", "", ""],
	];

	/// <summary>BidAsk on AAPL, and on BTC with fractional sizes.</summary>
	public static readonly string[][] BidAsk =
	[
		["99", "101", "3", "1793043165", "343.84", "343.87", "240", "280", "0"],
		["99", "101", "3", "1793043192", "79866.00", "79867.50", "0.10016748", "0.11602491", "0"],
	];

	public static readonly string[] MidPoint = ["99", "101", "4", "1793043169", "343.88"];

	/// <summary>
	/// History before AllLast trades (history requested: 3): the first 3 of the 48 ticks the frame
	/// carried, count changed to match; conditions come space-padded to 4 characters.
	/// </summary>
	public static readonly string[] HistoricalLast =
	[
		"98",
		"101",
		"3",
		"1793043312",
		"0",
		"344.27736",
		"60",
		"FINRA",
		"",
		"1793043312",
		"2",
		"344.186",
		"1",
		"FINRA",
		" 4 I",
		"1793043312",
		"2",
		"344.29",
		"10",
		"MEMX",
		" F I",
		"1",
	];

	/// <summary>History before BidAsk: the first 2 of 11 ticks.</summary>
	public static readonly string[] HistoricalBidAsk =
	[
		"97",
		"101",
		"2",
		"1793043306",
		"0",
		"344.32",
		"344.36",
		"80",
		"200",
		"1793043307",
		"0",
		"344.32",
		"344.36",
		"120",
		"200",
		"1",
	];

	/// <summary>History before BTC midpoints: all 3 ticks.</summary>
	public static readonly string[] HistoricalMidPoint =
	[
		"96",
		"101",
		"3",
		"1793043315",
		"0",
		"79869.625",
		"0",
		"1793043316",
		"0",
		"79868.375",
		"0",
		"1793043316",
		"0",
		"79866.00",
		"0",
		"1",
	];

	/// <summary>The 6th concurrent subscription.</summary>
	public static readonly string[] MaxRequestsError =
	[
		"4",
		"2",
		"101",
		"10190",
		"Max number of tick-by-tick requests has been reached.",
		"",
	];

	/// <summary>VOW on IBIS without the market data subscription.</summary>
	public static readonly string[] NoPermissionError =
	[
		"4",
		"2",
		"101",
		"10189",
		"Failed to request tick-by-tick data.No market data permissions for IBIS STK",
		"",
	];

	/// <summary>"Last" on a forex pair.</summary>
	public static readonly string[] NoDataError =
	[
		"4",
		"2",
		"101",
		"10189",
		"Failed to request tick-by-tick data.No historical market data for EUR/CASH@FXSUBPIP Last 0",
		"",
	];
}
