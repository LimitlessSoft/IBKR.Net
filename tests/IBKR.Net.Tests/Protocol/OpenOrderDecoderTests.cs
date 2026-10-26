using IBKR.Net.Protocol;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Protocol;

public class OpenOrderDecoderTests
{
	public static TheoryData<string[]> Recordings =>
		[RecordedOpenOrders.OpenOrderV178, RecordedOpenOrders.OpenOrderV187];

	[Theory]
	[MemberData(nameof(Recordings))]
	public void An_open_order_decodes_the_same_at_both_versions(string[] frame)
	{
		var order = Decoders.OpenOrder(frame);

		Assert.Equal(200, order.OrderId);
		Assert.Equal(101, order.ClientId);
		Assert.Equal(900000001, order.PermId);
		Assert.Equal(
			new Contract
			{
				ConId = 14232,
				Symbol = "VOW",
				SecurityType = "STK",
				Exchange = "SMART",
				Currency = "EUR",
				LocalSymbol = "VOW",
				TradingClass = "XETRA",
			},
			order.Contract
		);
		Assert.Equal(
			new Order
			{
				Action = "BUY",
				Quantity = 1,
				OrderType = "LMT",
				LimitPrice = 66,
				TimeInForce = "DAY",
				Account = "DU0000001",
			},
			order.Order
		);
	}

	[Fact]
	public void Without_its_status_an_open_order_has_everything_remaining()
	{
		var order = Decoders.OpenOrder(RecordedOpenOrders.OpenOrderV187);

		Assert.Equal("", order.Status);
		Assert.Equal(0, order.Filled);
		Assert.Equal(1, order.Remaining);
	}
}
