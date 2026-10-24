namespace IBKR.Net.Protocol;

/// <summary>First field of a request frame. Each id is confirmed on the wire before it is added.</summary>
internal static class OutgoingMessageIds
{
	public const string RequestCurrentTime = "49";
	public const string StartApi = "71";
}
