using System.Collections.Concurrent;
using System.Globalization;
using IBKR.Net.Client;
using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	// reqAllOpenOrders has no request id: the one running request hears every order frame
	private readonly SemaphoreSlim _openOrdersGate = new(1, 1);
	private volatile OpenOrdersHandler? _openOrders;

	// Modifications waiting for IB's answer, by order id: one at a time per order
	private readonly ConcurrentDictionary<int, TaskCompletionSource> _modifications = new();

	/// <summary>
	/// The protocol version orders work at: connect with
	/// <see cref="IbkrClientOptions.MaxServerVersion"/> set to it to place and cancel orders.
	/// </summary>
	public const int OrderProtocolVersion = ProtocolVersions.Orders;

	/// <summary>
	/// Sends an order (placeOrder) and returns its id and status updates. This trades for real on
	/// a live account. Needs a connection at <see cref="OrderProtocolVersion"/>.
	/// </summary>
	public async Task<PlacedOrder> PlaceOrderAsync(
		Contract contract,
		Order order,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(contract);
		ArgumentNullException.ThrowIfNull(order);
		EnsureOrderVersion();

		var id = NextRequestId();
		var handler = new OrderHandler(() => _requests.TryRemove(id, out _));
		_requests[id] = handler;
		try
		{
			await _connection
				.SendAsync(OrderEncoder.PlaceOrder(id, contract, order), cancellationToken)
				.ConfigureAwait(false);
		}
		catch
		{
			_requests.TryRemove(id, out _);
			throw;
		}
		return new PlacedOrder(id, handler.Reader.ReadAllAsync());
	}

	/// <summary>
	/// Changes a working order: placeOrder re-sent with the order's id. <paramref name="order"/>
	/// replaces the order as a whole, so pass every field as it should stay (side, quantity, type,
	/// time in force, account, outside-hours flag) with the new price. IB takes it only from the
	/// client that placed the order. This trades for real on a live account. Needs a connection at
	/// <see cref="OrderProtocolVersion"/>.
	/// <para>
	/// Completes when IB answers with an open order or order status frame for the order id; a
	/// refusal (an error for the order id) throws <see cref="IbkrException"/> and leaves the order
	/// as it was: an earlier <see cref="PlaceOrderAsync"/> stream of it is not failed. One
	/// modification per order at a time. Built from the placeOrder layout; a modification's
	/// answer has not been seen on the wire yet, so pass a cancellation token with a timeout.
	/// </para>
	/// </summary>
	public async Task ModifyOrderAsync(
		int orderId,
		Contract contract,
		Order order,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
		ArgumentNullException.ThrowIfNull(contract);
		ArgumentNullException.ThrowIfNull(order);
		EnsureOrderVersion();

		var answer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!_modifications.TryAdd(orderId, answer))
			throw new InvalidOperationException(
				$"Order {orderId} has a modification waiting for IB's answer."
			);
		try
		{
			await _connection
				.SendAsync(OrderEncoder.PlaceOrder(orderId, contract, order), cancellationToken)
				.ConfigureAwait(false);
			await answer.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			_modifications.TryRemove(new(orderId, answer));
		}
	}

	/// <summary>
	/// Asks IB to cancel an order (cancelOrder); its status stream ends with Cancelled or
	/// ApiCancelled. An order IB doesn't know fails that stream with error 10147.
	/// </summary>
	public Task CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
	{
		EnsureOrderVersion();
		return _connection.SendAsync(OrderEncoder.CancelOrder(orderId), cancellationToken);
	}

	/// <summary>
	/// Every working order of the login, whichever client or TWS placed it (reqAllOpenOrders),
	/// with its status. Read at protocol versions 178 and 187. One request runs at a time: IB's
	/// reply carries no request id.
	/// </summary>
	public async Task<IReadOnlyList<OpenOrder>> GetOpenOrdersAsync(
		CancellationToken cancellationToken = default
	)
	{
		await _openOrdersGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		var handler = new OpenOrdersHandler();
		_openOrders = handler;
		try
		{
			await _connection
				.SendAsync(
					new RequestBuilder(OutgoingMessageIds.RequestAllOpenOrders).Add(1).Build(),
					cancellationToken
				)
				.ConfigureAwait(false);
			return await handler.Result.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			_openOrders = null;
			_openOrdersGate.Release();
		}
	}

	/// <summary>
	/// The login's fills of the day, of every account, client and TWS (reqExecutions without
	/// filters), each with its commission report. Read at protocol versions 178 and 187.
	/// </summary>
	public Task<IReadOnlyList<Execution>> GetExecutionsAsync(
		CancellationToken cancellationToken = default
	)
	{
		var handler = new ExecutionsHandler();
		return RequestAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestExecutions)
					.Add(3)
					.Add(id)
					.Add(0) // client id filter: every client
					.Add(string.Empty) // account
					.Add(string.Empty) // time
					.Add(string.Empty) // symbol
					.Add(string.Empty) // security type
					.Add(string.Empty) // exchange
					.Add(string.Empty), // side
			handler,
			handler.Result,
			cancellationToken
		);
	}

	// An open order or order status frame for the order id answers its modification
	private void OnModificationAnswer(string[] fields)
	{
		if (
			fields.Length > 1
			&& int.TryParse(fields[1], CultureInfo.InvariantCulture, out var orderId)
			&& _modifications.TryRemove(orderId, out var answer)
		)
			answer.TrySetResult();
	}

	// An error for the order id refuses its modification; the order itself goes on as it was
	private bool OnModificationError(IbkrNotice notice)
	{
		if (!_modifications.TryRemove(notice.RequestId, out var answer))
			return false;
		answer.TrySetException(new IbkrException(notice.RequestId, notice.Code, notice.Message));
		return true;
	}

	private void OnModificationsClosed(Exception error)
	{
		foreach (var id in _modifications.Keys)
			if (_modifications.TryRemove(id, out var answer))
				answer.TrySetException(error);
	}

	private void EnsureOrderVersion()
	{
		if (ServerVersion != OrderProtocolVersion)
			throw new NotSupportedException(
				$"Orders need a connection at protocol version {OrderProtocolVersion} (this one is "
					+ $"{ServerVersion}); connect with MaxServerVersion = OrderProtocolVersion."
			);
	}

	// Order ids must be at least the server's next valid id; request ids share the counter
	private void OnNextValidId(string[] fields)
	{
		if (
			fields.Length < 3
			|| !int.TryParse(
				fields[2],
				System.Globalization.CultureInfo.InvariantCulture,
				out var next
			)
		)
			return;
		int last;
		do
		{
			last = Volatile.Read(ref _lastRequestId);
			if (last >= next - 1)
				return;
		} while (Interlocked.CompareExchange(ref _lastRequestId, next - 1, last) != last);
	}
}
