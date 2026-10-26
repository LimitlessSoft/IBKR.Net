using IBKR.Net.Protocol;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Protocol;

public class ExecutionDecoderTests
{
	[Fact]
	public void An_execution_decodes_its_fill_and_contract()
	{
		var execution = Decoders.Execution(RecordedExecutions.Buy);

		Assert.Equal("0000e001.00000001.01.01", execution.ExecId);
		Assert.Equal(0, execution.OrderId);
		Assert.Equal(0, execution.ClientId);
		Assert.Equal(900000001, execution.PermId);
		Assert.Equal(
			new Contract
			{
				ConId = 934002900,
				Symbol = "SPY",
				SecurityType = "OPT",
				LastTradeDateOrContractMonth = "20261026",
				Strike = 768,
				Right = "P",
				Multiplier = "100",
				Exchange = "CBOE",
				Currency = "USD",
				LocalSymbol = "SPY   261026P00768000",
				TradingClass = "SPY",
			},
			execution.Contract
		);
		Assert.Equal("DU0000001", execution.Account);
		Assert.Equal("CBOE", execution.Exchange);
		Assert.Equal("BOT", execution.Side);
		Assert.Equal(1, execution.Shares);
		Assert.Equal(4.61, execution.Price);
		Assert.Equal(1, execution.CumulativeQuantity);
		Assert.Equal(4.61, execution.AveragePrice);
		Assert.Equal("OptionChain", execution.OrderRef);
		Assert.Equal("20261026 10:00:59 US/Eastern", execution.TimeText);
	}

	[Fact]
	public void The_time_is_read_in_the_zone_IB_names()
	{
		var time = Decoders.Execution(RecordedExecutions.Buy).Time;

		Assert.Equal(new DateTimeOffset(2026, 10, 26, 14, 0, 59, TimeSpan.Zero), time);
	}

	[Fact]
	public void An_unknown_zone_leaves_the_time_unset()
	{
		string[] frame = [.. RecordedExecutions.Buy];
		frame[15] = "20261026 10:00:59 Nowhere/Zone";

		Assert.Null(Decoders.Execution(frame).Time);
	}

	[Fact]
	public void A_commission_report_reads_unset_realized_pnl_as_null()
	{
		Assert.Equal(
			new CommissionReport("0000e001.00000001.01.01", 1.04028, "USD", null),
			Decoders.CommissionReport(RecordedExecutions.BuyCommission)
		);
		Assert.Equal(
			14.916573,
			Decoders.CommissionReport(RecordedExecutions.SellCommission).RealizedPnl
		);
	}
}
