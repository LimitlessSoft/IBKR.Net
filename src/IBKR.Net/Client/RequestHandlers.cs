using System.Threading.Channels;

namespace IBKR.Net.Client;

/// <summary>Receives the frames and errors of one request id.</summary>
internal interface IRequestHandler
{
	void OnMessage(string[] fields);
	void OnError(IbkrException error);
	void OnClosed(Exception error);
}

/// <summary>A request answered by data frames and an end frame: completes with all the data.
/// <paramref name="decode"/> returns null for frames without data.</summary>
internal sealed class CollectHandler<T>(Func<string[], IEnumerable<T>?> decode, string endMessageId)
	: IRequestHandler
{
	private readonly List<T> _items = [];
	private readonly TaskCompletionSource<IReadOnlyList<T>> _result = new(
		TaskCreationOptions.RunContinuationsAsynchronously
	);

	public Task<IReadOnlyList<T>> Result => _result.Task;

	// The end frame may carry data itself (historical bars come in one frame that ends the request)
	public void OnMessage(string[] fields)
	{
		if (decode(fields) is { } items)
			_items.AddRange(items);
		if (fields[0] == endMessageId)
			_result.TrySetResult(_items);
	}

	public void OnError(IbkrException error) => _result.TrySetException(error);

	public void OnClosed(Exception error) => _result.TrySetException(error);
}

/// <summary>A subscription: every decoded frame goes to the reader until it is cancelled.</summary>
internal sealed class StreamHandler<T>(Func<string[], T?> decode) : IRequestHandler
	where T : class
{
	private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(
		new UnboundedChannelOptions { SingleReader = true }
	);

	public ChannelReader<T> Reader => _channel.Reader;

	public void OnMessage(string[] fields)
	{
		if (decode(fields) is { } item)
			_channel.Writer.TryWrite(item);
	}

	public void OnError(IbkrException error) => _channel.Writer.TryComplete(error);

	public void OnClosed(Exception error) => _channel.Writer.TryComplete(error);
}

/// <summary>
/// One placed order: its status updates until one is final, or an error rejects it. Calls
/// <paramref name="onDone"/> when nothing more will come.
/// </summary>
internal sealed class OrderHandler(Action onDone) : IRequestHandler
{
	private readonly Channel<OrderStatus> _channel = Channel.CreateUnbounded<OrderStatus>(
		new UnboundedChannelOptions { SingleReader = true }
	);

	public ChannelReader<OrderStatus> Reader => _channel.Reader;

	public void OnMessage(string[] fields)
	{
		if (fields[0] != Protocol.IncomingMessageIds.OrderStatus)
			return;
		var status = Protocol.Decoders.OrderStatus(fields);
		_channel.Writer.TryWrite(status);
		if (status.IsDone && _channel.Writer.TryComplete())
			onDone();
	}

	public void OnError(IbkrException error) => _channel.Writer.TryComplete(error);

	public void OnClosed(Exception error) => _channel.Writer.TryComplete(error);
}

/// <summary>
/// reqAllOpenOrders: open order frames, each followed by its status, until the end frame. Status
/// frames go to placed orders' handlers too, so a status is matched to its order by perm, client
/// and order id rather than routed by id.
/// </summary>
internal sealed class OpenOrdersHandler : IRequestHandler
{
	private readonly List<OpenOrder> _orders = [];
	private readonly Dictionary<(long, int, int), OrderStatus> _statuses = [];
	private readonly TaskCompletionSource<IReadOnlyList<OpenOrder>> _result = new(
		TaskCreationOptions.RunContinuationsAsynchronously
	);

	public Task<IReadOnlyList<OpenOrder>> Result => _result.Task;

	public void OnMessage(string[] fields)
	{
		switch (fields[0])
		{
			case Protocol.IncomingMessageIds.OpenOrder:
				_orders.Add(Protocol.Decoders.OpenOrder(fields));
				break;
			case Protocol.IncomingMessageIds.OrderStatus:
				var status = Protocol.Decoders.OrderStatus(fields);
				_statuses[(status.PermId, status.ClientId, status.OrderId)] = status;
				break;
			case Protocol.IncomingMessageIds.OpenOrderEnd:
				_result.TrySetResult([.. _orders.Select(WithStatus)]);
				break;
		}
	}

	public void OnError(IbkrException error) => _result.TrySetException(error);

	public void OnClosed(Exception error) => _result.TrySetException(error);

	private OpenOrder WithStatus(OpenOrder order) =>
		_statuses.TryGetValue((order.PermId, order.ClientId, order.OrderId), out var status)
			? order with
			{
				Status = status.Status,
				Filled = status.Filled,
				Remaining = status.Remaining,
			}
			: order;
}

/// <summary>
/// reqExecutions: execution frames until the end frame. Commission reports carry no request id,
/// only the exec id, so every running request hears them and keeps those of its own fills.
/// </summary>
internal sealed class ExecutionsHandler : IRequestHandler
{
	private readonly List<Execution> _executions = [];
	private readonly Dictionary<string, CommissionReport> _commissions = [];
	private readonly TaskCompletionSource<IReadOnlyList<Execution>> _result = new(
		TaskCreationOptions.RunContinuationsAsynchronously
	);

	public Task<IReadOnlyList<Execution>> Result => _result.Task;

	public void OnMessage(string[] fields)
	{
		switch (fields[0])
		{
			case Protocol.IncomingMessageIds.ExecutionData:
				_executions.Add(Protocol.Decoders.Execution(fields));
				break;
			case Protocol.IncomingMessageIds.CommissionReport:
				var report = Protocol.Decoders.CommissionReport(fields);
				_commissions[report.ExecId] = report;
				break;
			case Protocol.IncomingMessageIds.ExecutionDataEnd:
				_result.TrySetResult([.. _executions.Select(WithCommission)]);
				break;
		}
	}

	public void OnError(IbkrException error) => _result.TrySetException(error);

	public void OnClosed(Exception error) => _result.TrySetException(error);

	private Execution WithCommission(Execution execution) =>
		_commissions.TryGetValue(execution.ExecId, out var report)
			? execution with
			{
				Commission = report,
			}
			: execution;
}
