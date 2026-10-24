// Pass-through proxy between an API client and TWS / IB Gateway that logs every frame, so the
// protocol is learned from the wire instead of from IB's source code (see CLAUDE.md).
//
//   dotnet run --project tools/IbkrWireRecorder -- <listenPort> <gatewayPort> <out.jsonl>
//
// Point the client at 127.0.0.1:<listenPort>. Each line of the output is one frame:
//   {"t":"…","dir":"client"|"server","fields":["49","1"]}
// The first client frame is the handshake ("API\0" + version range), logged as one raw field.
// Recordings contain real account data: scrub them before committing anything.

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

if (
	args.Length != 3
	|| !int.TryParse(args[0], out var listenPort)
	|| !int.TryParse(args[1], out var gatewayPort)
)
{
	Console.Error.WriteLine("usage: IbkrWireRecorder <listenPort> <gatewayPort> <out.jsonl>");
	return 2;
}

var output = new StreamWriter(args[2], append: true) { AutoFlush = true };
var outputLock = new Lock();
var listener = new TcpListener(IPAddress.Loopback, listenPort);
listener.Start();
Console.WriteLine(
	$"Recording 127.0.0.1:{listenPort} -> 127.0.0.1:{gatewayPort} into {args[2]} (Ctrl+C stops)"
);

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
	e.Cancel = true;
	stop.Cancel();
};

try
{
	while (!stop.IsCancellationRequested)
	{
		var client = await listener.AcceptTcpClientAsync(stop.Token);
		_ = Task.Run(() => RelayAsync(client, stop.Token));
	}
}
catch (OperationCanceledException) { }
return 0;

async Task RelayAsync(TcpClient client, CancellationToken cancellationToken)
{
	using var _ = client;
	using var gateway = new TcpClient();
	await gateway.ConnectAsync(IPAddress.Loopback, gatewayPort, cancellationToken);
	var fromClient = client.GetStream();
	var fromGateway = gateway.GetStream();

	// Handshake: "API\0" then one frame whose payload is the version range, not NUL fields
	var prefix = new byte[4];
	await fromClient.ReadExactlyAsync(prefix, cancellationToken);
	var (rangeFrame, range) = await ReadFrameAsync(fromClient, cancellationToken);
	await fromGateway.WriteAsync(prefix, cancellationToken);
	await fromGateway.WriteAsync(rangeFrame, cancellationToken);
	Log("client", [Encoding.ASCII.GetString(prefix) + Encoding.UTF8.GetString(range)]);

	await Task.WhenAny(
		PumpAsync(fromClient, fromGateway, "client", cancellationToken),
		PumpAsync(fromGateway, fromClient, "server", cancellationToken)
	);
}

async Task PumpAsync(Stream from, Stream to, string direction, CancellationToken cancellationToken)
{
	try
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var (frame, payload) = await ReadFrameAsync(from, cancellationToken);
			await to.WriteAsync(frame, cancellationToken);
			var text = Encoding.UTF8.GetString(payload);
			Log(direction, text.EndsWith('\0') ? text[..^1].Split('\0') : text.Split('\0'));
		}
	}
	catch (Exception ex)
		when (ex is IOException or EndOfStreamException or OperationCanceledException)
	{
		Log(direction, ["<closed>"]);
	}
}

static async Task<(byte[] Frame, byte[] Payload)> ReadFrameAsync(
	Stream stream,
	CancellationToken cancellationToken
)
{
	var header = new byte[4];
	await stream.ReadExactlyAsync(header, cancellationToken);
	var payload = new byte[BinaryPrimitives.ReadInt32BigEndian(header)];
	await stream.ReadExactlyAsync(payload, cancellationToken);
	return ([.. header, .. payload], payload);
}

void Log(string direction, string[] fields)
{
	var line = JsonSerializer.Serialize(
		new
		{
			t = DateTimeOffset.UtcNow.ToString("O"),
			dir = direction,
			fields,
		}
	);
	lock (outputLock)
		output.WriteLine(line);
}
