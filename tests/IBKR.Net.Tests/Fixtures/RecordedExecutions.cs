namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Execution and commission frames recorded 2026-10-26 from IB Gateway (live, a SPY 0DTE put
/// bought and sold from TWS), answering reqExecutions (7) without filters; identical at versions
/// 178 and 187. Scrubbed: fake account, exec ids and perm ids. See docs/protocol.md, "Executions".
/// </summary>
public static class RecordedExecutions
{
	public static readonly string[] Request = ["7", "3", "9001", "0", "", "", "", "", "", ""];

	/// <summary>Execution (11): BOT 1 SPY 20261026 768 P at 4.61 on CBOE, a TWS order.</summary>
	public static readonly string[] Buy =
	[
		"11",
		"9001",
		"0",
		"934002900",
		"SPY",
		"OPT",
		"20261026",
		"768.0",
		"P",
		"100",
		"CBOE",
		"USD",
		"SPY   261026P00768000",
		"SPY",
		"0000e001.00000001.01.01",
		"20261026 10:00:59 US/Eastern",
		"DU0000001",
		"CBOE",
		"BOT",
		"1",
		"4.61",
		"900000001",
		"0",
		"0",
		"1",
		"4.61",
		"OptionChain",
		"",
		"",
		"",
		"2",
		"0",
	];

	/// <summary>Execution (11): SLD 1 of the same put at 4.78 on ISE.</summary>
	public static readonly string[] Sell =
	[
		"11",
		"9001",
		"0",
		"934002900",
		"SPY",
		"OPT",
		"20261026",
		"768.0",
		"P",
		"100",
		"ISE",
		"USD",
		"SPY   261026P00768000",
		"SPY",
		"0000e002.00000002.01.01",
		"20261026 10:04:31 US/Eastern",
		"DU0000001",
		"ISE",
		"SLD",
		"1",
		"4.78",
		"900000002",
		"0",
		"0",
		"1",
		"4.78",
		"",
		"",
		"",
		"",
		"1",
		"0",
	];

	/// <summary>Commission report (59) of the buy: opening fill, realized P&amp;L unset.</summary>
	public static readonly string[] BuyCommission =
	[
		"59",
		"1",
		"0000e001.00000001.01.01",
		"1.04028",
		"USD",
		"1.7976931348623157E308",
		"1.7976931348623157E308",
		"",
	];

	/// <summary>Commission report (59) of the sell, with the round trip's realized P&amp;L.</summary>
	public static readonly string[] SellCommission =
	[
		"59",
		"1",
		"0000e002.00000002.01.01",
		"1.043147",
		"USD",
		"14.916573",
		"1.7976931348623157E308",
		"",
	];

	public static readonly string[] End = ["55", "1", "9001"];
}
