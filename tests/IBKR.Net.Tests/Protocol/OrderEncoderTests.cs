using IBKR.Net.Protocol;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Protocol;

/// <summary>placeOrder / cancelOrder must go out exactly as recorded (docs/protocol.md, "Orders").</summary>
public class OrderEncoderTests
{
	private static readonly Contract Btc = new()
	{
		ConId = 479624278,
		Symbol = "BTC",
		SecurityType = "CRYPTO",
		Exchange = "PAXOS",
		Currency = "USD",
		LocalSymbol = "BTC.USD",
		TradingClass = "BTC",
	};

	private static readonly Order LimitGtc = new()
	{
		Action = "BUY",
		Quantity = 0.0002m,
		OrderType = "LMT",
		LimitPrice = 20000,
		TimeInForce = "GTC",
		Account = "DU0000001",
	};

	[Fact]
	public void Limit_order_matches_the_recording() =>
		Assert.Equal(RecordedOrders.PlaceLimitGtc, OrderEncoder.PlaceOrder(200, Btc, LimitGtc));

	[Fact]
	public void Cash_quantity_market_order_matches_the_recording() =>
		Assert.Equal(
			RecordedOrders.PlaceMarketCashIoc,
			OrderEncoder.PlaceOrder(
				201,
				Btc,
				LimitGtc with
				{
					Quantity = 0,
					OrderType = "MKT",
					LimitPrice = null,
					TimeInForce = "IOC",
					CashQuantity = 10,
				}
			)
		);

	[Fact]
	public void Sell_and_outside_rth_match_the_recording()
	{
		Assert.Equal(
			RecordedOrders.PlaceSell,
			OrderEncoder.PlaceOrder(202, Btc, LimitGtc with { Action = "SELL" })
		);
		Assert.Equal(
			RecordedOrders.PlaceOutsideRth,
			OrderEncoder.PlaceOrder(203, Btc, LimitGtc with { OutsideRegularHours = true })
		);
	}

	[Fact]
	public void Stop_order_without_account_matches_the_recording() =>
		Assert.Equal(
			RecordedOrders.PlaceStopDayNoAccount,
			OrderEncoder.PlaceOrder(
				205,
				Btc,
				LimitGtc with
				{
					Quantity = 3,
					OrderType = "STP",
					LimitPrice = null,
					AuxPrice = 2.5,
					TimeInForce = "DAY",
					Account = "",
				}
			)
		);

	[Fact]
	public void Cancel_matches_the_recording() =>
		Assert.Equal(RecordedOrders.CancelOrder, OrderEncoder.CancelOrder(200));

	[Fact]
	public void Order_status_decodes_the_recorded_frame() =>
		Assert.Equal(
			new OrderStatus(10, "ApiCancelled", 0, 0, 0, 0, 0, 0, 91, "", null),
			Decoders.OrderStatus(RecordedOrders.OrderStatusApiCancelled)
		);
}
