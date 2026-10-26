using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// Tick-by-tick requests go out as recorded (docs/protocol.md, "Tick-by-tick"); replies are
/// recorded frames.
/// </summary>
public class IbkrClientTickByTickTests
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

	private static DateTimeOffset At(long unixSeconds) =>
		DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

	private static string[] WithRequestId(string[] frame, int index, string id)
	{
		var copy = (string[])frame.Clone();
		copy[index] = id;
		return copy;
	}

	private static async Task<(FakeTwsServer Server, List<TickByTick> Ticks)> SubscribeAsync(
		IEnumerable<string[]> replies,
		int count,
		TickByTickType type,
		int historicalTicks = 0,
		bool ignoreSize = false,
		int serverVersion = 187
	)
	{
		var server = new FakeTwsServer(serverVersion)
		{
			Respond = f => f[0] == "97" ? replies.Select(x => WithRequestId(x, 1, f[1])) : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var ticks = new List<TickByTick>();
		await foreach (
			var tick in client.SubscribeTickByTickAsync(Aapl, type, historicalTicks, ignoreSize, Ct)
		)
		{
			ticks.Add(tick);
			if (ticks.Count == count)
				break;
		}
		await server.WaitForFramesAsync(3);
		return (server, ticks);
	}

	[Theory]
	[InlineData(187)]
	[InlineData(178)]
	public async Task Last_trades_match_the_recording_and_cancel_when_reading_stops(int version)
	{
		var (server, ticks) = await SubscribeAsync(
			RecordedTickByTick.Last,
			2,
			TickByTickType.Last,
			serverVersion: version
		);
		await using var _ = server;

		var sent = server.Received.Single(x => x[0] == "97");
		Assert.Equal(WithRequestId(RecordedTickByTick.LastRequest, 1, sent[1]), sent);
		Assert.Equal(
			new TickByTick(TickByTickType.Last, At(1793043156))
			{
				Price = 343.85,
				Size = 58,
				Exchange = "NYSE",
			},
			ticks[0]
		);
		Assert.Equal("F", ticks[1].SpecialConditions);
		var cancel = server.Received.Single(x => x[0] == "98");
		Assert.Equal(WithRequestId(RecordedTickByTick.Cancel, 1, sent[1]), cancel);
	}

	[Fact]
	public async Task All_last_reads_odd_lots_as_unreported_and_crypto_without_an_exchange()
	{
		var (server, ticks) = await SubscribeAsync(
			RecordedTickByTick.AllLast,
			2,
			TickByTickType.AllLast
		);
		await using var _ = server;

		Assert.Equal("AllLast", server.Received.Single(x => x[0] == "97")[14]);
		Assert.Equal(
			new TickByTick(TickByTickType.AllLast, At(1793043161))
			{
				Price = 343.87,
				Size = 1,
				Exchange = "FINRA",
				SpecialConditions = "I",
				Unreported = true,
			},
			ticks[0]
		);
		Assert.Equal(
			new TickByTick(TickByTickType.AllLast, At(1793043187)) { Price = 79853.50, Size = 1 },
			ticks[1]
		);
	}

	[Fact]
	public async Task Bid_ask_and_midpoint_ticks_carry_their_own_fields()
	{
		var (server, ticks) = await SubscribeAsync(
			[.. RecordedTickByTick.BidAsk, RecordedTickByTick.MidPoint],
			3,
			TickByTickType.BidAsk,
			ignoreSize: true
		);
		await using var _ = server;

		var sent = server.Received.Single(x => x[0] == "97");
		Assert.Equal(["BidAsk", "0", "1"], sent[14..]);
		Assert.Equal(
			new TickByTick(TickByTickType.BidAsk, At(1793043165))
			{
				BidPrice = 343.84,
				AskPrice = 343.87,
				BidSize = 240,
				AskSize = 280,
			},
			ticks[0]
		);
		Assert.Equal(0.10016748m, ticks[1].BidSize);
		Assert.Equal(0.11602491m, ticks[1].AskSize);
		Assert.Equal(
			new TickByTick(TickByTickType.MidPoint, At(1793043169)) { Price = 343.88 },
			ticks[2]
		);
	}

	[Fact]
	public async Task History_comes_first_one_tick_per_entry_with_conditions_trimmed()
	{
		var (server, ticks) = await SubscribeAsync(
			[RecordedTickByTick.HistoricalLast, RecordedTickByTick.AllLast[0]],
			4,
			TickByTickType.AllLast,
			historicalTicks: 3
		);
		await using var _ = server;

		Assert.Equal("3", server.Received.Single(x => x[0] == "97")[15]);
		Assert.All(ticks[..3], x => Assert.True(x.IsHistorical));
		Assert.False(ticks[3].IsHistorical);
		Assert.Equal(
			new TickByTick(TickByTickType.AllLast, At(1793043312))
			{
				Price = 344.186,
				Size = 1,
				Exchange = "FINRA",
				SpecialConditions = "4 I",
				Unreported = true,
				IsHistorical = true,
			},
			ticks[1]
		);
		Assert.Equal("F I", ticks[2].SpecialConditions);
	}

	[Fact]
	public async Task Bid_ask_and_midpoint_history_decode_to_ticks()
	{
		var (server, ticks) = await SubscribeAsync(
			[RecordedTickByTick.HistoricalBidAsk, RecordedTickByTick.HistoricalMidPoint],
			5,
			TickByTickType.BidAsk,
			historicalTicks: 2
		);
		await using var _ = server;

		Assert.Equal(
			new TickByTick(TickByTickType.BidAsk, At(1793043307))
			{
				BidPrice = 344.32,
				AskPrice = 344.36,
				BidSize = 120,
				AskSize = 200,
				IsHistorical = true,
			},
			ticks[1]
		);
		Assert.Equal(
			new TickByTick(TickByTickType.MidPoint, At(1793043315))
			{
				Price = 79869.625,
				IsHistorical = true,
			},
			ticks[2]
		);
		Assert.Equal(79866.00, ticks[4].Price);
	}

	[Theory]
	[InlineData(10190, "max")]
	[InlineData(10189, "permission")]
	[InlineData(10189, "data")]
	public async Task A_rejected_request_fails_the_stream_without_a_cancel(int code, string kind)
	{
		var error = kind switch
		{
			"max" => RecordedTickByTick.MaxRequestsError,
			"permission" => RecordedTickByTick.NoPermissionError,
			_ => RecordedTickByTick.NoDataError,
		};
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "97" ? [WithRequestId(error, 2, f[1])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var thrown = await Assert.ThrowsAsync<IbkrException>(async () =>
		{
			await foreach (
				var _ in client.SubscribeTickByTickAsync(
					Aapl,
					TickByTickType.Last,
					cancellationToken: Ct
				)
			) { }
		});

		Assert.Equal(code, thrown.Code);
		// IB answers a cancel of a rejected request with 300
		Assert.DoesNotContain(server.Received, x => x[0] == "98");
	}
}
