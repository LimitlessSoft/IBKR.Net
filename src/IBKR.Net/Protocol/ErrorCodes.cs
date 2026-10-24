namespace IBKR.Net.Protocol;

/// <summary>Which error codes are notices rather than a failed request.</summary>
internal static class ErrorCodes
{
	/// <summary>
	/// 2100–2199 are warnings (farm status 2104/2106/2158, and 2150 on reqPnLSingle, after which
	/// the data still arrives); 10167 is "displaying delayed market data".
	/// </summary>
	public static bool IsInformational(int code) => code is (>= 2100 and < 2200) or 10167;
}
