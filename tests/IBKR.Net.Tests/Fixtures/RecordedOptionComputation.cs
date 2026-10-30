namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// Option market data recorded read-only from IB Gateway (live) at protocol version 187 on
/// 2026-10-30, US regular hours: the SPY 2026-10-30 755 put streamed with generic tick 101. Market
/// data only, no account data. Request ids are replaced per test. See docs/protocol.md, "Option
/// greeks and open interest".
/// </summary>
public static class RecordedOptionComputation
{
	public static readonly string[] Request =
	[
		"1",
		"11",
		"5",
		"0",
		"SPY",
		"OPT",
		"20261030",
		"755",
		"P",
		"100",
		"SMART",
		"",
		"USD",
		"",
		"SPY",
		"0",
		"101",
		"0",
		"0",
		"",
	];

	/// <summary>The first replies, in order (quote ticks between them left out).</summary>
	public static readonly string[][] Replies =
	[
		["58", "1", "5", "1"],
		["81", "5", "0.01", "c70003", "3"],
		["1", "6", "5", "1", "1.30", "158", "1"],
		["2", "6", "5", "27", "0"],
		["2", "6", "5", "28", "41057"],
		[
			"21",
			"5",
			"13",
			"0",
			"0.25652007640268776",
			"-0.46693286118129407",
			"1.2843018132440893",
			"0.0",
			"0.11197500273737811",
			"0.05522320195516195",
			"-1.2843018132440893",
			"755.3499145507812",
		],
		// The 755 call's first bid computation (another request in the recording): only the
		// price and the underlying are set
		[
			"21",
			"5",
			"10",
			"0",
			"-1",
			"-2",
			"1.5700000524520874",
			"0.0",
			"-2",
			"-2",
			"-2",
			"755.28",
		],
	];
}
