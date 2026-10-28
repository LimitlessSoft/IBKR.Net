namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Historical ticks frames recorded from IB Gateway at protocol versions 178 and 187 (2026-10-28,
/// US regular hours), read-only; identical at both. Trades and quotes are public; no account data
/// is in them. Request ids are replaced per test; answers are cut to their first ticks with the
/// count changed to match. See docs/protocol.md, "Historical ticks".
/// </summary>
public static class RecordedHistoricalTicks
{
	/// <summary>AAPL trades before 2026-10-28 14:54:44 UTC, 5 asked; request id 4.</summary>
	public static readonly string[] TradesRequest =
	[
		"96",
		"4",
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
		"0",
		"",
		"20261028 14:54:44 UTC",
		"5",
		"TRADES",
		"0",
		"0",
		"",
	];

	/// <summary>A round lot, then an odd lot (attribute 2, condition padded to <c>"   I"</c>).</summary>
	public static readonly string[] Trades =
	[
		"98",
		"4",
		"2",
		"1793199224",
		"0",
		"345.51",
		"40",
		"FINRA",
		"",
		"1793199224",
		"2",
		"345.51",
		"1",
		"FINRA",
		"   I",
		"1",
	];

	public static readonly string[] BidAsk =
	[
		"97",
		"4",
		"2",
		"1793199277",
		"0",
		"345.25",
		"345.29",
		"40",
		"400",
		"1793199278",
		"0",
		"345.25",
		"345.29",
		"120",
		"400",
		"1",
	];

	public static readonly string[] MidPoint =
	[
		"96",
		"4",
		"2",
		"1793199277",
		"0",
		"345.27",
		"0",
		"1793199278",
		"0",
		"345.265",
		"0",
		"1",
	];

	/// <summary>1001 ticks asked.</summary>
	public static readonly string[] MaxLengthError =
	[
		"4",
		"2",
		"4",
		"10187",
		"Failed to request historical ticks:Max length exceeded",
		"",
	];
}
