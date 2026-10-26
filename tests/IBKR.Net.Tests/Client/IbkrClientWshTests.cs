using IBKR.Net.Tests.Fakes;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// Wall Street Horizon requests go out as recorded (docs/protocol.md, "Wall Street Horizon"). The
/// 10276 refusal is recorded; the data replies (104 / 105) are not, so those frames are made up
/// in the layout described there.
/// </summary>
public class IbkrClientWshTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private static string[] WithRequestId(string[] frame, int index, string id)
	{
		var copy = (string[])frame.Clone();
		copy[index] = id;
		return copy;
	}

	[Fact]
	public async Task Metadata_without_the_subscription_fails_with_10276()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "100" ? [WithRequestId(RecordedWsh.NotAllowed, 2, f[1])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var error = await Assert.ThrowsAsync<IbkrException>(() => client.GetWshMetaDataAsync(Ct));

		Assert.Equal(10276, error.Code);
		Assert.Equal("News feed is not allowed.", error.ErrorText);
		var sent = server.Received.Single(x => x[0] == "100");
		Assert.Equal(WithRequestId(RecordedWsh.MetaDataRequest, 1, sent[1]), sent);
		Assert.DoesNotContain(server.Received, x => x[0] == "101");
	}

	[Fact]
	public async Task Metadata_returns_the_json_of_the_reply()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "100"
					?
					[
						["104", f[1], "{\"meta_data\":{}}"],
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		Assert.Equal("{\"meta_data\":{}}", await client.GetWshMetaDataAsync(Ct));
	}

	[Fact]
	public async Task Event_data_by_contract_goes_out_as_recorded()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "102"
					?
					[
						["105", f[1], "[{\"event_type\":\"wshe_ed\"}]"],
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var json = await client.GetWshEventDataAsync(
			new WshEventDataRequest
			{
				ConId = 265598,
				StartDate = new DateOnly(2026, 10, 1),
				EndDate = new DateOnly(2026, 12, 31),
				TotalLimit = 50,
			},
			Ct
		);

		Assert.Equal("[{\"event_type\":\"wshe_ed\"}]", json);
		var sent = server.Received.Single(x => x[0] == "102");
		Assert.Equal(WithRequestId(RecordedWsh.EventDataByConIdRequest, 1, sent[1]), sent);
	}

	[Fact]
	public async Task Event_data_by_filter_leaves_contract_and_limit_unset_as_recorded()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "102"
					?
					[
						["105", f[1], "[]"],
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		await client.GetWshEventDataAsync(
			new WshEventDataRequest
			{
				Filter = RecordedWsh.EventDataByFilterRequest[3],
				StartDate = new DateOnly(2026, 10, 26),
				EndDate = new DateOnly(2026, 11, 2),
				TotalLimit = 100,
			},
			Ct
		);
		await client.GetWshEventDataAsync(
			new WshEventDataRequest { Filter = RecordedWsh.EventDataUnsetRequest[3] },
			Ct
		);

		var sent = server.Received.Where(x => x[0] == "102").ToArray();
		Assert.Equal(WithRequestId(RecordedWsh.EventDataByFilterRequest, 1, sent[0][1]), sent[0]);
		Assert.Equal(WithRequestId(RecordedWsh.EventDataUnsetRequest, 1, sent[1][1]), sent[1]);
	}

	[Fact]
	public async Task Event_data_without_the_subscription_fails_and_is_not_cancelled()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f => f[0] == "102" ? [WithRequestId(RecordedWsh.NotAllowed, 2, f[1])] : [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var error = await Assert.ThrowsAsync<IbkrException>(() =>
			client.GetWshEventDataAsync(new WshEventDataRequest { ConId = 265598 }, Ct)
		);

		Assert.Equal(10276, error.Code);
		Assert.DoesNotContain(server.Received, x => x[0] == "103");
	}

	[Fact]
	public async Task Giving_up_on_event_data_cancels_it_at_IB()
	{
		await using var server = new FakeTwsServer();
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);
		using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);

		var request = client.GetWshEventDataAsync(
			new WshEventDataRequest { ConId = 265598 },
			cancel.Token
		);
		await server.WaitForFramesAsync(2);
		await cancel.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
		var frames = await server.WaitForFramesAsync(3);
		var sent = frames.Single(x => x[0] == "102");
		Assert.Equal(["103", sent[1]], frames.Single(x => x[0] == "103"));
	}
}
