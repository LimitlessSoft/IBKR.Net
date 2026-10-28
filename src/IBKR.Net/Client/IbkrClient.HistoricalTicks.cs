using System.Globalization;
using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	/// <summary>The most ticks one historical ticks request may ask for; IB refuses more (10187).</summary>
	public const int MaxHistoricalTicks = 1000;

	/// <summary>
	/// Past ticks of a contract (reqHistoricalTicks), oldest first, all marked
	/// <see cref="TickByTick.IsHistorical"/>. Give <paramref name="end"/> for the ticks strictly
	/// before it (paging back: the oldest tick's time as the next end, no overlap), or
	/// <paramref name="start"/> for those from it on; neither = up to now. IB sends at least
	/// <paramref name="numberOfTicks"/> (at most <see cref="MaxHistoricalTicks"/>) when there are
	/// that many, often more: it ends on a whole second. Times are whole seconds.
	/// <paramref name="ignoreSize"/> skips BidAsk ticks where only a size changed. Refusals throw
	/// <see cref="IbkrException"/>: 10187 (no market data permissions, too many ticks asked), 200
	/// (unknown contract).
	/// </summary>
	public Task<IReadOnlyList<TickByTick>> GetHistoricalTicksAsync(
		Contract contract,
		HistoricalTickType whatToShow,
		int numberOfTicks,
		DateTimeOffset? start = null,
		DateTimeOffset? end = null,
		bool useRegularTradingHours = false,
		bool ignoreSize = false,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(numberOfTicks, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(numberOfTicks, MaxHistoricalTicks);
		if (start == null && end == null)
			end = DateTimeOffset.UtcNow;
		var (endMessageId, whatToShowText) = whatToShow switch
		{
			HistoricalTickType.Trades => (IncomingMessageIds.HistoricalTicksLast, "TRADES"),
			HistoricalTickType.BidAsk => (IncomingMessageIds.HistoricalTicksBidAsk, "BID_ASK"),
			HistoricalTickType.MidPoint => (IncomingMessageIds.HistoricalTicksMidPoint, "MIDPOINT"),
			_ => throw new ArgumentOutOfRangeException(nameof(whatToShow)),
		};
		return CollectAsync(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestHistoricalTicks)
					.Add(id)
					.AddContract(contract)
					.Add(false) // include expired
					.Add(FormatTickTime(start))
					.Add(FormatTickTime(end))
					.Add(numberOfTicks)
					.Add(whatToShowText)
					.Add(useRegularTradingHours)
					.Add(ignoreSize)
					.Add(string.Empty), // misc options
			// Every recorded answer was one frame with the done flag set
			fields =>
				fields[0] switch
				{
					IncomingMessageIds.HistoricalTicksLast => Decoders.HistoricalTicksLast(
						fields,
						TickByTickType.AllLast
					),
					IncomingMessageIds.HistoricalTicksBidAsk => Decoders.HistoricalTicksBidAsk(
						fields
					),
					IncomingMessageIds.HistoricalTicksMidPoint => Decoders.HistoricalTicksMidPoint(
						fields
					),
					_ => null,
				},
			endMessageId,
			cancellationToken
		);
	}

	// "20261028 14:53:26 UTC"; empty for none
	private static string FormatTickTime(DateTimeOffset? time) =>
		time is { } value
			? value.UtcDateTime.ToString("yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"
			: string.Empty;
}
