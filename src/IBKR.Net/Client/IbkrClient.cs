using System.Collections.Concurrent;
using System.Globalization;
using IBKR.Net.Protocol;
using IBKR.Net.Transport;

namespace IBKR.Net;

/// <summary>
/// A connection to TWS / IB Gateway. Create it with <see cref="ConnectAsync"/>; one-shot requests
/// return tasks, subscriptions return async streams.
/// </summary>
public sealed class IbkrClient : IAsyncDisposable
{
	private readonly TwsConnection _connection;

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

	/// <summary>Raised once when the connection closes; the exception is null for a clean close.</summary>
	public event Action<Exception?>? Disconnected;

	/// <summary>
	/// Connects, agrees on the protocol version and starts the API session. Fails with a
	/// <see cref="TimeoutException"/> after <see cref="IbkrClientOptions.ConnectTimeout"/>.
	/// </summary>
	public static async Task<IbkrClient> ConnectAsync(
		IbkrClientOptions options,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(options);
		return new IbkrClient(await TwsConnection.ConnectAsync(options, cancellationToken));
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
		await using var registration = cancellationToken.Register(() => waiter.TrySetCanceled());
		_serverTimeWaiters.Enqueue(waiter);
		try
		{
			await _connection.SendAsync(
				[OutgoingMessageIds.RequestCurrentTime, "1"],
				cancellationToken
			);
		}
		catch (Exception ex)
		{
			waiter.TrySetException(ex);
		}
		return await waiter.Task;
	}

	/// <summary>Closes the connection; pending requests fail.</summary>
	public ValueTask DisposeAsync() => _connection.DisposeAsync();

	private void OnMessage(string[] fields)
	{
		switch (fields[0])
		{
			case IncomingMessageIds.CurrentTime when fields.Length >= 3:
				var time = DateTimeOffset.FromUnixTimeSeconds(
					long.Parse(fields[2], CultureInfo.InvariantCulture)
				);
				while (_serverTimeWaiters.TryDequeue(out var waiter))
					if (waiter.TrySetResult(time))
						break;
				break;
		}
	}

	private void OnClosed(Exception? error)
	{
		var closed = new IOException("The connection to TWS / IB Gateway closed.", error);
		while (_serverTimeWaiters.TryDequeue(out var waiter))
			waiter.TrySetException(closed);
		Disconnected?.Invoke(error);
	}
}
