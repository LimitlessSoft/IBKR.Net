using System.Globalization;

namespace IBKR.Net.Protocol;

/// <summary>
/// placeOrder at <see cref="ProtocolVersions.Orders"/>: the 114-field frame as observed on the
/// wire (docs/protocol.md, "Orders"). Fields located by diffing recorded variants are set from the
/// order; every other field is sent exactly as observed.
/// </summary>
internal static class OrderEncoder
{
	// [24..113] as observed for every variant, except [33] outside RTH and [100] cash quantity
	// csharpier-ignore
	private static readonly string[] ObservedTail =
	[
		"O", "0", "", "1", "0", "0", "0", "0", "0", "0", "0", "", "0.0", "", "", "", "", "", "",
		"0", "", "-1", "0", "", "", "0", "", "", "0", "0", "", "0", "", "", "", "", "", "0", "",
		"", "", "", "0", "", "", "", "", "", "", "", "", "", "", "0", "", "", "0", "0", "", "",
		"0", "", "0", "0", "0", "0", "", "", "", "", "", "", "0", "", "", "", "", "", "", "", "",
		"0", "0", "0", "0", "", "", "0", "", "",
	];

	private const int TailStart = 24;
	private const int OutsideRegularHoursIndex = 33;
	private const int CashQuantityIndex = 100;

	public static IReadOnlyList<string> PlaceOrder(int orderId, Contract contract, Order order)
	{
		var fields = new List<string>(TailStart + ObservedTail.Length)
		{
			OutgoingMessageIds.PlaceOrder,
			Format(orderId),
			Format(contract.ConId),
			contract.Symbol,
			contract.SecurityType,
			contract.LastTradeDateOrContractMonth,
			// Observed as 0.0 for a contract without a strike
			contract.Strike
				is { } strike
				? Format(strike)
				: "0.0",
			contract.Right,
			contract.Multiplier,
			contract.Exchange,
			contract.PrimaryExchange,
			contract.Currency,
			contract.LocalSymbol,
			contract.TradingClass,
			string.Empty, // security id type
			string.Empty, // security id
			order.Action,
			order.Quantity.ToString(CultureInfo.InvariantCulture),
			order.OrderType,
			Format(order.LimitPrice),
			Format(order.AuxPrice),
			order.TimeInForce,
			string.Empty, // [22] not located yet; observed empty
			order.Account,
		};
		fields.AddRange(ObservedTail);
		fields[OutsideRegularHoursIndex] = order.OutsideRegularHours ? "1" : "0";
		fields[CashQuantityIndex] = Format(order.CashQuantity);
		return fields;
	}

	/// <summary>4: [1] 1, [2] order id, [3] manual cancel time (empty).</summary>
	public static IReadOnlyList<string> CancelOrder(int orderId) =>
		[OutgoingMessageIds.CancelOrder, "1", Format(orderId), string.Empty];

	private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

	private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

	private static string Format(double? value) => value is { } v ? Format(v) : string.Empty;
}
