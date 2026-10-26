namespace IBKR.Net;

/// <summary>
/// One fill (reqExecutions): what traded, where and at what price, with its commission report
/// when IB sent one. <see cref="OrderId"/> is the placing client's id, 0 for TWS orders.
/// </summary>
public sealed record Execution(
	string ExecId,
	int OrderId,
	int ClientId,
	long PermId,
	Contract Contract,
	string Account,
	string Exchange,
	string Side,
	decimal Shares,
	double Price,
	decimal CumulativeQuantity,
	double AveragePrice,
	string OrderRef
)
{
	/// <summary>The fill time; null when the time zone IB names is unknown on this system.</summary>
	public DateTimeOffset? Time { get; init; }

	/// <summary>The fill time as IB sent it: <c>20261026 10:00:59 US/Eastern</c>.</summary>
	public string TimeText { get; init; } = string.Empty;

	/// <summary>Null when IB sent no commission report for this fill.</summary>
	public CommissionReport? Commission { get; init; }
}

/// <summary>
/// What a fill cost (commissionReport). <see cref="RealizedPnl"/> is null on a fill that opens
/// a position.
/// </summary>
public sealed record CommissionReport(
	string ExecId,
	double Commission,
	string Currency,
	double? RealizedPnl
);
