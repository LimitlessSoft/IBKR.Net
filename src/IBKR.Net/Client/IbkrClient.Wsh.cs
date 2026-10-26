using System.Globalization;
using IBKR.Net.Client;
using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	/// <summary>
	/// Wall Street Horizon's metadata as IB sends it, a JSON document (reqWshMetaData): the event
	/// types and the filter fields event data requests take. Ask for it before
	/// <see cref="GetWshEventDataAsync"/>. Without the WSH subscription IB fails it with 10276.
	/// </summary>
	public Task<string> GetWshMetaDataAsync(CancellationToken cancellationToken = default) =>
		RequestWshAsync(
			id => new RequestBuilder(OutgoingMessageIds.RequestWshMetaData).Add(id),
			id => new RequestBuilder(OutgoingMessageIds.CancelWshMetaData).Add(id),
			IncomingMessageIds.WshMetaData,
			cancellationToken
		);

	/// <summary>
	/// Wall Street Horizon corporate events (earnings dates, dividends, meetings …) as IB sends
	/// them, a JSON document (reqWshEventData). Without the WSH subscription IB fails it with
	/// 10276 "News feed is not allowed."
	/// </summary>
	public Task<string> GetWshEventDataAsync(
		WshEventDataRequest request,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(request);
		return RequestWshAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestWshEventData)
					.Add(id)
					.Add(request.ConId ?? int.MaxValue) // unset, as recorded
					.Add(request.Filter)
					.Add(request.FillWatchlist)
					.Add(request.FillPortfolio)
					.Add(request.FillCompetitors)
					.Add(FormatWshDate(request.StartDate))
					.Add(FormatWshDate(request.EndDate))
					.Add(request.TotalLimit ?? int.MaxValue),
			id => new RequestBuilder(OutgoingMessageIds.CancelWshEventData).Add(id),
			IncomingMessageIds.WshEventData,
			cancellationToken
		);
	}

	// One reply frame carrying the JSON; a caller giving up first cancels the request at IB
	private async Task<string> RequestWshAsync(
		Func<int, RequestBuilder> request,
		Func<int, RequestBuilder> cancel,
		string replyMessageId,
		CancellationToken cancellationToken
	)
	{
		var id = NextRequestId();
		var handler = new CollectHandler<string>(
			fields => fields[0] == replyMessageId ? [Decoders.WshData(fields)] : null,
			replyMessageId
		);
		_requests[id] = handler;
		try
		{
			await _connection
				.SendAsync(request(id).Build(), cancellationToken)
				.ConfigureAwait(false);
			return (await handler.Result.WaitAsync(cancellationToken).ConfigureAwait(false))[0];
		}
		catch (OperationCanceledException) when (_requests.ContainsKey(id) && IsConnected)
		{
			await SendQuietlyAsync(cancel(id)).ConfigureAwait(false);
			throw;
		}
		finally
		{
			_requests.TryRemove(id, out _);
		}
	}

	private static string FormatWshDate(DateOnly? date) =>
		date?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty;
}
