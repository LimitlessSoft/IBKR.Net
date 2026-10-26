namespace IBKR.Net.Tests.Fixtures;

/// <summary>
/// News frames recorded from IB Gateway 10.50 at protocol version 187 (2026-10-26). Headlines are
/// public; the article text is shortened. No account data is in them. See docs/protocol.md, "News".
/// </summary>
public static class RecordedNews
{
	public static readonly string[] Providers =
	[
		"85",
		"3",
		"BRFG",
		"Briefing.com General Market Columns",
		"BRFUPDN",
		"Briefing.com Analyst Actions",
		"DJNL",
		"Dow Jones Newsletters",
	];

	public static readonly string[][] HistoricalNews =
	[
		[
			"86",
			"10",
			"2026-10-26 13:03:00.0",
			"DJ-N",
			"DJ-N$1fa3b9b6",
			"{A:800015:L:en}* Apple Unveils All-New iPad Mini >AAPL",
		],
		[
			"86",
			"10",
			"2026-10-26 13:03:00.0",
			"DJ-N",
			"DJ-N$1fa3b98e",
			"{A:800015:L:en}Press Release: Apple(R) gives iPad mini(R) its -3-",
		],
		["87", "10", "1"],
	];

	public static readonly string[] Article =
	[
		"83",
		"11",
		"0",
		"<html><body><p><strong>Apple (AAPL -8%)</strong> reported strong Q3 (Jun) results</p>&#10;</body></html>",
	];

	/// <summary>reqMktData for AAPL with news ticks 292 from five providers.</summary>
	public static readonly string[] ContractNewsRequest =
	[
		"1",
		"11",
		"30",
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
		"mdoff,292:BRFG+BRFUPDN+DJ-N+DJ-RT+DJNL",
		"0",
		"0",
		"",
	];

	public static readonly string[][] ContractNewsTicks =
	[
		[
			"84",
			"30",
			"1793019780000",
			"DJ-N",
			"DJ-N$1fa3b986",
			"Press Release: Apple(R) gives iPad mini(R) its biggest update ever",
			"A:800015:L:en",
		],
		[
			"84",
			"30",
			"1793019780000",
			"DJ-N",
			"DJ-N$1fa3b9b6",
			"* Apple Unveils All-New iPad Mini >AAPL",
			"A:800015:L:en",
		],
	];

	/// <summary>reqContractDetails for secType NEWS, exchange BRFUPDN (the provider's broad tape).</summary>
	public static readonly string[] BroadTapeDetails =
	[
		"10",
		"40",
		"BRFUPDN:BRF_ALL",
		"News",
		"",
		"",
		"0",
		"",
		"NEWS",
		"",
		"",
		"",
		"Global:Global",
		"99384238",
		"0.01",
		"",
		"",
		"NEWS",
		"1",
		"0",
		"All Briefing.com Events",
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
		"2147483647",
		"",
		"",
		"",
		"",
		"",
		"1",
		"1",
		"1",
		"0",
	];

	/// <summary>reqMktData for BRFG's broad tape: the provider as exchange, plain tick 292.</summary>
	public static readonly string[] BroadTapeRequest =
	[
		"1",
		"11",
		"20",
		"100283238",
		"BRFG:BRFG_ALL",
		"NEWS",
		"",
		"",
		"",
		"",
		"BRFG",
		"",
		"",
		"",
		"",
		"0",
		"mdoff,292",
		"0",
		"0",
		"",
	];

	public static readonly string[] BroadTapeTick =
	[
		"84",
		"20",
		"1793016071000",
		"BRFG",
		"BRFG$1fa39a77",
		"Futures point to higher open",
		"A:800015:L:en",
	];
}
