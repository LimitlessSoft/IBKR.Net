using System.Collections.Concurrent;
using System.Globalization;
using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	// reqNewsProviders carries no request id: replies complete the waiting calls in order
	private readonly ConcurrentQueue<
		TaskCompletionSource<IReadOnlyList<NewsProvider>>
	> _newsProviderWaiters = new();

	/// <summary>The news providers the account can read over the API (reqNewsProviders).</summary>
	public async Task<IReadOnlyList<NewsProvider>> GetNewsProvidersAsync(
		CancellationToken cancellationToken = default
	)
	{
		var waiter = new TaskCompletionSource<IReadOnlyList<NewsProvider>>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		await using var registration = cancellationToken
			.Register(() => waiter.TrySetCanceled())
			.ConfigureAwait(false);
		_newsProviderWaiters.Enqueue(waiter);
		try
		{
			await _connection
				.SendAsync([OutgoingMessageIds.RequestNewsProviders], cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			waiter.TrySetException(ex);
		}
		return await waiter.Task.ConfigureAwait(false);
	}

	/// <summary>
	/// Past headlines of a contract from the given providers, newest first (reqHistoricalNews):
	/// the latest ones, or those strictly before <paramref name="before"/> — pass the oldest
	/// headline's time to page back. IB returns at most 300 per request.
	/// </summary>
	public async Task<HistoricalNews> GetHistoricalNewsAsync(
		int conId,
		IEnumerable<string> providerCodes,
		int maxResults,
		DateTimeOffset? before = null,
		CancellationToken cancellationToken = default
	)
	{
		var hasMore = false;
		var headlines = await CollectAsync<NewsHeadline>(
				id =>
					new RequestBuilder(OutgoingMessageIds.RequestHistoricalNews)
						.Add(id)
						.Add(conId)
						.Add(string.Join('+', providerCodes))
						.Add(FormatNewsTime(before)) // IB's "start", observed as an upper bound
						.Add(string.Empty) // IB's "end": no effect observed
						.Add(maxResults)
						.Add(string.Empty), // options
				fields =>
				{
					if (fields[0] == IncomingMessageIds.HistoricalNewsEnd)
						hasMore = Decoders.HistoricalNewsEnd(fields);
					return fields[0] == IncomingMessageIds.HistoricalNews
						? [Decoders.HistoricalNews(fields)]
						: null;
				},
				IncomingMessageIds.HistoricalNewsEnd,
				cancellationToken
			)
			.ConfigureAwait(false);
		return new HistoricalNews(headlines, hasMore);
	}

	/// <summary>The full text of a headline's article (reqNewsArticle).</summary>
	public async Task<NewsArticle> GetNewsArticleAsync(
		string providerCode,
		string articleId,
		CancellationToken cancellationToken = default
	) =>
		(
			await CollectAsync<NewsArticle>(
					id =>
						new RequestBuilder(OutgoingMessageIds.RequestNewsArticle)
							.Add(id)
							.Add(providerCode)
							.Add(articleId)
							.Add(string.Empty), // options
					fields =>
						fields[0] == IncomingMessageIds.NewsArticle
							? [Decoders.NewsArticle(fields)]
							: null,
					IncomingMessageIds.NewsArticle,
					cancellationToken
				)
				.ConfigureAwait(false)
		)[0];

	/// <summary>
	/// Live headlines (news ticks over reqMktData, without quotes) until you stop reading; IB first
	/// replays recent ones. For a contract, pass the providers to listen to; for a broad tape
	/// contract (<see cref="GetBroadTapeContractsAsync"/>) leave them empty. Uses a market data line.
	/// </summary>
	public IAsyncEnumerable<NewsHeadline> SubscribeNewsAsync(
		Contract contract,
		IEnumerable<string>? providerCodes = null,
		CancellationToken cancellationToken = default
	)
	{
		var codes = string.Join('+', providerCodes ?? []);
		var ticks = codes.Length == 0 ? "mdoff,292" : $"mdoff,292:{codes}";
		return StreamAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestMarketData)
					.Add(11)
					.Add(id)
					.AddContract(contract)
					.Add(false) // delta-neutral combo
					.Add(ticks)
					.Add(false) // snapshot
					.Add(false) // regulatory snapshot
					.Add(string.Empty), // options
			id => new RequestBuilder(OutgoingMessageIds.CancelMarketData).Add(1).Add(id),
			fields => fields[0] == IncomingMessageIds.TickNews ? Decoders.TickNews(fields) : null,
			cancellationToken
		);
	}

	/// <summary>
	/// A provider's broad tape contracts (all its headlines, not tied to a symbol), ready for
	/// <see cref="SubscribeNewsAsync"/>. Fails with error 200 or 321 when the provider has none.
	/// </summary>
	public async Task<IReadOnlyList<Contract>> GetBroadTapeContractsAsync(
		string providerCode,
		CancellationToken cancellationToken = default
	)
	{
		var details = await GetContractDetailsAsync(
				new Contract { SecurityType = "NEWS", Exchange = providerCode },
				cancellationToken
			)
			.ConfigureAwait(false);
		// Details name the exchange "NEWS"; a subscription needs the provider there instead
		return
		[
			.. details.Select(x => new Contract
			{
				ConId = x.Contract.ConId,
				Symbol = x.Contract.Symbol,
				SecurityType = "NEWS",
				Exchange = providerCode,
			}),
		];
	}

	private void OnNewsProviders(string[] fields)
	{
		var providers = Decoders.NewsProviders(fields);
		while (_newsProviderWaiters.TryDequeue(out var waiter))
			if (waiter.TrySetResult(providers))
				break;
	}

	private void OnNewsProvidersClosed(Exception error)
	{
		while (_newsProviderWaiters.TryDequeue(out var waiter))
			waiter.TrySetException(error);
	}

	// "2026-10-01 00:00:00.0" in UTC, as replies use
	private static string FormatNewsTime(DateTimeOffset? time) =>
		time?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.0", CultureInfo.InvariantCulture)
		?? string.Empty;
}
