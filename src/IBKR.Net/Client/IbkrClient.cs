using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using IBKR.Net.Client;
using IBKR.Net.Protocol;
using IBKR.Net.Transport;

namespace IBKR.Net;

/// <summary>
/// A connection to TWS / IB Gateway. Create it with <see cref="ConnectAsync"/>; one-shot requests
/// return tasks, subscriptions return async streams that cancel the IB request when you stop
/// reading.
/// </summary>
public sealed partial class IbkrClient : IAsyncDisposable
{
	private readonly TwsConnection _connection;
	private readonly ConcurrentDictionary<int, IRequestHandler> _requests = new();
	private readonly TaskCompletionSource _ready = new(
		TaskCreationOptions.RunContinuationsAsynchronously
	);
	private int _lastRequestId;

	// reqCurrentTime carries no request id: replies complete the waiting calls in order
	private readonly ConcurrentQueue<TaskCompletionSource<DateTimeOffset>> _serverTimeWaiters =
		new();

	private IbkrClient(TwsConnection connection)
	{
		_connection = connection;
		connection.MessageReceived += OnMessage;
		connection.Closed += OnClosed;
		connection.StartReading();
	}

	/// <summary>The protocol version agreed with the server.</summary>
	public int ServerVersion => _connection.ServerVersion;

	/// <summary>The server's local time at connect, as the server formatted it.</summary>
	public string ConnectionTime => _connection.ConnectionTime;

	/// <summary>False once the socket has closed; a closed client can't reconnect.</summary>
	public bool IsConnected => _connection.IsConnected;

	/// <summary>The accounts this login can see, known once <see cref="ConnectAsync"/> returns.</summary>
	public IReadOnlyList<string> ManagedAccounts { get; private set; } = [];

	/// <summary>Raised once when the connection closes; the exception is null for a clean close.</summary>
	public event Action<Exception?>? Disconnected;

	/// <summary>
	/// Informational messages and errors not tied to a request (farm status, delayed data shown,
	/// connectivity). Raised on the reader thread; keep handlers quick.
	/// </summary>
	public event Action<IbkrNotice>? Notice;

	/// <summary>
	/// Connects, agrees on the protocol version, starts the API session and waits until the server
	/// is ready. Fails with a <see cref="TimeoutException"/> after
	/// <see cref="IbkrClientOptions.ConnectTimeout"/>.
	/// </summary>
	public static async Task<IbkrClient> ConnectAsync(
		IbkrClientOptions options,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(options);
		var client = new IbkrClient(
			await TwsConnection.ConnectAsync(options, cancellationToken).ConfigureAwait(false)
		);
		try
		{
			await client
				._ready.Task.WaitAsync(options.ConnectTimeout, cancellationToken)
				.ConfigureAwait(false);
			return client;
		}
		catch
		{
			await client.DisposeAsync().ConfigureAwait(false);
			throw;
		}
	}

	/// <summary>The server's current time; a cheap round trip, usable as a heartbeat.</summary>
	public async Task<DateTimeOffset> GetServerTimeAsync(
		CancellationToken cancellationToken = default
	)
	{
		var waiter = new TaskCompletionSource<DateTimeOffset>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		// A cancelled or unsent request leaves its waiter completed; replies skip completed waiters
		await using var registration = cancellationToken
			.Register(() => waiter.TrySetCanceled())
			.ConfigureAwait(false);
		_serverTimeWaiters.Enqueue(waiter);
		try
		{
			await _connection
				.SendAsync([OutgoingMessageIds.RequestCurrentTime, "1"], cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			waiter.TrySetException(ex);
		}
		return await waiter.Task.ConfigureAwait(false);
	}

	/// <summary>Closes the connection; pending requests and streams fail.</summary>
	public ValueTask DisposeAsync() => _connection.DisposeAsync();

	private int NextRequestId() => Interlocked.Increment(ref _lastRequestId);

	// A one-shot request: data frames until the end frame
	private async Task<IReadOnlyList<T>> CollectAsync<T>(
		Func<int, RequestBuilder> request,
		Func<string[], IEnumerable<T>?> decode,
		string endMessageId,
		CancellationToken cancellationToken
	)
	{
		var id = NextRequestId();
		var handler = new CollectHandler<T>(decode, endMessageId);
		_requests[id] = handler;
		try
		{
			await _connection
				.SendAsync(request(id).Build(), cancellationToken)
				.ConfigureAwait(false);
			return await handler.Result.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			_requests.TryRemove(id, out _);
		}
	}

	// A subscription: decoded frames until the caller stops reading, then the IB cancel message
	private async IAsyncEnumerable<T> StreamAsync<T>(
		Func<int, RequestBuilder> request,
		Func<int, RequestBuilder> cancel,
		Func<string[], T?> decode,
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
		where T : class
	{
		var id = NextRequestId();
		var handler = new StreamHandler<T>(decode);
		_requests[id] = handler;
		try
		{
			await _connection
				.SendAsync(request(id).Build(), cancellationToken)
				.ConfigureAwait(false);
			await foreach (
				var item in handler.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)
			)
				yield return item;
		}
		finally
		{
			// Still registered = the caller stopped reading; an IB error has removed it already
			if (_requests.TryRemove(id, out _) && IsConnected)
				await SendQuietlyAsync(cancel(id)).ConfigureAwait(false);
		}
	}

	private async Task SendQuietlyAsync(RequestBuilder request)
	{
		try
		{
			await _connection
				.SendAsync(request.Build(), CancellationToken.None)
				.ConfigureAwait(false);
		}
		catch (IOException) { }
	}

	private void OnMessage(string[] fields)
	{
		var messageId = fields[0];
		switch (messageId)
		{
			case IncomingMessageIds.Error:
				OnError(Decoders.Error(fields));
				return;
			case IncomingMessageIds.ManagedAccounts:
				ManagedAccounts = Decoders.ManagedAccounts(fields);
				return;
			case IncomingMessageIds.NextValidId:
				_ready.TrySetResult();
				return;
			case IncomingMessageIds.CurrentTime when fields.Length >= 3:
				var time = DateTimeOffset.FromUnixTimeSeconds(
					long.Parse(fields[2], CultureInfo.InvariantCulture)
				);
				while (_serverTimeWaiters.TryDequeue(out var waiter))
					if (waiter.TrySetResult(time))
						break;
				return;
			case IncomingMessageIds.Position or IncomingMessageIds.PositionEnd:
				OnPositionMessage(fields);
				return;
		}

		if (
			IncomingMessageIds.RequestIdIndex(messageId) is { } index
			&& index < fields.Length
			&& int.TryParse(fields[index], CultureInfo.InvariantCulture, out var requestId)
			&& _requests.TryGetValue(requestId, out var handler)
		)
			handler.OnMessage(fields);
	}

	private void OnError(IbkrNotice notice)
	{
		if (
			notice.RequestId > 0
			&& !ErrorCodes.IsInformational(notice.Code)
			&& _requests.TryRemove(notice.RequestId, out var handler)
		)
		{
			handler.OnError(new IbkrException(notice.RequestId, notice.Code, notice.Message));
			return;
		}
		Notice?.Invoke(notice);
	}

	private void OnClosed(Exception? error)
	{
		var closed = new IOException("The connection to TWS / IB Gateway closed.", error);
		_ready.TrySetException(closed);
		while (_serverTimeWaiters.TryDequeue(out var waiter))
			waiter.TrySetException(closed);
		foreach (var id in _requests.Keys)
			if (_requests.TryRemove(id, out var handler))
				handler.OnClosed(closed);
		OnPositionsClosed(closed);
		Disconnected?.Invoke(error);
	}
}
