namespace IBKR.Net;

/// <summary>An error IB reported for a request (message 4), e.g. 200 "No security definition".</summary>
public sealed class IbkrException(int requestId, int code, string message)
	: Exception($"[{code}] {message} (reqId {requestId})")
{
	/// <summary>The request it belongs to; -1 when it isn't tied to one.</summary>
	public int RequestId => requestId;

	/// <summary>IB's error code.</summary>
	public int Code => code;

	/// <summary>IB's text, without the code.</summary>
	public string ErrorText => message;
}

/// <summary>
/// An informational message or an error not tied to a request: farm status (2104, 2106, 2158),
/// delayed data shown (10167), connectivity (1100, 1102) …
/// </summary>
public sealed record IbkrNotice(int RequestId, int Code, string Message);
