using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using IBKR.Net.Transport;

namespace IBKR.Net.Tests.Fakes;

/// <summary>
/// A TWS stand-in on loopback: accepts one client, answers the version handshake and replies to
/// each request frame with whatever <see cref="Respond"/> returns.
/// </summary>
public sealed class FakeTwsServer : IAsyncDisposable
{
	private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
	private readonly CancellationTokenSource _stop = new();
	private readonly Task _serve;
	private readonly TaskCompletionSource<string> _versionRange = new(
		TaskCreationOptions.RunContinuationsAsynchronously
	);
	private NetworkStream? _client;

	public FakeTwsServer(int serverVersion = 187, string connectionTime = "20261024 12:00:00 CET")
	{
		ServerVersion = serverVersion;
		ConnectionTime = connectionTime;
		_listener.Start();
		_serve = Task.Run(ServeAsync);
	}

	public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
	public int ServerVersion { get; }
	public string ConnectionTime { get; }

	/// <summary>Every frame the client sent after the handshake, in order.</summary>
	public ConcurrentQueue<string[]> Received { get; } = new();

	/// <summary>The handshake's version range, e.g. "v100..187".</summary>
	public Task<string> VersionRange => _versionRange.Task;

	/// <summary>Frames to send back for a request frame; none by default.</summary>
	public Func<string[], IEnumerable<string[]>> Respond { get; set; } = _ => [];

	public IbkrClientOptions Options(int clientId = 7) =>
		new()
		{
			Host = "127.0.0.1",
			Port = Port,
			ClientId = clientId,
		};

	/// <summary>Sends a frame unprompted, e.g. a market data tick.</summary>
	public async Task SendAsync(params string[] fields) =>
		await _client!.WriteAsync(Framing.EncodeFields(fields), _stop.Token);

	/// <summary>Closes the client's socket from the server side.</summary>
	public void Drop() => _client?.Socket.Close();

	/// <summary>Waits until the client has sent <paramref name="count"/> frames.</summary>
	public async Task<string[][]> WaitForFramesAsync(int count)
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		while (Received.Count < count)
			await Task.Delay(10, timeout.Token);
		return [.. Received];
	}

	public async ValueTask DisposeAsync()
	{
		await _stop.CancelAsync();
		_client?.Dispose();
		_listener.Stop();
		try
		{
			await _serve;
		}
		catch (Exception) { }
		_stop.Dispose();
	}

	private async Task ServeAsync()
	{
		using var socket = await _listener.AcceptTcpClientAsync(_stop.Token);
		_client = socket.GetStream();

		var prefix = new byte[4];
		await _client.ReadExactlyAsync(prefix, _stop.Token);
		var header = new byte[4];
		await _client.ReadExactlyAsync(header, _stop.Token);
		var range = new byte[BinaryPrimitives.ReadInt32BigEndian(header)];
		await _client.ReadExactlyAsync(range, _stop.Token);
		_versionRange.TrySetResult(
			Encoding.ASCII.GetString(prefix) + Encoding.ASCII.GetString(range)
		);

		await _client.WriteAsync(
			Framing.EncodeFields([ServerVersion.ToString(), ConnectionTime]),
			_stop.Token
		);

		while (await Framing.ReadFieldsAsync(_client, _stop.Token) is { } fields)
		{
			Received.Enqueue(fields);
			foreach (var reply in Respond(fields))
				await _client.WriteAsync(Framing.EncodeFields(reply), _stop.Token);
		}
	}
}
