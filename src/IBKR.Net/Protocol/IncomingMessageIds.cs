namespace IBKR.Net.Protocol;

/// <summary>First field of a frame from the server. Each id is confirmed on the wire before it is added.</summary>
internal static class IncomingMessageIds
{
	public const string CurrentTime = "49";
}
