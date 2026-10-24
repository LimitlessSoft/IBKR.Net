namespace IBKR.Net;

/// <summary>
/// An instrument. Identify one by <see cref="ConId"/> (with <see cref="Exchange"/>), or describe it
/// by symbol, type, exchange and currency to search for it.
/// </summary>
public sealed record Contract
{
	/// <summary>IB's contract id; 0 when unknown.</summary>
	public int ConId { get; init; }
	public string Symbol { get; init; } = string.Empty;

	/// <summary>STK, OPT, FUT, IND, CASH, CRYPTO …</summary>
	public string SecurityType { get; init; } = string.Empty;

	/// <summary>yyyyMMdd (a day) or yyyyMM (a month), for derivatives.</summary>
	public string LastTradeDateOrContractMonth { get; init; } = string.Empty;

	/// <summary>Option strike; null = any strike (IB reads 0 as "strike 0").</summary>
	public double? Strike { get; init; }

	/// <summary>C or P, for options.</summary>
	public string Right { get; init; } = string.Empty;
	public string Multiplier { get; init; } = string.Empty;

	/// <summary>Routing exchange, usually SMART.</summary>
	public string Exchange { get; init; } = string.Empty;

	/// <summary>Listing exchange (ARCA, NASDAQ …), for stocks routed via SMART.</summary>
	public string PrimaryExchange { get; init; } = string.Empty;
	public string Currency { get; init; } = string.Empty;
	public string LocalSymbol { get; init; } = string.Empty;
	public string TradingClass { get; init; } = string.Empty;
}
