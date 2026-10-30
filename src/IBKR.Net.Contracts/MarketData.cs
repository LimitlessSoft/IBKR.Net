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
/// <see cref="Value"/>, an option's computed greeks <see cref="Option"/> (types 10 bid, 11 ask,
/// 12 last, 13 model). Option open interest comes as size ticks 27 (call) / 28 (put) when the
/// generic tick list holds 101.
/// </summary>
public sealed record MarketDataTick(
	int Field,
	double? Price = null,
	decimal? Size = null,
	double? Value = null,
	string? StringValue = null,
	OptionComputation? Option = null
);

/// <summary>
/// IB's computation for an option (tickOptionComputation), from one of its prices: implied
/// volatility, greeks and the underlying price it used. Values IB didn't compute are null (on the
/// wire -1 for volatility and prices, -2 for the greeks). Gamma is per 1.00 move of the
/// underlying, vega and theta per option share (one vol point, one day).
/// </summary>
public sealed record OptionComputation(
	double? ImpliedVolatility,
	double? Delta,
	double? OptionPrice,
	double? PresentValueDividend,
	double? Gamma,
	double? Vega,
	double? Theta,
	double? UnderlyingPrice
);

/// <summary>
/// A price bar; <see cref="Volume"/>, <see cref="Wap"/> and <see cref="Count"/> are -1 when IB has
/// none (e.g. midpoint bars).
/// </summary>
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
