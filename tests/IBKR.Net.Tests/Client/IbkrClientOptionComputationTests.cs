using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// Option greeks (21) and open interest (size ticks 27 / 28 with generic tick 101) on a market
/// data stream (docs/protocol.md, "Option greeks and open interest").
/// </summary>
public class IbkrClientOptionComputationTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static readonly Contract SpyPut = new()
	{
		Symbol = "SPY",
		SecurityType = "OPT",
		LastTradeDateOrContractMonth = "20261030",
		Strike = 755,
		Right = "P",
		Multiplier = "100",
		Exchange = "SMART",
		Currency = "USD",
		TradingClass = "SPY",
	};

	private static string[] WithRequestId(string[] frame, string id)
	{
		var copy = (string[])frame.Clone();
		copy[frame[0] is "81" or "21" ? 1 : 2] = id;
		return copy;
	}

	private static async Task<List<MarketDataTick>> StreamAsync(FakeTwsServer server, int count)
	{
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var ticks = new List<MarketDataTick>();
		await foreach (var tick in client.SubscribeMarketDataAsync(SpyPut, "101", Ct))
		{
			ticks.Add(tick);
			if (ticks.Count == count)
				break;
		}
		return ticks;
	}

	private static FakeTwsServer Server() =>
		new()
		{
			Respond = f =>
				f[0] == "1"
					? RecordedOptionComputation.Replies.Select(x => WithRequestId(x, f[2]))
					: [],
		};

	[Fact]
	public async Task Request_goes_out_as_recorded()
	{
		await using var server = Server();

		await StreamAsync(server, 1);

		var sent = server.Received.Single(x => x[0] == "1");
		Assert.Equal(WithRequestId(RecordedOptionComputation.Request, sent[2]), sent);
	}

	[Fact]
	public async Task Model_greeks_and_open_interest_are_decoded()
	{
		await using var server = Server();

		var ticks = await StreamAsync(server, 5);

		Assert.Equal([1, 27, 28, 13, 10], ticks.Select(x => x.Field));
		Assert.Equal(41057m, ticks.Single(x => x.Field == 28).Size);
		Assert.Equal(0m, ticks.Single(x => x.Field == 27).Size);
		var model = ticks.Single(x => x.Field == 13).Option;
		Assert.NotNull(model);
		Assert.Equal(0.25652007640268776, model.ImpliedVolatility);
		Assert.Equal(-0.46693286118129407, model.Delta);
		Assert.Equal(1.2843018132440893, model.OptionPrice);
		Assert.Equal(0.0, model.PresentValueDividend);
		Assert.Equal(0.11197500273737811, model.Gamma);
		Assert.Equal(0.05522320195516195, model.Vega);
		Assert.Equal(-1.2843018132440893, model.Theta);
		Assert.Equal(755.3499145507812, model.UnderlyingPrice);
	}

	[Fact]
	public async Task Values_IB_did_not_compute_are_null()
	{
		await using var server = Server();

		var ticks = await StreamAsync(server, 5);

		var bid = ticks.Single(x => x.Field == 10).Option;
		Assert.NotNull(bid);
		Assert.Null(bid.ImpliedVolatility);
		Assert.Null(bid.Delta);
		Assert.Null(bid.Gamma);
		Assert.Null(bid.Vega);
		Assert.Null(bid.Theta);
		Assert.Equal(1.5700000524520874, bid.OptionPrice);
		Assert.Equal(755.28, bid.UnderlyingPrice);
	}
}
