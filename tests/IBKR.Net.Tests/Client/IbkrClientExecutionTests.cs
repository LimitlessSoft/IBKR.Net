using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

public class IbkrClientExecutionTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static string[] WithRequestId(string[] frame, int index, string id)
	{
		string[] copy = [.. frame];
		copy[index] = id;
		return copy;
	}

	// Answers reqExecutions with the recorded fills under the request's id
	private static FakeTwsServer Server(Func<string, IEnumerable<string[]>> reply) =>
		new() { Respond = f => f[0] == "7" ? reply(f[2]) : [] };

	private static IEnumerable<string[]> Recorded(string id) =>
		[
			WithRequestId(RecordedExecutions.Buy, 1, id),
			WithRequestId(RecordedExecutions.Sell, 1, id),
			RecordedExecutions.BuyCommission,
			RecordedExecutions.SellCommission,
			WithRequestId(RecordedExecutions.End, 2, id),
		];

	[Fact]
	public async Task Executions_ask_without_filters_and_carry_their_commissions()
	{
		await using var server = Server(Recorded);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var executions = await client.GetExecutionsAsync(Ct);

		var sent = (await server.WaitForFramesAsync(2))[1];
		Assert.Equal(WithRequestId(RecordedExecutions.Request, 2, sent[2]), sent);
		Assert.Equal(2, executions.Count);
		Assert.Equal(1.04028, executions[0].Commission?.Commission);
		Assert.Null(executions[0].Commission?.RealizedPnl);
		Assert.Equal(14.916573, executions[1].Commission?.RealizedPnl);
	}

	[Fact]
	public async Task A_fill_without_a_commission_report_has_none()
	{
		await using var server = Server(id =>
			[
				WithRequestId(RecordedExecutions.Buy, 1, id),
				WithRequestId(RecordedExecutions.End, 2, id),
			]
		);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		Assert.Null(Assert.Single(await client.GetExecutionsAsync(Ct)).Commission);
	}

	[Fact]
	public async Task No_fills_is_an_empty_list()
	{
		await using var server = Server(id => [WithRequestId(RecordedExecutions.End, 2, id)]);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		Assert.Empty(await client.GetExecutionsAsync(Ct));
	}

	[Fact]
	public async Task Concurrent_requests_each_get_their_own_fills()
	{
		await using var server = Server(Recorded);
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var results = await Task.WhenAll(
			client.GetExecutionsAsync(Ct),
			client.GetExecutionsAsync(Ct)
		);

		Assert.All(
			results,
			executions => Assert.All(executions, execution => Assert.NotNull(execution.Commission))
		);
		Assert.All(results, executions => Assert.Equal(2, executions.Count));
	}

	[Fact]
	public async Task Executions_fail_when_the_connection_drops()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		var request = client.GetExecutionsAsync(Ct);
		await server.WaitForFramesAsync(2);

		server.Drop();

		await Assert.ThrowsAsync<IOException>(() => request);
	}
}
