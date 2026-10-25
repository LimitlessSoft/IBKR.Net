namespace IBKR.Net.Protocol;

/// <summary>
/// The client version range sent in the handshake. The server answers with the highest version it
/// shares with us, and its message formats follow that version. The cap keeps every message in the
/// text format: newer server versions move messages to protobuf.
/// </summary>
internal static class ProtocolVersions
{
	public const int Min = 100;

	/// <summary>Seen working end to end on 2026-10-23 (TWS negotiated exactly this).</summary>
	public const int Max = 187;

	/// <summary>
	/// The version placeOrder / orderStatus were observed at (2026-10-25). Newer versions may add
	/// order fields, so orders go over a connection capped here.
	/// </summary>
	public const int Orders = 178;

	/// <summary>The range to offer: up to <paramref name="max"/> when given, within Min..Max.</summary>
	public static string Range(int? max)
	{
		var top = max ?? Max;
		if (top is < Min or > Max)
			throw new ArgumentOutOfRangeException(
				nameof(max),
				top,
				$"The highest version must be within {Min}..{Max}."
			);
		return $"v{Min}..{top}";
	}
}
