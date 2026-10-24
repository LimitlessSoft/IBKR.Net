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
