namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Wall Street Horizon frames recorded from IB Gateway at protocol version 187 (2026-10-26),
/// read-only, on an account without the WSH subscription: every request was answered with 10276.
/// No account data is in them. Request ids are replaced per test. See docs/protocol.md, "Wall
/// Street Horizon".
/// </summary>
public static class RecordedWsh
{
	public static readonly string[] MetaDataRequest = ["100", "4"];

	public static readonly string[] MetaDataCancel = ["101", "4"];

	/// <summary>One contract (AAPL) between two dates, at most 50 events.</summary>
	public static readonly string[] EventDataByConIdRequest =
	[
		"102",
		"5",
		"265598",
		"",
		"0",
		"0",
		"0",
		"20261001",
		"20261231",
		"50",
	];

	/// <summary>A market-wide filter, no contract (unset = 2147483647), at most 100 events.</summary>
	public static readonly string[] EventDataByFilterRequest =
	[
		"102",
		"6",
		"2147483647",
		"{\"country\":\"All\",\"limit_region\":10,\"limit\":100,\"wshe_ed\":\"true\"}",
		"0",
		"0",
		"0",
		"20261026",
		"20261102",
		"100",
	];

	/// <summary>A filter without dates or limit: both unset.</summary>
	public static readonly string[] EventDataUnsetRequest =
	[
		"102",
		"8",
		"2147483647",
		"{\"country\":\"All\",\"watchlist\":[\"265598\"],\"limit_region\":10,\"limit\":10,\"wshe_ed\":\"true\",\"wshe_bod\":\"true\"}",
		"0",
		"0",
		"0",
		"",
		"",
		"2147483647",
	];

	public static readonly string[] EventDataCancel = ["103", "5"];

	/// <summary>The answer to every WSH request without the subscription (request id 4).</summary>
	public static readonly string[] NotAllowed =
	[
		"4",
		"2",
		"4",
		"10276",
		"News feed is not allowed.",
		"",
	];
}
