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

	/// <summary>
	/// 3 (at version 178): [1] order id, [2] status, [3] filled, [4] remaining, [5] average fill
	/// price, [6] perm id, [7] parent id, [8] last fill price, [9] client id, [10] why held,
	/// [11] market cap price. Empty numbers read as 0 (market cap price: null).
	/// </summary>
	public static OrderStatus OrderStatus(string[] fields)
	{
		var reader = new FieldReader(fields, 1);
		return new OrderStatus(
			reader.Int(),
			reader.String(),
			reader.Decimal(),
			reader.Decimal(),
			reader.Double(),
			reader.Long(),
			reader.Int(),
			reader.Double(),
			reader.Int(),
			reader.String(),
			reader.NullableDouble()
		);
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

	/// <summary>46: [3] tick type, [4] value (45 = last trade time, unix seconds).</summary>
	public static MarketDataTick TickString(string[] fields)
	{
		var reader = new FieldReader(fields, 3);
		return new MarketDataTick(reader.Int(), StringValue: reader.String());
	}

	/// <summary>85: [1] count, then code and name per provider (no request id).</summary>
	public static IReadOnlyList<NewsProvider> NewsProviders(string[] fields)
	{
		var reader = new FieldReader(fields, 1);
		var providers = new List<NewsProvider>();
		for (var count = reader.Int(); count > 0; count--)
			providers.Add(new NewsProvider(reader.String(), reader.String()));
		return providers;
	}

	/// <summary>
	/// 86: [1] request id, [2] time (<c>2026-10-26 13:03:00.0</c>, UTC), [3] provider,
	/// [4] article id, [5] headline with its metadata in front: <c>{A:800015:L:en}Text</c>.
	/// </summary>
	public static NewsHeadline HistoricalNews(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		var time = DateTimeOffset.ParseExact(
			reader.String(),
			"yyyy-MM-dd HH:mm:ss.F",
			CultureInfo.InvariantCulture,
			DateTimeStyles.AssumeUniversal
		);
		var provider = reader.String();
		var articleId = reader.String();
		var headline = reader.String();
		var extraData = string.Empty;
		if (headline.StartsWith('{') && headline.IndexOf('}') is var close and > 0)
		{
			extraData = headline[1..close];
			headline = headline[(close + 1)..];
		}
		return new NewsHeadline(time, provider, articleId, headline, extraData);
	}

	/// <summary>87: [1] request id, [2] 1 when IB has older headlines.</summary>
	public static bool HistoricalNewsEnd(string[] fields) => new FieldReader(fields, 2).Int() == 1;

	/// <summary>83: [1] request id, [2] type (0 text / HTML, 1 base64 binary), [3] text.</summary>
	public static NewsArticle NewsArticle(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		return new NewsArticle((NewsArticleType)reader.Int(), reader.String());
	}

	/// <summary>
	/// 84: [1] request id, [2] time in unix milliseconds, [3] provider, [4] article id,
	/// [5] headline, [6] metadata (<c>A:800015:L:en</c>).
	/// </summary>
	public static NewsHeadline TickNews(string[] fields)
	{
		var reader = new FieldReader(fields, 2);
		return new NewsHeadline(
			DateTimeOffset.FromUnixTimeMilliseconds(reader.Long()),
			reader.String(),
			reader.String(),
			reader.String(),
			reader.String()
		);
	}

	/// <summary>50: [3] time, open, high, low, close, volume, WAP, count; empty = -1.</summary>
	public static Bar RealTimeBar(string[] fields)
	{
		var reader = new FieldReader(fields, 3);
		return new Bar(
			DateTimeOffset.FromUnixTimeSeconds(reader.Long()),
			reader.Double(),
			reader.Double(),
			reader.Double(),
			reader.Double(),
			reader.DecimalOr(-1),
			reader.DecimalOr(-1),
			reader.Int()
		);
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
		var contract = ReadContract(reader);
		return new Position(account, contract, reader.Decimal(), reader.Double());
	}

	/// <summary>
	/// 5 (versions 178 and 187): [1] order id, [2..12] the contract (as in 61), [13] action,
	/// [14] quantity, [15] order type, [16] limit price, [17] aux price, [18] time in force,
	/// [20] account, [24] client id, [25] perm id. The later fields are not read (see
	/// docs/protocol.md); status, filled and remaining come from the 3 frame that follows.
	/// </summary>
	public static OpenOrder OpenOrder(string[] fields)
	{
		var reader = new FieldReader(fields, 1);
		var orderId = reader.Int();
		var contract = ReadContract(reader);
		var action = reader.String();
		var quantity = reader.Decimal();
		var orderType = reader.String();
		var limitPrice = reader.NullableDouble();
		var auxPrice = reader.NullableDouble();
		var timeInForce = reader.String();
		reader.Skip();
		var account = reader.String();
		reader.Skip(3);
		var clientId = reader.Int();
		var permId = reader.Long();
		var order = new Order
		{
			Action = action,
			Quantity = quantity,
			OrderType = orderType,
			// An LMT order came with aux price 0.0: zero is "not set" for both prices
			LimitPrice = limitPrice is 0 ? null : limitPrice,
			AuxPrice = auxPrice is 0 ? null : auxPrice,
			TimeInForce = timeInForce,
			Account = account,
		};
		return new OpenOrder(orderId, clientId, permId, contract, order, "", 0, quantity);
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

	// The 11-field contract of positions and open orders; a stock's right comes as "?"
	private static Contract ReadContract(FieldReader reader) =>
		new()
		{
			ConId = reader.Int(),
			Symbol = reader.String(),
			SecurityType = reader.String(),
			LastTradeDateOrContractMonth = reader.String(),
			Strike = reader.NullableDouble() is > 0 and var strike ? strike : null,
			Right = reader.String() is var right and not "?" ? right : string.Empty,
			Multiplier = reader.String(),
			Exchange = reader.String(),
			Currency = reader.String(),
			LocalSymbol = reader.String(),
			TradingClass = reader.String(),
		};

	// With date format 2, intraday bars come as unix seconds and daily and longer ones as yyyyMMdd
	private static DateTimeOffset BarTime(string value) =>
		value.Length == 8
			? new DateTimeOffset(
				DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture),
				TimeSpan.Zero
			)
			: DateTimeOffset.FromUnixTimeSeconds(long.Parse(value, CultureInfo.InvariantCulture));
}
