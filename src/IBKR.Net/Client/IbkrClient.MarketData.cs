using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	/// <summary>Which data later market data requests get (reqMarketDataType).</summary>
	public Task SetMarketDataTypeAsync(
		MarketDataType type,
		CancellationToken cancellationToken = default
	) =>
		_connection.SendAsync(
			new RequestBuilder(OutgoingMessageIds.RequestMarketDataType)
				.Add(1)
				.Add((int)type)
				.Build(),
			cancellationToken
		);

	/// <summary>
	/// Streaming quotes for a contract (reqMktData); uses one of the account's market data lines
	/// until you stop reading.
	/// </summary>
	public IAsyncEnumerable<MarketDataTick> SubscribeMarketDataAsync(
		Contract contract,
		string genericTicks = "",
		CancellationToken cancellationToken = default
	) =>
		StreamAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestMarketData)
					.Add(11)
					.Add(id)
					.AddContract(contract)
					.Add(false) // delta-neutral combo
					.Add(genericTicks)
					.Add(false) // snapshot
					.Add(false) // regulatory snapshot
					.Add(string.Empty), // options
			id => new RequestBuilder(OutgoingMessageIds.CancelMarketData).Add(1).Add(id),
			fields =>
				fields[0] switch
				{
					IncomingMessageIds.TickPrice => Decoders.TickPrice(fields),
					IncomingMessageIds.TickSize => Decoders.TickSize(fields),
					IncomingMessageIds.TickGeneric => Decoders.TickGeneric(fields),
					_ => null,
				},
			cancellationToken
		);

	/// <summary>
	/// Historical bars (reqHistoricalData) ending now, or at <paramref name="endDateTime"/>
	/// ("yyyyMMdd-HH:mm:ss" UTC). Durations and bar sizes use IB's strings: "5 D", "5 mins".
	/// </summary>
	public async Task<IReadOnlyList<Bar>> GetHistoricalBarsAsync(
		Contract contract,
		string duration,
		string barSize,
		string whatToShow = "TRADES",
		bool useRegularTradingHours = true,
		string endDateTime = "",
		CancellationToken cancellationToken = default
	) =>
		await CollectAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestHistoricalData)
					.Add(id)
					.AddContract(contract)
					.Add(false) // include expired
					.Add(endDateTime)
					.Add(barSize)
					.Add(duration)
					.Add(useRegularTradingHours)
					.Add(whatToShow)
					.Add(2) // date format: unix seconds
					.Add(false) // keep up to date
					.Add(string.Empty), // chart options
			fields =>
				fields[0] == IncomingMessageIds.HistoricalData
					? Decoders.HistoricalData(fields)
					: null,
			IncomingMessageIds.HistoricalData,
			cancellationToken
		);
}
