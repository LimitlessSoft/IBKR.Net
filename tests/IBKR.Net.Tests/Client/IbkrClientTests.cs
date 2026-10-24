using IBKR.Net.Tests.Fakes;

namespace IBKR.Net.Tests.Client;

public class IbkrClientTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Connect_negotiates_the_version_and_starts_the_api()
	{
		await using var server = new FakeTwsServer(serverVersion: 176);

		await using var client = await IbkrClient.ConnectAsync(server.Options(clientId: 7), Ct);

		Assert.Equal("API\0v100..187", await server.VersionRange);
		Assert.Equal((176, "20261024 12:00:00 CET"), (client.ServerVersion, client.ConnectionTime));
		var frames = await server.WaitForFramesAsync(1);
		Assert.Equal(["71", "2", "7", ""], frames[0]);
	}

	[Fact]
	public async Task Server_time_comes_from_the_current_time_reply()
	{
		await using var server = new FakeTwsServer
		{
			Respond = fields =>
				fields[0] == "49"
					?
					[
						["49", "1", "1792800000"],
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var time = await client.GetServerTimeAsync(Ct);

		Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1792800000), time);
		Assert.Contains(server.Received, x => x.SequenceEqual(["49", "1"]));
	}

	[Fact]
	public async Task A_dropped_connection_fails_pending_requests_and_raises_disconnected()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var disconnected = new TaskCompletionSource<Exception?>();
		client.Disconnected += error => disconnected.TrySetResult(error);
		await server.WaitForFramesAsync(1);

		var pending = client.GetServerTimeAsync(Ct);
		await server.WaitForFramesAsync(2);
		server.Drop();

		await Assert.ThrowsAsync<IOException>(() => pending);
		await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
		Assert.False(client.IsConnected);
	}

	[Fact]
	public async Task Connecting_to_a_closed_port_fails() =>
		await Assert.ThrowsAnyAsync<Exception>(() =>
			IbkrClient.ConnectAsync(
				new IbkrClientOptions { Port = 1, ConnectTimeout = TimeSpan.FromSeconds(2) },
				Ct
			)
		);
}
