namespace IBKR.Net.Protocol;

/// <summary>First field of a server frame; each id was seen on the wire (docs/protocol.md).</summary>
internal static class IncomingMessageIds
{
	public const string TickPrice = "1";
	public const string TickSize = "2";
	public const string OrderStatus = "3";
	public const string Error = "4";
	public const string OpenOrder = "5";
	public const string NextValidId = "9";
	public const string ContractData = "10";
	public const string ExecutionData = "11";
	public const string MarketDepth = "12";
	public const string MarketDepthL2 = "13";
	public const string ManagedAccounts = "15";
	public const string HistoricalData = "17";
	public const string TickGeneric = "45";
	public const string TickString = "46";
	public const string RealTimeBar = "50";
	public const string CurrentTime = "49";
	public const string ContractDataEnd = "52";
	public const string OpenOrderEnd = "53";
	public const string ExecutionDataEnd = "55";
	public const string MarketDataType = "58";
	public const string CommissionReport = "59";
	public const string Position = "61";
	public const string PositionEnd = "62";
	public const string AccountSummary = "63";
	public const string AccountSummaryEnd = "64";
	public const string SecDefOptParams = "75";
	public const string SecDefOptParamsEnd = "76";
	public const string MarketDepthExchanges = "80";
	public const string TickReqParams = "81";
	public const string NewsArticle = "83";
	public const string TickNews = "84";
	public const string NewsProviders = "85";
	public const string HistoricalNews = "86";
	public const string HistoricalNewsEnd = "87";
	public const string Pnl = "94";
	public const string PnlSingle = "95";

	/// <summary>
	/// Where a message carries its request id, for messages tied to a request; null for the
	/// others (current time, positions, managed accounts …).
	/// </summary>
	public static int? RequestIdIndex(string messageId) =>
		messageId switch
		{
			ContractData or SecDefOptParams or SecDefOptParamsEnd or TickReqParams => 1,
			HistoricalData or Pnl or PnlSingle or ExecutionData => 1,
			NewsArticle or TickNews or HistoricalNews or HistoricalNewsEnd => 1,
			OrderStatus => 1, // the order id
			ExecutionDataEnd => 2,
			Error or ContractDataEnd or MarketDataType or AccountSummary or AccountSummaryEnd => 2,
			TickPrice or TickSize or TickGeneric or TickString or RealTimeBar => 2,
			MarketDepth or MarketDepthL2 => 2,
			_ => null,
		};
}
