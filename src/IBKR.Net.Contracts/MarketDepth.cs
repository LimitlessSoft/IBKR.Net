namespace IBKR.Net;

/// <summary>What a depth update does to its row.</summary>
public enum MarketDepthOperation
{
	/// <summary>A new row at the position; the rows from there on move down one.</summary>
	Insert = 0,

	/// <summary>The row at the position gets the new price and size.</summary>
	Update = 1,

	/// <summary>The row at the position goes; the rows below move up one.</summary>
	Delete = 2,
}

/// <summary>Which side of the book a depth update is for.</summary>
public enum MarketDepthSide
{
	Ask = 0,
	Bid = 1,
}

/// <summary>
/// One change to an order book (reqMktDepth): an operation on the row at <see cref="Position"/>
/// (0 = best) of one side. <see cref="MarketMaker"/> is the exchange or market maker quoting the
/// row for Level 2 and SMART depth, null for a single exchange's depth. A delete carries price and
/// size 0. IB keeps each side at the requested number of rows, and may send deletes for rows past
/// the end; a book built from these should ignore those.
/// </summary>
public sealed record MarketDepthUpdate(
	int Position,
	MarketDepthOperation Operation,
	MarketDepthSide Side,
	double Price,
	decimal Size,
	string? MarketMaker = null,
	bool IsSmartDepth = false
);

/// <summary>
/// An exchange IB serves depth for (reqMktDepthExchanges). <see cref="ServiceDataType"/> is IB's
/// name for the kind of depth (<c>Deep</c>, <c>Deep2</c>, <c>DeepX</c>, <c>AggDeep</c>);
/// <see cref="AggregatedGroup"/> groups exchanges SMART depth aggregates, null when unset.
/// </summary>
public sealed record MarketDepthExchange(
	string Exchange,
	string SecurityType,
	string ListingExchange,
	string ServiceDataType,
	int? AggregatedGroup
);
