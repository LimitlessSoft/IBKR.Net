using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// Market data snapshots (docs/protocol.md, "Market data snapshot"): the request is reqMktData with
/// the snapshot flag, the call completes on tickSnapshotEnd (57) with the ticks before it.
/// </summary>
public class IbkrClientSnapshotTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static readonly Contract Aapl = new()
	{
		ConId = 265598,
		Symbol = "AAPL",
		SecurityType = "STK",
		Exchange = "SMART",
		PrimaryExchange = "NASDAQ",
		Currency = "USD",
	};

	private static string[] WithRequestId(string[] frame, string id)
	{
		var copy = (string[])frame.Clone();
		copy[frame[0] is "81" ? 1 : 2] = id;
		return copy;
	}

	[Fact]
	public async Task Snapshot_goes_out_as_recorded_and_collects_the_ticks_until_the_end()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "1" ? RecordedSnapshot.Replies.Select(x => WithRequestId(x, f[2])) : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var ticks = await client.GetMarketDataSnapshotAsync(Aapl, Ct);

		Assert.Equal([1, 2, 4, 8, 9, 32, 33, 84, 45, 49], ticks.Select(x => x.Field));
		Assert.Equal(345.32, ticks.Single(x => x.Field == 4).Price);
		Assert.Equal(174925.427226m, ticks.Single(x => x.Field == 8).Size);
		var sent = server.Received.Single(x => x[0] == "1");
		Assert.Equal(WithRequestId(RecordedSnapshot.Request, sent[2]), sent);
		// Never the paid regulatory snapshot
		Assert.Equal("0", sent[18]);
		// Completed by IB: nothing to cancel
		Assert.DoesNotContain(server.Received, x => x[0] == "2");
	}

	[Fact]
	public async Task An_unsubscribed_listing_fails_with_the_first_code()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "1"
					? RecordedSnapshot.NotSubscribed.Select(x => WithRequestId(x, f[2]))
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var error = await Assert.ThrowsAsync<IbkrException>(() =>
			client.GetMarketDataSnapshotAsync(
				new Contract
				{
					Symbol = "VOW3",
					SecurityType = "STK",
					Exchange = "IBIS",
					Currency = "EUR",
				},
				Ct
			)
		);

		Assert.Equal(10090, error.Code);
		Assert.DoesNotContain(server.Received, x => x[0] == "2");
	}

	[Fact]
	public async Task Giving_up_before_the_end_cancels_the_request()
	{
		// The ticks but no end, as during IB's 11 seconds before it
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "1"
					? RecordedSnapshot.Replies.SkipLast(1).Select(x => WithRequestId(x, f[2]))
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		using var giveUp = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		giveUp.CancelAfter(TimeSpan.FromMilliseconds(300));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			client.GetMarketDataSnapshotAsync(Aapl, giveUp.Token)
		);

		var sent = server.Received.Single(x => x[0] == "1");
		var cancel = await WaitForAsync(server, x => x[0] == "2");
		Assert.Equal(["2", "1", sent[2]], cancel);
	}

	private static async Task<string[]> WaitForAsync(
		FakeTwsServer server,
		Func<string[], bool> match
	)
	{
		for (var i = 0; i < 100; i++)
		{
			if (server.Received.FirstOrDefault(match) is { } frame)
				return frame;
			await Task.Delay(20, Ct);
		}
		throw new TimeoutException("frame not received");
	}
}
