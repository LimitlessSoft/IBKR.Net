using System.Collections.Concurrent;
using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	// reqMktDepthExchanges carries no request id: replies complete the waiting calls in order
	private readonly ConcurrentQueue<
		TaskCompletionSource<IReadOnlyList<MarketDepthExchange>>
	> _depthExchangeWaiters = new();

	/// <summary>
	/// Order book updates for a contract (reqMktDepth) until you stop reading: inserts, updates
	/// and deletes of the rows of each side, up to <paramref name="rows"/> deep. With
	/// <paramref name="isSmartDepth"/> IB aggregates every exchange the account has depth for and
	/// names the exchange of each row (send the contract with exchange SMART). IB allows only a
	/// few depth subscriptions at once (3 by default): more fail with error 309; a missing depth
	/// subscription fails with 354 or 10089.
	/// </summary>
	public IAsyncEnumerable<MarketDepthUpdate> SubscribeMarketDepthAsync(
		Contract contract,
		int rows,
		bool isSmartDepth = false,
		CancellationToken cancellationToken = default
	) =>
		StreamAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestMarketDepth)
					.Add(5)
					.Add(id)
					.AddContract(contract)
					.Add(rows)
					.Add(isSmartDepth)
					.Add(string.Empty), // options
			id =>
				new RequestBuilder(OutgoingMessageIds.CancelMarketDepth)
					.Add(1)
					.Add(id)
					.Add(isSmartDepth),
			fields =>
				fields[0] switch
				{
					IncomingMessageIds.MarketDepth => Decoders.MarketDepth(fields),
					IncomingMessageIds.MarketDepthL2 => Decoders.MarketDepthL2(fields),
					_ => null,
				},
			cancellationToken
		);

	/// <summary>The exchanges IB serves market depth for (reqMktDepthExchanges).</summary>
	public async Task<IReadOnlyList<MarketDepthExchange>> GetMarketDepthExchangesAsync(
		CancellationToken cancellationToken = default
	)
	{
		var waiter = new TaskCompletionSource<IReadOnlyList<MarketDepthExchange>>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		await using var registration = cancellationToken
			.Register(() => waiter.TrySetCanceled())
			.ConfigureAwait(false);
		_depthExchangeWaiters.Enqueue(waiter);
		try
		{
			await _connection
				.SendAsync([OutgoingMessageIds.RequestMarketDepthExchanges], cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			waiter.TrySetException(ex);
		}
		return await waiter.Task.ConfigureAwait(false);
	}

	private void OnMarketDepthExchanges(string[] fields)
	{
		var exchanges = Decoders.MarketDepthExchanges(fields);
		while (_depthExchangeWaiters.TryDequeue(out var waiter))
			if (waiter.TrySetResult(exchanges))
				break;
	}

	private void OnMarketDepthExchangesClosed(Exception error)
	{
		while (_depthExchangeWaiters.TryDequeue(out var waiter))
			waiter.TrySetException(error);
	}
}
