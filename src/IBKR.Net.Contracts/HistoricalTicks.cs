namespace IBKR.Net;

/// <summary>Which past ticks <c>IbkrClient.GetHistoricalTicksAsync</c> asks for (reqHistoricalTicks).</summary>
public enum HistoricalTickType
{
	/// <summary>
	/// Every trade, odd lots included (flagged <see cref="TickByTick.Unreported"/>), as
	/// <see cref="TickByTickType.AllLast"/> ticks.
	/// </summary>
	Trades = 1,

	/// <summary>Best bid and ask, as <see cref="TickByTickType.BidAsk"/> ticks.</summary>
	BidAsk = 2,

	/// <summary>The bid / ask midpoint, as <see cref="TickByTickType.MidPoint"/> ticks.</summary>
	MidPoint = 3,
}
