using System.Runtime.CompilerServices;
using IBKR.Net.Client;
using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	// reqPositions has no request id: every active positions request hears every position frame
	private readonly Lock _positionsLock = new();
	private readonly List<IRequestHandler> _positionListeners = [];

	/// <summary>Current positions of every managed account (reqPositions, until its end).</summary>
	public async Task<IReadOnlyList<Position>> GetPositionsAsync(
		CancellationToken cancellationToken = default
	)
	{
		var handler = new CollectHandler<Position>(
			fields => fields[0] == IncomingMessageIds.Position ? [Decoders.Position(fields)] : null,
			IncomingMessageIds.PositionEnd
		);
		AddPositionListener(handler);
		try
		{
			await RequestPositionsAsync(cancellationToken).ConfigureAwait(false);
			return await handler.Result.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			await RemovePositionListenerAsync(handler).ConfigureAwait(false);
		}
	}

	/// <summary>Current positions, then every change, until you stop reading (reqPositions).</summary>
	public async IAsyncEnumerable<Position> SubscribePositionsAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	)
	{
		var handler = new StreamHandler<Position>(fields =>
			fields[0] == IncomingMessageIds.Position ? [Decoders.Position(fields)] : null
		);
		AddPositionListener(handler);
		try
		{
			await RequestPositionsAsync(cancellationToken).ConfigureAwait(false);
			await foreach (
				var position in handler.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)
			)
				yield return position;
		}
		finally
		{
			await RemovePositionListenerAsync(handler).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Account summary values (reqAccountSummary): the current values, then each change, until
	/// you stop reading. IB allows two of these per connection, and IB Gateway does not free a slot
	/// when one is cancelled: keep one open rather than taking repeated snapshots.
	/// </summary>
	public IAsyncEnumerable<AccountValue> SubscribeAccountSummaryAsync(
		IEnumerable<string> tags,
		string group = "All",
		CancellationToken cancellationToken = default
	) =>
		StreamAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestAccountSummary)
					.Add(1)
					.Add(id)
					.Add(group)
					.Add(string.Join(",", tags)),
			id => new RequestBuilder(OutgoingMessageIds.CancelAccountSummary).Add(1).Add(id),
			fields =>
				fields[0] == IncomingMessageIds.AccountSummary
					? Decoders.AccountSummary(fields)
					: null,
			cancellationToken
		);

	/// <summary>Daily, unrealized and realized P&amp;L of an account, as it changes (reqPnL).</summary>
	public IAsyncEnumerable<AccountPnl> SubscribeAccountPnlAsync(
		string account,
		CancellationToken cancellationToken = default
	) =>
		StreamAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestPnl)
					.Add(id)
					.Add(account)
					.Add(string.Empty), // model code
			id => new RequestBuilder(OutgoingMessageIds.CancelPnl).Add(id),
			fields => fields[0] == IncomingMessageIds.Pnl ? Decoders.Pnl(fields) : null,
			cancellationToken
		);

	/// <summary>P&amp;L of one position, as it changes (reqPnLSingle).</summary>
	public IAsyncEnumerable<PositionPnl> SubscribePositionPnlAsync(
		string account,
		int conId,
		CancellationToken cancellationToken = default
	) =>
		StreamAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestPnlSingle)
					.Add(id)
					.Add(account)
					.Add(string.Empty) // model code
					.Add(conId),
			id => new RequestBuilder(OutgoingMessageIds.CancelPnlSingle).Add(id),
			fields => fields[0] == IncomingMessageIds.PnlSingle ? Decoders.PnlSingle(fields) : null,
			cancellationToken
		);

	private Task RequestPositionsAsync(CancellationToken cancellationToken) =>
		_connection.SendAsync(
			new RequestBuilder(OutgoingMessageIds.RequestPositions).Add(1).Build(),
			cancellationToken
		);

	private void AddPositionListener(IRequestHandler handler)
	{
		lock (_positionsLock)
			_positionListeners.Add(handler);
	}

	// The last listener leaving cancels the positions feed
	private async Task RemovePositionListenerAsync(IRequestHandler handler)
	{
		bool last;
		lock (_positionsLock)
		{
			_positionListeners.Remove(handler);
			last = _positionListeners.Count == 0;
		}
		if (last && IsConnected)
			await SendQuietlyAsync(new RequestBuilder(OutgoingMessageIds.CancelPositions).Add(1))
				.ConfigureAwait(false);
	}

	private void OnPositionMessage(string[] fields)
	{
		IRequestHandler[] listeners;
		lock (_positionsLock)
			listeners = [.. _positionListeners];
		foreach (var listener in listeners)
			listener.OnMessage(fields);
	}

	private void OnPositionsClosed(Exception error)
	{
		IRequestHandler[] listeners;
		lock (_positionsLock)
			listeners = [.. _positionListeners];
		foreach (var listener in listeners)
			listener.OnClosed(error);
	}
}
