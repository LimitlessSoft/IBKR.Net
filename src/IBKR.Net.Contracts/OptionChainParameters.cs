namespace IBKR.Net;

/// <summary>
/// Expirations and strikes of an underlying's options on one exchange and trading class
/// (reqSecDefOptParams). <see cref="Strikes"/> is the union over all expirations.
/// </summary>
public sealed record OptionChainParameters(
	string Exchange,
	int UnderlyingConId,
	string TradingClass,
	string Multiplier,
	IReadOnlyList<string> Expirations,
	IReadOnlyList<double> Strikes
);
