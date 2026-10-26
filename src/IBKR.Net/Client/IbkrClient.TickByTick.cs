using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	/// <summary>
	/// Every trade, quote or midpoint change of a contract as it happens (reqTickByTickData),
	/// until you stop reading. <paramref name="historicalTicks"/> &gt; 0 first replays recent
	/// ticks (<see cref="TickByTick.IsHistorical"/>; IB sends at least that many, often more: whole
	/// seconds). <paramref name="ignoreSize"/> skips BidAsk ticks where only a size changed.
	/// IB allows only a few tick-by-tick subscriptions at once (5 on the recorded account): more
	/// fail with error 10190; missing market data permissions, or a type the contract has no data
	/// for (Last on forex), fail with 10189 (or 354); an unknown contract with 200.
	/// </summary>
	public IAsyncEnumerable<TickByTick> SubscribeTickByTickAsync(
		Contract contract,
		TickByTickType tickType,
		int historicalTicks = 0,
		bool ignoreSize = false,
		CancellationToken cancellationToken = default
	) =>
		StreamManyAsync<TickByTick>(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestTickByTick)
					.Add(id)
					.AddContract(contract)
					.Add(tickType.ToString())
					.Add(historicalTicks)
					.Add(ignoreSize),
			id => new RequestBuilder(OutgoingMessageIds.CancelTickByTick).Add(id),
			fields =>
				fields[0] switch
				{
					IncomingMessageIds.TickByTick => [Decoders.TickByTick(fields)],
					IncomingMessageIds.HistoricalTicksLast => Decoders.HistoricalTicksLast(
						fields,
						tickType
					),
					IncomingMessageIds.HistoricalTicksBidAsk => Decoders.HistoricalTicksBidAsk(
						fields
					),
					IncomingMessageIds.HistoricalTicksMidPoint => Decoders.HistoricalTicksMidPoint(
						fields
					),
					_ => null,
				},
			cancellationToken
		);
}
