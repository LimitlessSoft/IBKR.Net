namespace IBKR.Net;

/// <summary>
/// What to trade (placeOrder). Set <see cref="Quantity"/>, or <see cref="CashQuantity"/> for an
/// amount of money (crypto buys, e.g. $10 of BTC).
/// </summary>
public sealed record Order
{
	/// <summary>BUY or SELL.</summary>
	public string Action { get; init; } = string.Empty;

	/// <summary>Units to trade; 0 when <see cref="CashQuantity"/> is set.</summary>
	public decimal Quantity { get; init; }

	/// <summary>MKT, LMT, STP …</summary>
	public string OrderType { get; init; } = string.Empty;

	/// <summary>Limit price, for LMT; null = unset.</summary>
	public double? LimitPrice { get; init; }

	/// <summary>Stop (trigger) price, for STP; null = unset.</summary>
	public double? AuxPrice { get; init; }

	/// <summary>DAY, GTC, IOC … Crypto takes GTC (limit) or IOC (market).</summary>
	public string TimeInForce { get; init; } = string.Empty;

	/// <summary>Required when the login sees several accounts (error 435 otherwise).</summary>
	public string Account { get; init; } = string.Empty;

	/// <summary>Also fill outside regular trading hours.</summary>
	public bool OutsideRegularHours { get; init; }

	/// <summary>Amount of money to spend instead of a <see cref="Quantity"/>; null = unset.</summary>
	public double? CashQuantity { get; init; }
}

/// <summary>An order's state (orderStatus); IB sends one on every change.</summary>
public sealed record OrderStatus(
	int OrderId,
	string Status,
	decimal Filled,
	decimal Remaining,
	double AverageFillPrice,
	long PermId,
	int ParentId,
	double LastFillPrice,
	int ClientId,
	string WhyHeld,
	double? MarketCapPrice
)
{
	/// <summary>
	/// Filled, Cancelled, ApiCancelled or Inactive: no more updates follow, except for a
	/// reactivated Inactive order.
	/// </summary>
	public bool IsDone => Status is "Filled" or "Cancelled" or "ApiCancelled" or "Inactive";
}

/// <summary>
/// An order that was sent: its id (to cancel it) and its status updates until it is done. A
/// rejection (e.g. 460 "No trading permissions") fails <see cref="Statuses"/> with an
/// <c>IbkrException</c>. Stopping reading never cancels the order.
/// </summary>
public sealed record PlacedOrder(int OrderId, IAsyncEnumerable<OrderStatus> Statuses);
