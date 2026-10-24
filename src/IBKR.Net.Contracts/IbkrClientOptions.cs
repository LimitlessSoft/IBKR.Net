namespace IBKR.Net;

/// <summary>Where and how <c>IbkrClient</c> connects to TWS / IB Gateway.</summary>
public sealed class IbkrClientOptions
{
	/// <summary>Host running TWS / IB Gateway; the API socket only accepts local connections by default.</summary>
	public string Host { get; init; } = "127.0.0.1";

	/// <summary>API port: IB Gateway 4001 live / 4002 paper, TWS 7496 live / 7497 paper.</summary>
	public int Port { get; init; } = 4002;

	/// <summary>Every connection to the same TWS / Gateway needs a distinct client id.</summary>
	public int ClientId { get; init; } = 1;

	/// <summary>How long connecting and the version handshake may take.</summary>
	public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);
}
