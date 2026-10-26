using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>News requests go out as recorded (docs/protocol.md, "News"); replies are recorded frames.</summary>
public class IbkrClientNewsTests
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
		copy[1] = id;
		return copy;
	}

	private static string[] WithRequestId(string[] frame, int index, string id)
	{
		var copy = (string[])frame.Clone();
		copy[index] = id;
		return copy;
	}

	[Fact]
	public async Task Providers_come_from_the_reply_without_a_request_id()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "85" ? [RecordedNews.Providers] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var providers = await client.GetNewsProvidersAsync(Ct);

		Assert.Contains(server.Received, x => x.SequenceEqual(["85"]));
		Assert.Equal(["BRFG", "BRFUPDN", "DJNL"], providers.Select(x => x.Code));
		Assert.Equal("Briefing.com Analyst Actions", providers[1].Name);
	}

	[Fact]
	public async Task Historical_news_request_matches_the_recording_and_splits_the_metadata()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "86" ? RecordedNews.HistoricalNews.Select(x => WithRequestId(x, f[1])) : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var news = await client.GetHistoricalNewsAsync(
			265598,
			["BRFG", "BRFUPDN", "DJ-N"],
			3,
			cancellationToken: Ct
		);

		var sent = server.Received.Single(x => x[0] == "86");
		Assert.Equal(["86", sent[1], "265598", "BRFG+BRFUPDN+DJ-N", "", "", "3", ""], sent);
		Assert.True(news.HasMore);
		Assert.Equal(2, news.Headlines.Count);
		var first = news.Headlines[0];
		Assert.Equal(
			new NewsHeadline(
				new DateTimeOffset(2026, 10, 26, 13, 3, 0, TimeSpan.Zero),
				"DJ-N",
				"DJ-N$1fa3b9b6",
				"* Apple Unveils All-New iPad Mini >AAPL",
				"A:800015:L:en"
			),
			first
		);
	}

	[Fact]
	public async Task Historical_news_before_goes_out_in_the_recorded_format()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "86"
					?
					[
						["87", f[1], "0"],
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var news = await client.GetHistoricalNewsAsync(
			265598,
			["BRFG", "BRFUPDN"],
			5,
			new DateTimeOffset(2026, 10, 1, 2, 0, 0, TimeSpan.FromHours(2)),
			Ct
		);

		var sent = server.Received.Single(x => x[0] == "86");
		Assert.Equal(
			["86", sent[1], "265598", "BRFG+BRFUPDN", "2026-10-01 00:00:00.0", "", "5", ""],
			sent
		);
		Assert.False(news.HasMore);
		Assert.Empty(news.Headlines);
	}

	[Fact]
	public async Task Article_request_matches_the_recording()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "84" ? [WithRequestId(RecordedNews.Article, f[1])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var article = await client.GetNewsArticleAsync("BRFG", "BRFG$1f064106", Ct);

		var sent = server.Received.Single(x => x[0] == "84");
		Assert.Equal(["84", sent[1], "BRFG", "BRFG$1f064106", ""], sent);
		Assert.Equal(NewsArticleType.Text, article.Type);
		Assert.StartsWith("<html><body><p><strong>Apple", article.Text);
	}

	[Fact]
	public async Task Contract_news_streams_headlines_and_cancels_when_reading_stops()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "1"
					? RecordedNews.ContractNewsTicks.Select(x => WithRequestId(x, f[2]))
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var headlines = new List<NewsHeadline>();
		await foreach (
			var headline in client.SubscribeNewsAsync(
				Aapl,
				["BRFG", "BRFUPDN", "DJ-N", "DJ-RT", "DJNL"],
				Ct
			)
		)
		{
			headlines.Add(headline);
			if (headlines.Count == 2)
				break;
		}

		var sent = server.Received.Single(x => x[0] == "1");
		Assert.Equal(WithRequestId(RecordedNews.ContractNewsRequest, 2, sent[2]), sent);
		Assert.Equal(["DJ-N$1fa3b986", "DJ-N$1fa3b9b6"], headlines.Select(x => x.ArticleId));
		Assert.Equal(new DateTimeOffset(2026, 10, 26, 13, 3, 0, TimeSpan.Zero), headlines[0].Time);
		Assert.Equal("A:800015:L:en", headlines[0].ExtraData);
		var cancel = (await server.WaitForFramesAsync(3)).Single(x => x[0] == "2");
		Assert.Equal(["2", "1", sent[2]], cancel);
	}

	[Fact]
	public async Task Broad_tape_contracts_name_the_provider_as_exchange()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "9"
					? [WithRequestId(RecordedNews.BroadTapeDetails, f[2]), ["52", "1", f[2]]]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var contracts = await client.GetBroadTapeContractsAsync("BRFUPDN", Ct);

		var sent = server.Received.Single(x => x[0] == "9");
		Assert.Equal(
			[
				"9",
				"8",
				sent[2],
				"0",
				"",
				"NEWS",
				"",
				"",
				"",
				"",
				"BRFUPDN",
				"",
				"",
				"",
				"",
				"0",
				"",
				"",
				"",
			],
			sent
		);
		var tape = Assert.Single(contracts);
		Assert.Equal(
			(99384238, "BRFUPDN:BRF_ALL", "NEWS", "BRFUPDN"),
			(tape.ConId, tape.Symbol, tape.SecurityType, tape.Exchange)
		);
	}

	[Fact]
	public async Task Broad_tape_subscription_matches_the_recording()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "1" ? [WithRequestId(RecordedNews.BroadTapeTick, f[2])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var tape = new Contract
		{
			ConId = 100283238,
			Symbol = "BRFG:BRFG_ALL",
			SecurityType = "NEWS",
			Exchange = "BRFG",
		};

		NewsHeadline? first = null;
		await foreach (var headline in client.SubscribeNewsAsync(tape, cancellationToken: Ct))
		{
			first = headline;
			break;
		}

		var sent = server.Received.Single(x => x[0] == "1");
		Assert.Equal(WithRequestId(RecordedNews.BroadTapeRequest, 2, sent[2]), sent);
		Assert.Equal("Futures point to higher open", first!.Headline);
	}

	[Fact]
	public async Task A_rejected_broad_tape_fails_the_stream()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "1"
					?
					[
						[
							"4",
							"2",
							f[2],
							"321",
							"Error validating request.-'bL' : cause - The entered news source is invalid.",
						],
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var tape = new Contract
		{
			Symbol = "DJ-RT:DJ-RT_ALL",
			SecurityType = "NEWS",
			Exchange = "DJ-RT",
		};

		var error = await Assert.ThrowsAsync<IbkrException>(async () =>
		{
			await foreach (var _ in client.SubscribeNewsAsync(tape, cancellationToken: Ct)) { }
		});
		Assert.Equal(321, error.Code);
	}

	[Fact]
	public void A_history_headline_without_metadata_keeps_its_text()
	{
		var headline = IBKR.Net.Protocol.Decoders.HistoricalNews([
			"86",
			"1",
			"2026-10-26 13:03:00.0",
			"BRFG",
			"BRFG$1",
			"{Odd} title",
		]);
		var plain = IBKR.Net.Protocol.Decoders.HistoricalNews([
			"86",
			"1",
			"2026-10-26 13:03:00.0",
			"BRFG",
			"BRFG$2",
			"Plain title",
		]);

		Assert.Equal(("Odd", " title"), (headline.ExtraData, headline.Headline));
		Assert.Equal(("", "Plain title"), (plain.ExtraData, plain.Headline));
	}
}
