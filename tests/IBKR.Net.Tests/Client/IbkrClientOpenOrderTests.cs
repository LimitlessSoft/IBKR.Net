using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

public class IbkrClientOpenOrderTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static FakeTwsServer Server(params string[][] reply) =>
		new() { Respond = f => f[0] == "16" ? reply : [] };

	[Fact]
	public async Task Open_orders_ask_for_every_client_and_take_their_status()
	{
		await using var server = Server(
			RecordedOpenOrders.OpenOrderV187,
			RecordedOpenOrders.OpenOrderStatus,
			["53", "1"]
		);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var order = Assert.Single(await client.GetOpenOrdersAsync(Ct));

		Assert.Equal(["16", "1"], (await server.WaitForFramesAsync(2))[1]);
		Assert.Equal("VOW", order.Contract.Symbol);
		Assert.Equal("Submitted", order.Status);
		Assert.Equal(0, order.Filled);
		Assert.Equal(1, order.Remaining);
	}

	[Fact]
	public async Task No_open_orders_is_an_empty_list()
	{
		await using var server = Server(["53", "1"]);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		Assert.Empty(await client.GetOpenOrdersAsync(Ct));
	}

	[Fact]
	public async Task A_status_of_another_order_does_not_change_an_open_one()
	{
		string[] otherStatus = [.. RecordedOpenOrders.OpenOrderStatus];
		otherStatus[6] = "900000002"; // perm id
		otherStatus[2] = "Cancelled";
		await using var server = Server(RecordedOpenOrders.OpenOrderV178, otherStatus, ["53", "1"]);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		Assert.Equal("", Assert.Single(await client.GetOpenOrdersAsync(Ct)).Status);
	}

	[Fact]
	public async Task Open_orders_fail_when_the_connection_drops()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var request = client.GetOpenOrdersAsync(Ct);
		await server.WaitForFramesAsync(2);

		server.Drop();

		await Assert.ThrowsAsync<IOException>(() => request);
	}

	[Fact]
	public async Task Requests_run_one_at_a_time_and_both_complete()
	{
		await using var server = Server(RecordedOpenOrders.OpenOrderV187, ["53", "1"]);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var results = await Task.WhenAll(
			client.GetOpenOrdersAsync(Ct),
			client.GetOpenOrdersAsync(Ct)
		);

		Assert.All(results, orders => Assert.Single(orders));
	}
}
