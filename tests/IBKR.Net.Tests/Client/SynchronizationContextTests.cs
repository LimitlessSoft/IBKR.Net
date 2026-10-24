using IBKR.Net.Tests.Fakes;

namespace IBKR.Net.Tests.Client;

/// <summary>
/// A UI app may block on the client from a thread whose synchronization context no longer runs
/// anything (e.g. disconnecting after its dispatcher stopped). The library must not resume there.
/// </summary>
public class SynchronizationContextTests
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Dispose_finishes_on_a_context_that_never_runs_continuations()
	{
		await using var server = new FakeTwsServer();
		var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var finished = OnDeadContext(() => client.DisposeAsync().AsTask());

		Assert.True(finished);
	}

	[Fact]
	public async Task A_request_finishes_on_a_context_that_never_runs_continuations()
	{
		await using var server = new FakeTwsServer
		{
			Respond = f =>
				f[0] == "49"
					?
					[
						["49", "1", "1792800000"],
					]
					: [],
		};
		await using var client = await IbkrClient.ConnectAsync(server.Options(), Ct);

		var finished = OnDeadContext(() => client.GetServerTimeAsync(Ct));

		Assert.True(finished);
	}

	// Starts the work with a dead context current and blocks on it, as a stopped UI thread would
	private static bool OnDeadContext(Func<Task> work)
	{
		var previous = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(new DeadContext());
		try
		{
			return work().Wait(TimeSpan.FromSeconds(5));
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previous);
		}
	}

	private sealed class DeadContext : SynchronizationContext
	{
		public override void Post(SendOrPostCallback d, object? state) { }
	}
}
