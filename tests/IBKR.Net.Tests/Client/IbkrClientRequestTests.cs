using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// Each request must go out exactly as recorded from IB's own client (docs/protocol.md), with our
/// request id in place of the recorded one; replies are the recorded frames.
/// </summary>
public class IbkrClientRequestTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static readonly Contract Spy = new() { ConId = 756733, Exchange = "SMART" };

	// Recorded replies carry the recorded request id; re-address them to the one we sent
	private static string[] WithRequestId(string[] frame, int index, string id)
	{
		var copy = (string[])frame.Clone();
		copy[index] = id;
		return copy;
	}

	[Fact]
	public async Task Session_start_exposes_the_managed_accounts()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		Assert.Equal(["DU0000001", "DU0000002"], client.ManagedAccounts);
	}

	[Fact]
	public async Task Contract_details_request_matches_the_recording_and_collects_until_end()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "9"
					? [WithRequestId(Recorded.ContractDataStock, 1, f[2]), ["52", "1", f[2]]]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var details = await client.GetContractDetailsAsync(
			new Contract
			{
				Symbol = "SPY",
				SecurityType = "STK",
				Exchange = "SMART",
				Currency = "USD",
			},
			Ct
		);

		Assert.Equal(756733, Assert.Single(details).Contract.ConId);
		var sent = server.Received.Single(x => x[0] == "9");
		Assert.Equal(
			[
				"9",
				"8",
				sent[2],
				"0",
				"SPY",
				"STK",
				"",
				"",
				"",
				"",
				"SMART",
				"",
				"USD",
				"",
				"",
				"0",
				"",
				"",
				"",
			],
			sent
		);
	}

	[Fact]
	public async Task A_request_error_fails_the_call()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "9" ? [WithRequestId(Recorded.NoSecurityDefinition, 2, f[2])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var error = await Assert.ThrowsAsync<IbkrException>(() =>
			client.GetContractDetailsAsync(new Contract { Symbol = "NOSUCHXYZ" }, Ct)
		);

		Assert.Equal(200, error.Code);
	}

	[Fact]
	public async Task Notices_are_raised_not_thrown()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var notice = new TaskCompletionSource<IbkrNotice>();
		client.Notice += x => notice.TrySetResult(x);

		await server.SendAsync("4", "2", "-1", "2106", "HMDS data farm connection is OK", "");

		Assert.Equal(2106, (await notice.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct)).Code);
	}

	[Fact]
	public async Task Option_parameters_request_matches_the_recording()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "78"
					? [WithRequestId(Recorded.SecDefOptParams, 1, f[1]), ["76", f[1]]]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var parameters = await client.GetOptionChainParametersAsync(
			"SPY",
			"STK",
			756733,
			cancellationToken: Ct
		);

		Assert.Equal("SMART", Assert.Single(parameters).Exchange);
		var sent = server.Received.Single(x => x[0] == "78");
		Assert.Equal(["78", sent[1], "SPY", "", "STK", "756733"], sent);
	}

	[Fact]
	public async Task Historical_request_matches_the_recording_and_ends_with_its_one_frame()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "20" ? [WithRequestId(Recorded.HistoricalData, 1, f[1])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var bars = await client.GetHistoricalBarsAsync(Spy, "1 D", "5 mins", cancellationToken: Ct);

		Assert.Equal(2, bars.Count);
		var sent = server.Received.Single(x => x[0] == "20");
		Assert.Equal(
			[
				"20",
				sent[1],
				"756733",
				"",
				"",
				"",
				"",
				"",
				"",
				"SMART",
				"",
				"",
				"",
				"",
				"0",
				"",
				"5 mins",
				"1 D",
				"1",
				"TRADES",
				"2",
				"0",
				"",
			],
			sent
		);
	}

	[Fact]
	public async Task Market_data_streams_ticks_and_cancels_when_reading_stops()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "1"
					? Recorded.MarketData.Select(x => WithRequestId(x, x[0] is "81" ? 1 : 2, f[2]))
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var ticks = new List<MarketDataTick>();
		await foreach (var tick in client.SubscribeMarketDataAsync(Spy, cancellationToken: Ct))
		{
			ticks.Add(tick);
			if (ticks.Count == 4)
				break;
		}

		Assert.Equal([1, 9, 8, 49], ticks.Select(x => x.Field));
		var sent = server.Received.Single(x => x[0] == "1");
		Assert.Equal(
			[
				"1",
				"11",
				sent[2],
				"756733",
				"",
				"",
				"",
				"",
				"",
				"",
				"SMART",
				"",
				"",
				"",
				"",
				"0",
				"",
				"0",
				"0",
				"",
			],
			sent
		);
		var cancel = (await server.WaitForFramesAsync(3)).Single(x => x[0] == "2");
		Assert.Equal(["2", "1", sent[2]], cancel);
	}

	[Fact]
	public async Task Positions_collect_until_end_then_cancel()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "61" ? [Recorded.Position, ["62", "1"]] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var positions = await client.GetPositionsAsync(Ct);

		Assert.Equal("DU0000001", Assert.Single(positions).Account);
		var frames = await server.WaitForFramesAsync(3);
		Assert.Equal(["61", "1"], frames[1]);
		Assert.Equal(["64", "1"], frames[2]);
	}

	[Fact]
	public async Task Account_summary_streams_values_and_cancels()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "62"
					? [WithRequestId(Recorded.AccountSummary, 2, f[2]), ["64", "1", f[2]]]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		AccountValue? first = null;
		await foreach (
			var value in client.SubscribeAccountSummaryAsync(
				["NetLiquidation"],
				cancellationToken: Ct
			)
		)
		{
			first = value;
			break;
		}

		Assert.Equal("NetLiquidation", first!.Tag);
		var sent = server.Received.Single(x => x[0] == "62");
		Assert.Equal(["62", "1", sent[2], "All", "NetLiquidation"], sent);
		var cancel = (await server.WaitForFramesAsync(3)).Single(x => x[0] == "63");
		Assert.Equal(["63", "1", sent[2]], cancel);
	}

	[Fact]
	public async Task Pnl_requests_match_the_recording()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] switch
				{
					"92" => [WithRequestId(Recorded.Pnl, 1, f[1])],
					"94" => [WithRequestId(Recorded.PnlSingle, 1, f[1])],
					_ => [],
				},
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		await foreach (var _ in client.SubscribeAccountPnlAsync("DU0000001", Ct))
			break;
		await foreach (var _ in client.SubscribePositionPnlAsync("DU0000001", 920268479, Ct))
			break;

		var frames = await server.WaitForFramesAsync(5);
		var pnl = frames.Single(x => x[0] == "92");
		var single = frames.Single(x => x[0] == "94");
		Assert.Equal(["92", pnl[1], "DU0000001", ""], pnl);
		Assert.Equal(["94", single[1], "DU0000001", "", "920268479"], single);
		Assert.Contains(frames, x => x.SequenceEqual(["93", pnl[1]]));
		Assert.Contains(frames, x => x.SequenceEqual(["95", single[1]]));
	}

	[Fact]
	public async Task Market_data_type_request_matches_the_recording()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		await client.SetMarketDataTypeAsync(MarketDataType.Delayed, Ct);

		Assert.Equal(["59", "1", "3"], (await server.WaitForFramesAsync(2))[1]);
	}

	[Fact]
	public async Task Realtime_bars_request_matches_the_recording_and_cancels()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "50" ? [WithRequestId(Recorded.RealTimeBarMidpoint, 2, f[2])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		Bar? first = null;
		await foreach (
			var bar in client.SubscribeRealtimeBarsAsync(
				new Contract { ConId = 479624278, Exchange = "PAXOS" },
				"MIDPOINT",
				useRegularTradingHours: false,
				Ct
			)
		)
		{
			first = bar;
			break;
		}

		Assert.Equal(80354.625, first!.Close);
		var sent = server.Received.Single(x => x[0] == "50");
		Assert.Equal(
			[
				"50",
				"3",
				sent[2],
				"479624278",
				"",
				"",
				"",
				"",
				"",
				"",
				"PAXOS",
				"",
				"",
				"",
				"",
				"5",
				"MIDPOINT",
				"0",
				"",
			],
			sent
		);
		var cancel = (await server.WaitForFramesAsync(3)).Single(x => x[0] == "51");
		Assert.Equal(["51", "1", sent[2]], cancel);
	}
}
