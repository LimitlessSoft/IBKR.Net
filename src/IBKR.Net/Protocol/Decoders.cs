using System.Globalization;

namespace IBKR.Net.Protocol;

/// <summary>Turns server frames into models; one method per message in docs/protocol.md.</summary>
internal static class Decoders
{
	/// <summary>4: [2] request id, [3] code, [4] message.</summary>
	public static IbkrNotice Error(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		return new IbkrNotice(reader.Int(), reader.Int(), reader.String());
	}

	/// <summary>15: [2] comma-separated accounts, with a trailing comma.</summary>
	public static IReadOnlyList<string> ManagedAccounts(string[] fields) =>
		new FieldReader(fields, 2).String().Split(',', StringSplitOptions.RemoveEmptyEntries);

	/// <summary>10: [1] request id, then the contract and its details.</summary>
	public static ContractDetails ContractData(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		var symbol = reader.String();
		var securityType = reader.String();
		reader.Skip(); // last trade date and time
		var lastTradeDate = reader.String();
		var strike = reader.NullableDouble();
		var right = reader.String();
		var exchange = reader.String();
		var currency = reader.String();
		var localSymbol = reader.String();
		var marketName = reader.String();
		var tradingClass = reader.String();
		var conId = reader.Int();
		var minTick = reader.Double();
		var multiplier = reader.String();
		reader.Skip(); // order types
		var validExchanges = reader.String();
		reader.Skip(); // price magnifier
		var underlyingConId = reader.Int();
		var longName = reader.String();
		var primaryExchange = reader.String();
		var contractMonth = reader.String();
		var industry = reader.String();
		var category = reader.String();
		var subcategory = reader.String();
		var timeZoneId = reader.String();
		var tradingHours = reader.String();
		var liquidHours = reader.String();
		reader.Skip(2); // ev rule, ev multiplier
		var securityIds = new Dictionary<string, string>();
		for (var count = reader.Int(); count > 0; count--)
			securityIds[reader.String()] = reader.String();

		return new ContractDetails
		{
			Contract = new Contract
			{
				ConId = conId,
				Symbol = symbol,
				SecurityType = securityType,
				LastTradeDateOrContractMonth = lastTradeDate,
				Strike = strike > 0 ? strike : null,
				Right = right,
				Multiplier = multiplier,
				Exchange = exchange,
				PrimaryExchange = primaryExchange,
				Currency = currency,
				LocalSymbol = localSymbol,
				TradingClass = tradingClass,
			},
			MarketName = marketName,
			MinTick = minTick,
			ValidExchanges = validExchanges,
			UnderlyingConId = underlyingConId,
			LongName = longName,
			ContractMonth = contractMonth,
			Industry = industry,
			Category = category,
			Subcategory = subcategory,
			TimeZoneId = timeZoneId,
			TradingHours = tradingHours,
			LiquidHours = liquidHours,
			SecurityIds = securityIds,
		};
	}

	/// <summary>75: [1] request id, exchange, underlying conId, class, multiplier, counted lists.</summary>
	public static OptionChainParameters SecDefOptParams(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		var exchange = reader.String();
		var underlyingConId = reader.Int();
		var tradingClass = reader.String();
		var multiplier = reader.String();
		var expirations = new List<string>();
		for (var count = reader.Int(); count > 0; count--)
			expirations.Add(reader.String());
		var strikes = new List<double>();
		for (var count = reader.Int(); count > 0; count--)
			strikes.Add(reader.Double());
		return new OptionChainParameters(
			exchange,
			underlyingConId,
			tradingClass,
			multiplier,
			[.. expirations.Order()],
			[.. strikes.Order()]
		);
	}

	/// <summary>1: [3] tick type, [4] price, [5] size.</summary>
	public static MarketDataTick TickPrice(string[] fields)
	{
		var reader = new FieldReader(fields, 3);
		return new MarketDataTick(reader.Int(), Price: reader.Double(), Size: reader.Decimal());
	}

	/// <summary>2: [3] tick type, [4] size.</summary>
	public static MarketDataTick TickSize(string[] fields)
	{
		var reader = new FieldReader(fields, 3);
		return new MarketDataTick(reader.Int(), Size: reader.Decimal());
	}

	/// <summary>45: [3] tick type, [4] value.</summary>
	public static MarketDataTick TickGeneric(string[] fields)
	{
		var reader = new FieldReader(fields, 3);
		return new MarketDataTick(reader.Int(), Value: reader.Double());
	}

	/// <summary>17: [1] request id, start, end, count, then 8 fields per bar.</summary>
	public static IReadOnlyList<Bar> HistoricalData(string[] fields)
	{
		var reader = new FieldReader(fields, 4);
		var bars = new List<Bar>();
		for (var count = reader.Int(); count > 0; count--)
			bars.Add(
				new Bar(
					BarTime(reader.String()),
					reader.Double(),
					reader.Double(),
					reader.Double(),
					reader.Double(),
					reader.Decimal(),
					reader.Decimal(),
					reader.Int()
				)
			);
		return bars;
	}

	/// <summary>61: account, the contract, position, average cost.</summary>
	public static Position Position(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		var account = reader.String();
		var contract = new Contract
		{
			ConId = reader.Int(),
			Symbol = reader.String(),
			SecurityType = reader.String(),
			LastTradeDateOrContractMonth = reader.String(),
			Strike = reader.NullableDouble() is > 0 and var strike ? strike : null,
			Right = reader.String(),
			Multiplier = reader.String(),
			Exchange = reader.String(),
			Currency = reader.String(),
			LocalSymbol = reader.String(),
			TradingClass = reader.String(),
		};
		return new Position(account, contract, reader.Decimal(), reader.Double());
	}

	/// <summary>63: [3] account, tag, value, currency.</summary>
	public static AccountValue AccountSummary(string[] fields)
	{
		var reader = new FieldReader(fields, 3);
		return new AccountValue(reader.String(), reader.String(), reader.String(), reader.String());
	}

	/// <summary>94: [2] daily, unrealized, realized.</summary>
	public static AccountPnl Pnl(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		return new AccountPnl(
			reader.NullableDouble(),
			reader.NullableDouble(),
			reader.NullableDouble()
		);
	}

	/// <summary>95: [2] position, daily, unrealized, realized, value.</summary>
	public static PositionPnl PnlSingle(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		return new PositionPnl(
			reader.Decimal(),
			reader.NullableDouble(),
			reader.NullableDouble(),
			reader.NullableDouble(),
			reader.NullableDouble()
		);
	}

	// With date format 2, intraday bars come as unix seconds and daily and longer ones as yyyyMMdd
	private static DateTimeOffset BarTime(string value) =>
		value.Length == 8
			? new DateTimeOffset(
				DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture),
				TimeSpan.Zero
			)
			: DateTimeOffset.FromUnixTimeSeconds(long.Parse(value, CultureInfo.InvariantCulture));
}
