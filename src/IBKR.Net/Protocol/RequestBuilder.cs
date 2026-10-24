using System.Globalization;

namespace IBKR.Net.Protocol;

/// <summary>Builds a request frame's fields; see docs/protocol.md for each message's layout.</summary>
internal sealed class RequestBuilder(string messageId)
{
	private readonly List<string> _fields = [messageId];

	public RequestBuilder Add(string? value)
	{
		_fields.Add(value ?? string.Empty);
		return this;
	}

	public RequestBuilder Add(int value) => Add(value.ToString(CultureInfo.InvariantCulture));

	public RequestBuilder Add(bool value) => Add(value ? "1" : "0");

	/// <summary>Null is IB's "unset" and goes out as an empty field, never as 0.</summary>
	public RequestBuilder Add(double? value) =>
		Add(value is { } v ? v.ToString(CultureInfo.InvariantCulture) : string.Empty);

	/// <summary>The 12-field contract block several requests share.</summary>
	public RequestBuilder AddContract(Contract contract) =>
		Add(contract.ConId)
			.Add(contract.Symbol)
			.Add(contract.SecurityType)
			.Add(contract.LastTradeDateOrContractMonth)
			.Add(contract.Strike)
			.Add(contract.Right)
			.Add(contract.Multiplier)
			.Add(contract.Exchange)
			.Add(contract.PrimaryExchange)
			.Add(contract.Currency)
			.Add(contract.LocalSymbol)
			.Add(contract.TradingClass);

	public IReadOnlyList<string> Build() => _fields;
}
