using IBKR.Net.Client;
using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
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
	/// Asks IB to cancel an order (cancelOrder); its status stream ends with Cancelled or
	/// ApiCancelled. An order IB doesn't know fails that stream with error 10147.
	/// </summary>
	public Task CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
	{
		EnsureOrderVersion();
		return _connection.SendAsync(OrderEncoder.CancelOrder(orderId), cancellationToken);
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
