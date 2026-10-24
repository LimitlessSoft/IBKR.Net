using System.Globalization;

namespace IBKR.Net.Protocol;

/// <summary>Reads a frame's fields in order; see docs/protocol.md for each message's layout.</summary>
internal sealed class FieldReader(string[] fields, int start = 0)
{
	private int _index = start;

	public bool HasMore => _index < fields.Length;

	public string String() => _index < fields.Length ? fields[_index++] : string.Empty;

	public int Int() =>
		int.TryParse(String(), CultureInfo.InvariantCulture, out var value) ? value : 0;

	public long Long() =>
		long.TryParse(String(), CultureInfo.InvariantCulture, out var value) ? value : 0;

	public double Double() =>
		double.TryParse(String(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
			? value
			: 0;

	/// <summary>Empty and IB's "unset" (double max) read as null.</summary>
	public double? NullableDouble() =>
		double.TryParse(String(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
		&& value < double.MaxValue
			? value
			: null;

	public decimal Decimal() =>
		decimal.TryParse(String(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
			? value
			: 0;

	/// <summary>Empty reads as <paramref name="unset"/> (e.g. -1 for a midpoint bar's volume).</summary>
	public decimal DecimalOr(decimal unset) =>
		decimal.TryParse(String(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
			? value
			: unset;

	public void Skip(int count = 1) => _index += count;
}
