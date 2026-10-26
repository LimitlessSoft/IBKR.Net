namespace IBKR.Net.Protocol;

/// <summary>First field of a request frame; each id was seen on the wire (docs/protocol.md).</summary>
internal static class OutgoingMessageIds
{
	public const string RequestMarketData = "1";
	public const string CancelMarketData = "2";
	public const string PlaceOrder = "3";
	public const string CancelOrder = "4";
	public const string RequestExecutions = "7";
	public const string RequestContractDetails = "9";
	public const string RequestMarketDepth = "10";
	public const string CancelMarketDepth = "11";
	public const string RequestAllOpenOrders = "16";
	public const string RequestHistoricalData = "20";
	public const string RequestCurrentTime = "49";
	public const string RequestRealTimeBars = "50";
	public const string CancelRealTimeBars = "51";
	public const string RequestMarketDataType = "59";
	public const string RequestPositions = "61";
	public const string RequestAccountSummary = "62";
	public const string CancelAccountSummary = "63";
	public const string CancelPositions = "64";
	public const string StartApi = "71";
	public const string RequestSecDefOptParams = "78";
	public const string RequestMarketDepthExchanges = "82";
	public const string RequestNewsArticle = "84";
	public const string RequestNewsProviders = "85";
	public const string RequestHistoricalNews = "86";
	public const string RequestPnl = "92";
	public const string CancelPnl = "93";
	public const string RequestPnlSingle = "94";
	public const string CancelPnlSingle = "95";
	public const string RequestTickByTick = "97";
	public const string CancelTickByTick = "98";
}
