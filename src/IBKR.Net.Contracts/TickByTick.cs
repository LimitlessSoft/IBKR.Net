namespace IBKR.Net;

/// <summary>Which ticks a tick-by-tick subscription streams (reqTickByTickData).</summary>
public enum TickByTickType
{
	/// <summary>Trades, without the ones IB leaves out of its "last" (odd lots, unreported).</summary>
	Last = 1,

	/// <summary>Every trade, odd lots and trades reported away from the exchanges included.</summary>
	AllLast = 2,

	/// <summary>Best bid and ask, each time either price or size changes.</summary>
	BidAsk = 3,

	/// <summary>The midpoint of best bid and ask, each time it changes.</summary>
	MidPoint = 4,
}

/// <summary>
/// One tick of a tick-by-tick subscription (reqTickByTickData). <see cref="Time"/> is whole
/// seconds: several ticks often share one.
/// <list type="bullet">
/// <item><see cref="TickByTickType.Last"/> / <see cref="TickByTickType.AllLast"/>: a trade —
/// <see cref="Price"/>, <see cref="Size"/>, <see cref="Exchange"/> (empty for crypto),
/// <see cref="SpecialConditions"/> (IB's sale condition codes, space separated: <c>I</c> odd lot,
/// <c>F</c>, <c>4 W</c> …; empty when none), <see cref="PastLimit"/>, <see cref="Unreported"/>.</item>
/// <item><see cref="TickByTickType.BidAsk"/>: <see cref="BidPrice"/>, <see cref="AskPrice"/>,
/// <see cref="BidSize"/>, <see cref="AskSize"/>, <see cref="BidPastLow"/>,
/// <see cref="AskPastHigh"/>.</item>
/// <item><see cref="TickByTickType.MidPoint"/>: <see cref="Price"/> is the midpoint.</item>
/// </list>
/// <see cref="IsHistorical"/> marks the ticks replayed before the live ones when the subscription
/// asked for history.
/// </summary>
public sealed record TickByTick(TickByTickType Type, DateTimeOffset Time)
{
	public double Price { get; init; }
	public decimal Size { get; init; }
	public string Exchange { get; init; } = "";
	public string SpecialConditions { get; init; } = "";

	/// <summary>Trade attribute: the price is outside the day's limit (attribute bit 0).</summary>
	public bool PastLimit { get; init; }

	/// <summary>Trade attribute: an unreported trade (attribute bit 1; seen on odd lots).</summary>
	public bool Unreported { get; init; }

	public double BidPrice { get; init; }
	public double AskPrice { get; init; }
	public decimal BidSize { get; init; }
	public decimal AskSize { get; init; }

	/// <summary>Quote attribute: the bid is below the day's low (attribute bit 0).</summary>
	public bool BidPastLow { get; init; }

	/// <summary>Quote attribute: the ask is above the day's high (attribute bit 1).</summary>
	public bool AskPastHigh { get; init; }

	public bool IsHistorical { get; init; }
}
