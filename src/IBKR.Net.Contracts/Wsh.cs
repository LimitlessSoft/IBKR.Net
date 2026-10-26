namespace IBKR.Net;

/// <summary>
/// What a Wall Street Horizon event data request asks for (reqWshEventData). Either a contract
/// (<see cref="ConId"/>) or a JSON <see cref="Filter"/> in WSH's format (e.g.
/// <c>{"country":"All","limit":100,"wshe_ed":"true"}</c>; the event type tags come from the
/// metadata, <c>IbkrClient.GetWshMetaDataAsync</c>). Null leaves a field unset.
/// Needs IB's "Wall Street Horizon Corporate Event Data" subscription; without it IB fails the
/// request with 10276 "News feed is not allowed."
/// </summary>
public sealed record WshEventDataRequest
{
	/// <summary>One contract's events; leave null when <see cref="Filter"/> is used.</summary>
	public int? ConId { get; init; }

	/// <summary>WSH's JSON filter; leave null when <see cref="ConId"/> is used.</summary>
	public string? Filter { get; init; }

	/// <summary>Add the events of the account's TWS watchlist contracts.</summary>
	public bool FillWatchlist { get; init; }

	/// <summary>Add the events of the account's positions.</summary>
	public bool FillPortfolio { get; init; }

	/// <summary>Add the events of the contracts' competitors.</summary>
	public bool FillCompetitors { get; init; }

	/// <summary>First day of events (inclusive), sent as <c>yyyyMMdd</c>.</summary>
	public DateOnly? StartDate { get; init; }

	/// <summary>Last day of events, sent as <c>yyyyMMdd</c>.</summary>
	public DateOnly? EndDate { get; init; }

	/// <summary>The most events to return.</summary>
	public int? TotalLimit { get; init; }
}
