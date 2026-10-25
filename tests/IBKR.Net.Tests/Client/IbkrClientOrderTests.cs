using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

public class IbkrClientOrderTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static readonly Contract Btc = new()
	{
		ConId = 479624278,
		Symbol = "BTC",
		SecurityType = "CRYPTO",
		Exchange = "PAXOS",
		Currency = "USD",
	};

	private static readonly Order Buy = new()
	{
		Action = "BUY",
		Quantity = 0.0002m,
		OrderType = "LMT",
		LimitPrice = 20000,
		TimeInForce = "GTC",
		Account = "DU0000001",
	};

	private static FakeTwsServer OrderServer() =>
		new(serverVersion: IbkrClient.OrderProtocolVersion)
		{
			SessionStart =
			[
				["15", "1", "DU0000001,DU0000002,"],
				["9", "1", "100"],
			],
		};

	private static IbkrClientOptions OrderOptions(FakeTwsServer server) =>
		new()
		{
			Port = server.Port,
			ClientId = 7,
			MaxServerVersion = IbkrClient.OrderProtocolVersion,
		};

	private static string[] Status(int orderId, string status, string filled, string remaining) =>
		["3", orderId.ToString(), status, filled, remaining, "", "0", "0", "", "7", "", ""];

	[Fact]
	public async Task An_order_connection_offers_versions_up_to_the_order_version()
	{
		await using var server = OrderServer();
		await using var client = await IbkrClient.ConnectAsync(OrderOptions(server), Ct);

		Assert.Equal("API\0v100..178", await server.VersionRange);
	}

	[Fact]
	public async Task A_version_outside_the_supported_range_is_rejected() =>
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
			IbkrClient.ConnectAsync(new IbkrClientOptions { Port = 1, MaxServerVersion = 99 }, Ct)
		);

	[Fact]
	public async Task Orders_need_a_connection_at_the_order_version()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		await Assert.ThrowsAsync<NotSupportedException>(() => client.PlaceOrderAsync(Btc, Buy, Ct));
	}

	[Fact]
	public async Task An_order_takes_the_next_valid_id_and_streams_statuses_until_done()
	{
		await using var server = OrderServer();
		server.Respond = f =>
			f[0] switch
			{
				"3" => [Status(int.Parse(f[1]), "Submitted", "0", "0.0002")],
				"4" => [Status(int.Parse(f[2]), "Cancelled", "0", "0")],
				_ => [],
			};
		await using var client = await IbkrClient.ConnectAsync(OrderOptions(server), Ct);

		var order = await client.PlaceOrderAsync(Btc, Buy, Ct);
		var statuses = new List<string>();
		await foreach (var status in order.Statuses.WithCancellation(Ct))
		{
			statuses.Add(status.Status);
			if (status.Status == "Submitted")
				await client.CancelOrderAsync(order.OrderId, Ct);
		}

		Assert.Equal(100, order.OrderId);
		Assert.Equal(["Submitted", "Cancelled"], statuses);
		Assert.Contains(server.Received, x => x.SequenceEqual(["4", "1", "100", ""]));
	}

	[Fact]
	public async Task A_rejection_fails_the_status_stream()
	{
		await using var server = OrderServer();
		server.Respond = f =>
			f[0] == "3"
				?
				[
					[
						.. RecordedOrders.NoTradingPermissions[..2],
						f[1],
						.. RecordedOrders.NoTradingPermissions[3..],
					],
				]
				: [];
		await using var client = await IbkrClient.ConnectAsync(OrderOptions(server), Ct);

		var order = await client.PlaceOrderAsync(Btc, Buy, Ct);

		var error = await Assert.ThrowsAsync<IbkrException>(async () =>
		{
			await foreach (var _ in order.Statuses.WithCancellation(Ct)) { }
		});
		Assert.Equal((460, order.OrderId), (error.Code, error.RequestId));
	}

	[Fact]
	public async Task A_cancel_confirmation_error_does_not_end_the_order()
	{
		await using var server = OrderServer();
		server.Respond = f =>
			f[0] == "3"
				?
				[
					["4", "2", f[1], "202", "Order Canceled - reason:", ""],
					Status(int.Parse(f[1]), "Cancelled", "0", "0"),
				]
				: [];
		await using var client = await IbkrClient.ConnectAsync(OrderOptions(server), Ct);

		var order = await client.PlaceOrderAsync(Btc, Buy, Ct);
		var last = await order.Statuses.LastAsync(Ct);

		Assert.Equal("Cancelled", last.Status);
	}
}
