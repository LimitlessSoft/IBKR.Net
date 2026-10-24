namespace IBKR.Net;

/// <summary>Which market data IB serves (reqMarketDataType).</summary>
public enum MarketDataType
{
	Live = 1,
	Frozen = 2,
	Delayed = 3,
	DelayedFrozen = 4,
}

/// <summary>
/// One market data update. <see cref="Field"/> is IB's tick type (1 bid, 2 ask, 4 last, 9 close …);
/// price ticks set <see cref="Price"/> (-1 = no price), size ticks <see cref="Size"/>, generic ticks
/// <see cref="Value"/>.
/// </summary>
public sealed record MarketDataTick(
	int Field,
	double? Price = null,
	decimal? Size = null,
	double? Value = null,
	string? StringValue = null
);

/// <summary>A price bar; <see cref="Wap"/> and <see cref="Count"/> are -1 when IB has none.</summary>
public sealed record Bar(
	DateTimeOffset Time,
	double Open,
	double High,
	double Low,
	double Close,
	decimal Volume,
	decimal Wap,
	int Count
);
