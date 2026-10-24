namespace IBKR.Net;

/// <summary>A resolved contract with its trading details (reqContractDetails).</summary>
public sealed record ContractDetails
{
	public required Contract Contract { get; init; }
	public string MarketName { get; init; } = string.Empty;
	public double MinTick { get; init; }

	/// <summary>Comma-separated exchanges the contract can be routed to.</summary>
	public string ValidExchanges { get; init; } = string.Empty;
	public int UnderlyingConId { get; init; }
	public string LongName { get; init; } = string.Empty;
	public string ContractMonth { get; init; } = string.Empty;
	public string Industry { get; init; } = string.Empty;
	public string Category { get; init; } = string.Empty;
	public string Subcategory { get; init; } = string.Empty;
	public string TimeZoneId { get; init; } = string.Empty;

	/// <summary>e.g. "20261026:0400-20261026:2000;20261027:CLOSED".</summary>
	public string TradingHours { get; init; } = string.Empty;

	/// <summary>Regular hours, same format as <see cref="TradingHours"/>.</summary>
	public string LiquidHours { get; init; } = string.Empty;

	/// <summary>Other identifiers by type, e.g. ISIN.</summary>
	public IReadOnlyDictionary<string, string> SecurityIds { get; init; } =
		new Dictionary<string, string>();
}
