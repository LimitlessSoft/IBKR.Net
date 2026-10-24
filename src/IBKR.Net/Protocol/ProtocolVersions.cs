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
}
