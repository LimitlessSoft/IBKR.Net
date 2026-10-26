using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// Market depth requests go out as recorded (docs/protocol.md, "Market depth"); replies are
/// recorded frames.
/// </summary>
public class IbkrClientMarketDepthTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static readonly Contract Aapl = new()
	{
		ConId = 265598,
		Symbol = "AAPL",
		SecurityType = "STK",
		Exchange = "SMART",
		Currency = "USD",
	};

	private static string[] WithRequestId(string[] frame, int index, string id)
	{
		var copy = (string[])frame.Clone();
		copy[index] = id;
		return copy;
	}

	private static async Task<List<MarketDepthUpdate>> ReadAsync(
		IAsyncEnumerable<MarketDepthUpdate> stream,
		int count
	)
	{
		var updates = new List<MarketDepthUpdate>();
		await foreach (var update in stream)
		{
			updates.Add(update);
			if (updates.Count == count)
				break;
		}
		return updates;
	}

	[Fact]
	public async Task Smart_depth_matches_the_recording_and_names_the_exchange_of_each_row()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "10"
					?
					[
						WithRequestId(RecordedMarketDepth.PermissionsNotice, 2, f[2]),
						.. RecordedMarketDepth.SmartUpdates.Select(x => WithRequestId(x, 2, f[2])),
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var notices = new List<IbkrNotice>();
		client.Notice += notices.Add;

		var updates = await ReadAsync(
			client.SubscribeMarketDepthAsync(Aapl, 10, isSmartDepth: true, Ct),
			3
		);

		var sent = server.Received.Single(x => x[0] == "10");
		Assert.Equal(WithRequestId(RecordedMarketDepth.SmartRequest, 2, sent[2]), sent);
		Assert.Equal(
			new MarketDepthUpdate(
				0,
				MarketDepthOperation.Insert,
				MarketDepthSide.Bid,
				343.84,
				40,
				"IEX",
				true
			),
			updates[0]
		);
		Assert.Equal(
			new MarketDepthUpdate(
				2,
				MarketDepthOperation.Update,
				MarketDepthSide.Ask,
				344.05,
				80,
				"DRCTEDGE",
				true
			),
			updates[2]
		);
		Assert.Contains(notices, x => x.Code == 2152);
		var cancel = (await server.WaitForFramesAsync(3)).Single(x => x[0] == "11");
		Assert.Equal(WithRequestId(RecordedMarketDepth.SmartCancel, 2, sent[2]), cancel);
	}

	[Fact]
	public async Task One_exchange_depth_has_no_market_maker_and_deletes_carry_zeros()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "10"
					? RecordedMarketDepth.ExchangeUpdates.Select(x => WithRequestId(x, 2, f[2]))
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var updates = await ReadAsync(
			client.SubscribeMarketDepthAsync(Aapl with { Exchange = "IEX" }, 5, false, Ct),
			3
		);

		var sent = server.Received.Single(x => x[0] == "10");
		Assert.Equal(WithRequestId(RecordedMarketDepth.ExchangeRequest, 2, sent[2]), sent);
		Assert.Equal(
			new MarketDepthUpdate(1, MarketDepthOperation.Update, MarketDepthSide.Bid, 343.84, 42),
			updates[1]
		);
		Assert.Equal(
			new MarketDepthUpdate(4, MarketDepthOperation.Delete, MarketDepthSide.Bid, 0, 0),
			updates[2]
		);
		var cancel = (await server.WaitForFramesAsync(3)).Single(x => x[0] == "11");
		Assert.Equal(WithRequestId(RecordedMarketDepth.ExchangeCancel, 2, sent[2]), cancel);
	}

	[Theory]
	[InlineData(309)]
	[InlineData(354)]
	public async Task A_rejected_depth_request_fails_the_stream_without_a_cancel(int code)
	{
		var error =
			code == 309
				? RecordedMarketDepth.MaxRequestsError
				: RecordedMarketDepth.NotSubscribedError;
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "10" ? [WithRequestId(error, 2, f[2])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var thrown = await Assert.ThrowsAsync<IbkrException>(async () =>
		{
			await foreach (var _ in client.SubscribeMarketDepthAsync(Aapl, 5, true, Ct)) { }
		});

		Assert.Equal(code, thrown.Code);
		// The stream's cleanup has run by now; IB answers a cancel of a rejected request with 310
		Assert.DoesNotContain(server.Received, x => x[0] == "11");
	}

	[Fact]
	public async Task Depth_exchanges_come_from_the_reply_without_a_request_id()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "82" ? [RecordedMarketDepth.Exchanges] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var exchanges = await client.GetMarketDepthExchangesAsync(Ct);

		Assert.Contains(server.Received, x => x.SequenceEqual(["82"]));
		Assert.Equal(4, exchanges.Count);
		Assert.Equal(new MarketDepthExchange("ISLAND", "STK", "", "Deep2", null), exchanges[1]);
		Assert.Equal(new MarketDepthExchange("SMART", "STK", "PINK", "AggDeep", 1), exchanges[2]);
		Assert.Equal(4, exchanges[3].AggregatedGroup);
	}
}
