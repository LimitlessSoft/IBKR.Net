using IBKR.Net.Protocol;
using IBKR.Net.Tests.Fixtures;

namespace IBKR.Net.Tests.Protocol;

public class DecodersTests
{
	[Fact]
	public void A_stock_listing_decodes_with_its_primary_exchange_and_isin()
	{
		var details = Decoders.ContractData(Recorded.ContractDataStock);

		Assert.Equal(
			new Contract
			{
				ConId = 756733,
				Symbol = "SPY",
				SecurityType = "STK",
				Exchange = "SMART",
				PrimaryExchange = "ARCA",
				Currency = "USD",
				LocalSymbol = "SPY",
				TradingClass = "SPY",
			},
			details.Contract
		);
		Assert.Equal(
			("SS SPDR S&P 500 ETF TRUST-US", 0.01, "US/Eastern", "US78462F1030"),
			(details.LongName, details.MinTick, details.TimeZoneId, details.SecurityIds["ISIN"])
		);
		Assert.StartsWith("20261024:CLOSED", details.LiquidHours);
	}

	[Fact]
	public void An_option_decodes_with_strike_right_and_underlying()
	{
		var details = Decoders.ContractData(Recorded.ContractDataOption);

		Assert.Equal(
			(933461775, "20261026", 705.0, "C", "100", "SPY   261026C00705000"),
			(
				details.Contract.ConId,
				details.Contract.LastTradeDateOrContractMonth,
				details.Contract.Strike!.Value,
				details.Contract.Right,
				details.Contract.Multiplier,
				details.Contract.LocalSymbol
			)
		);
		Assert.Equal((756733, "202610"), (details.UnderlyingConId, details.ContractMonth));
		Assert.Empty(details.SecurityIds);
	}

	[Fact]
	public void Option_parameters_list_expirations_and_strikes_sorted()
	{
		var parameters = Decoders.SecDefOptParams(Recorded.SecDefOptParams);

		Assert.Equal(
			("SMART", 756733, "SPY", "100"),
			(
				parameters.Exchange,
				parameters.UnderlyingConId,
				parameters.TradingClass,
				parameters.Multiplier
			)
		);
		Assert.Equal(["20261026", "20261027", "20261028"], parameters.Expirations);
		Assert.Equal([690.5, 700, 705, 710], parameters.Strikes);
	}

	[Fact]
	public void Historical_bars_have_unix_times_and_all_eight_fields()
	{
		var bars = Decoders.HistoricalData(Recorded.HistoricalData);

		Assert.Equal(2, bars.Count);
		Assert.Equal(
			new Bar(
				DateTimeOffset.FromUnixTimeSeconds(1792762200),
				759.9,
				760.73,
				759.72,
				759.99,
				998384,
				760.033m,
				5488
			),
			bars[0]
		);
	}

	[Fact]
	public void Ticks_decode_by_message_kind()
	{
		Assert.Equal(
			new MarketDataTick(9, Price: 763.960022, Size: 0),
			Decoders.TickPrice(Recorded.MarketData[3])
		);
		Assert.Equal(new MarketDataTick(8, Size: 0), Decoders.TickSize(Recorded.MarketData[4]));
		Assert.Equal(
			new MarketDataTick(49, Value: 0),
			Decoders.TickGeneric(Recorded.MarketData[5])
		);
	}

	[Fact]
	public void A_position_carries_its_contract()
	{
		var position = Decoders.Position(Recorded.Position);

		Assert.Equal(
			("DU0000001", 2m, 150.5),
			(position.Account, position.Quantity, position.AverageCost)
		);
		Assert.Equal(
			(920268479, "OPT", 759.0, "C"),
			(
				position.Contract.ConId,
				position.Contract.SecurityType,
				position.Contract.Strike!.Value,
				position.Contract.Right
			)
		);
	}

	[Fact]
	public void Account_values_and_pnl_decode()
	{
		Assert.Equal(
			new AccountValue("DU0000001", "NetLiquidation", "100000.00", "USD"),
			Decoders.AccountSummary(Recorded.AccountSummary)
		);
		Assert.Equal(new AccountPnl(8.5936, 0, 35.3089492), Decoders.Pnl(Recorded.Pnl));
	}

	[Fact]
	public void Unset_pnl_values_are_null()
	{
		var pnl = Decoders.PnlSingle(Recorded.PnlSingle);

		Assert.Equal(new PositionPnl(0, 6.8042, null, 6.8042216, null), pnl);
	}

	[Fact]
	public void Errors_carry_request_id_code_and_text() =>
		Assert.Equal(
			new IbkrNotice(3, 200, "No security definition has been found for the request"),
			Decoders.Error(Recorded.NoSecurityDefinition)
		);
}
