using System.Globalization;
using System.Net.Sockets;
using System.Text;
using IBKR.Net.Protocol;

namespace IBKR.Net.Transport;

/// <summary>
/// One API socket to TWS / IB Gateway: the version handshake, serialized frame writes and a
/// background reader that hands every incoming frame to <see cref="MessageReceived"/>.
/// </summary>
internal sealed class TwsConnection : IAsyncDisposable
{
	private static readonly byte[] ApiPrefix = Encoding.ASCII.GetBytes("API\0");

	private readonly TcpClient _tcp;
	private readonly Stream _stream;
	private readonly SemaphoreSlim _writeLock = new(1, 1);
	private readonly CancellationTokenSource _stop = new();
	private Task? _reader;
	private int _closed;

	private TwsConnection(TcpClient tcp, int serverVersion, string connectionTime)
	{
		_tcp = tcp;
		_stream = tcp.GetStream();
		ServerVersion = serverVersion;
		ConnectionTime = connectionTime;
	}

	/// <summary>The protocol version both sides agreed on; message formats depend on it.</summary>
	public int ServerVersion { get; }

	/// <summary>The server's local time at connect, as it sent it (e.g. "20261024 17:38:01 CET").</summary>
	public string ConnectionTime { get; }

	public bool IsConnected => Volatile.Read(ref _closed) == 0;

	/// <summary>Raised on the reader thread for every frame; handlers must be quick.</summary>
	public event Action<string[]>? MessageReceived;

	/// <summary>Raised once when the socket closes; the exception is null for a clean close.</summary>
	public event Action<Exception?>? Closed;

	public static async Task<TwsConnection> ConnectAsync(
		IbkrClientOptions options,
		CancellationToken cancellationToken
	)
	{
		var versionRange = ProtocolVersions.Range(options.MaxServerVersion);
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(options.ConnectTimeout);

		var tcp = new TcpClient { NoDelay = true };
		try
		{
			await tcp.ConnectAsync(options.Host, options.Port, timeout.Token).ConfigureAwait(false);
			var stream = tcp.GetStream();

			await stream.WriteAsync(ApiPrefix, timeout.Token).ConfigureAwait(false);
			await stream
				.WriteAsync(Framing.EncodeRaw(versionRange), timeout.Token)
				.ConfigureAwait(false);

			var reply =
				await Framing.ReadFieldsAsync(stream, timeout.Token).ConfigureAwait(false)
				?? throw new IOException("The server closed the connection during the handshake.");
			if (
				reply.Length < 2
				|| !int.TryParse(reply[0], CultureInfo.InvariantCulture, out var serverVersion)
			)
				throw new InvalidDataException(
					$"Unexpected handshake reply: {string.Join(" | ", reply)}."
				);

			var connection = new TwsConnection(tcp, serverVersion, reply[1]);
			await connection
				.SendAsync(
					[
						OutgoingMessageIds.StartApi,
						"2",
						options.ClientId.ToString(CultureInfo.InvariantCulture),
						string.Empty,
					],
					timeout.Token
				)
				.ConfigureAwait(false);
			return connection;
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			tcp.Dispose();
			throw new TimeoutException(
				$"Connecting to {options.Host}:{options.Port} took longer than {options.ConnectTimeout}."
			);
		}
		catch
		{
			tcp.Dispose();
			throw;
		}
	}

	/// <summary>Starts reading; call once, after subscribing to the events.</summary>
	public void StartReading() => _reader ??= Task.Run(ReadLoopAsync);

	public async Task SendAsync(IReadOnlyList<string> fields, CancellationToken cancellationToken)
	{
		if (!IsConnected)
			throw new IOException("The connection to TWS / IB Gateway is closed.");

		var frame = Framing.EncodeFields(fields);
		await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			await _stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public async ValueTask DisposeAsync()
	{
		await _stop.CancelAsync().ConfigureAwait(false);
		_tcp.Dispose();
		if (_reader != null)
			await _reader.ConfigureAwait(false);
		OnClosed(null);
		_stop.Dispose();
		_writeLock.Dispose();
	}

	private async Task ReadLoopAsync()
	{
		Exception? error = null;
		try
		{
			while (
				await Framing.ReadFieldsAsync(_stream, _stop.Token).ConfigureAwait(false)
					is { } fields
			)
				if (fields.Length > 0)
					MessageReceived?.Invoke(fields);
		}
		catch (Exception) when (_stop.IsCancellationRequested) { }
		catch (Exception ex)
		{
			error = ex;
		}
		OnClosed(error);
	}

	private void OnClosed(Exception? error)
	{
		if (Interlocked.Exchange(ref _closed, 1) == 0)
			Closed?.Invoke(error);
	}
}
