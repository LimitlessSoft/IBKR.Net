namespace IBKR.Net;

/// <summary>A position in one account (reqPositions).</summary>
public sealed record Position(
	string Account,
	Contract Contract,
	decimal Quantity,
	double AverageCost
);

/// <summary>One account summary value, e.g. NetLiquidation (reqAccountSummary).</summary>
public sealed record AccountValue(string Account, string Tag, string Value, string Currency);

/// <summary>Account P&amp;L (reqPnL); null until IB has a value.</summary>
public sealed record AccountPnl(double? DailyPnl, double? UnrealizedPnl, double? RealizedPnl);

/// <summary>P&amp;L of one position (reqPnLSingle); null until IB has a value.</summary>
public sealed record PositionPnl(
	decimal Position,
	double? DailyPnl,
	double? UnrealizedPnl,
	double? RealizedPnl,
	double? MarketValue
);
