using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// Historical ticks requests go out as recorded (docs/protocol.md, "Historical ticks"); replies are
/// recorded frames.
/// </summary>
public class IbkrClientHistoricalTicksTests
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

	private static readonly DateTimeOffset End = new(2026, 10, 28, 14, 54, 44, TimeSpan.Zero);

	private static DateTimeOffset At(long unixSeconds) =>
		DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

	private static string[] WithRequestId(string[] frame, int index, string id)
	{
		var copy = (string[])frame.Clone();
		copy[index] = id;
		return copy;
	}

	private static FakeTwsServer Serve(string[] reply, int idIndex = 1, int version = 187) =>
		new(version) { Respond = f => f[0] == "96" ? [WithRequestId(reply, idIndex, f[1])] : [] };

	[Theory]
	[InlineData(187)]
	[InlineData(178)]
	public async Task Trades_request_matches_the_recording_and_reads_odd_lots_as_unreported(
		int version
	)
	{
		await using var server = Serve(RecordedHistoricalTicks.Trades, version: version);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var ticks = await client.GetHistoricalTicksAsync(
			Aapl,
			HistoricalTickType.Trades,
			5,
			end: End,
			cancellationToken: Ct
		);

		var sent = server.Received.Single(x => x[0] == "96");
		Assert.Equal(WithRequestId(RecordedHistoricalTicks.TradesRequest, 1, sent[1]), sent);
		Assert.Equal(
			[
				new TickByTick(TickByTickType.AllLast, At(1793199224))
				{
					Price = 345.51,
					Size = 40,
					Exchange = "FINRA",
					IsHistorical = true,
				},
				new TickByTick(TickByTickType.AllLast, At(1793199224))
				{
					Price = 345.51,
					Size = 1,
					Exchange = "FINRA",
					SpecialConditions = "I",
					Unreported = true,
					IsHistorical = true,
				},
			],
			ticks
		);
	}

	[Fact]
	public async Task A_start_goes_in_the_start_field_with_regular_hours_and_ignore_size()
	{
		await using var server = Serve(RecordedHistoricalTicks.BidAsk);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var ticks = await client.GetHistoricalTicksAsync(
			Aapl,
			HistoricalTickType.BidAsk,
			1000,
			start: new DateTimeOffset(2026, 10, 28, 9, 30, 0, TimeSpan.FromHours(-4)),
			useRegularTradingHours: true,
			ignoreSize: true,
			cancellationToken: Ct
		);

		var sent = server.Received.Single(x => x[0] == "96");
		Assert.Equal(["20261028 13:30:00 UTC", "", "1000", "BID_ASK", "1", "1", ""], sent[15..]);
		Assert.Equal(
			new TickByTick(TickByTickType.BidAsk, At(1793199278))
			{
				BidPrice = 345.25,
				AskPrice = 345.29,
				BidSize = 120,
				AskSize = 400,
				IsHistorical = true,
			},
			ticks[1]
		);
	}

	[Fact]
	public async Task Midpoints_decode_to_midpoint_ticks()
	{
		await using var server = Serve(RecordedHistoricalTicks.MidPoint);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var ticks = await client.GetHistoricalTicksAsync(
			Aapl,
			HistoricalTickType.MidPoint,
			5,
			end: End,
			cancellationToken: Ct
		);

		Assert.Equal("MIDPOINT", server.Received.Single(x => x[0] == "96")[18]);
		Assert.Equal([345.27, 345.265], ticks.Select(x => x.Price));
		Assert.All(ticks, x => Assert.Equal(TickByTickType.MidPoint, x.Type));
	}

	[Fact]
	public async Task A_refusal_throws_with_its_code()
	{
		await using var server = Serve(RecordedHistoricalTicks.MaxLengthError, idIndex: 2);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var error = await Assert.ThrowsAsync<IbkrException>(() =>
			client.GetHistoricalTicksAsync(
				Aapl,
				HistoricalTickType.Trades,
				1000,
				end: End,
				cancellationToken: Ct
			)
		);

		Assert.Equal(10187, error.Code);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1001)]
	public async Task More_than_a_thousand_or_no_ticks_is_refused_before_asking_ib(int count)
	{
		await using var server = Serve(RecordedHistoricalTicks.Trades);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
			client.GetHistoricalTicksAsync(
				Aapl,
				HistoricalTickType.Trades,
				count,
				end: End,
				cancellationToken: Ct
			)
		);
		Assert.DoesNotContain(server.Received, x => x[0] == "96");
	}
}
