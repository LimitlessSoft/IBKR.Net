using IBKR.Net.Protocol;

namespace IBKR.Net;

public sealed partial class IbkrClient
{
	/// <summary>
	/// Every contract matching <paramref name="query"/> (reqContractDetails), e.g. all listings of a
	/// symbol, or every option of one expiration. Fails with error 200 when nothing matches.
	/// </summary>
	public Task<IReadOnlyList<ContractDetails>> GetContractDetailsAsync(
		Contract query,
		CancellationToken cancellationToken = default
	) =>
		CollectAsync<ContractDetails>(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestContractDetails)
					.Add(8)
					.Add(id)
					.AddContract(query)
					.Add(false) // include expired
					.Add(string.Empty) // security id type
					.Add(string.Empty) // security id
					.Add(string.Empty), // issuer id
			fields =>
				fields[0] == IncomingMessageIds.ContractData
					? [Decoders.ContractData(fields)]
					: null,
			IncomingMessageIds.ContractDataEnd,
			cancellationToken
		);

	/// <summary>
	/// The expirations and strikes of an underlying's options, one entry per exchange and trading
	/// class (reqSecDefOptParams). Much lighter than resolving every option contract.
	/// </summary>
	public Task<IReadOnlyList<OptionChainParameters>> GetOptionChainParametersAsync(
		string underlyingSymbol,
		string underlyingSecurityType,
		int underlyingConId,
		string futuresExchange = "",
		CancellationToken cancellationToken = default
	) =>
		CollectAsync<OptionChainParameters>(
			id =>
				new RequestBuilder(OutgoingMessageIds.RequestSecDefOptParams)
					.Add(id)
					.Add(underlyingSymbol)
					.Add(futuresExchange)
					.Add(underlyingSecurityType)
					.Add(underlyingConId),
			fields =>
				fields[0] == IncomingMessageIds.SecDefOptParams
					? [Decoders.SecDefOptParams(fields)]
					: null,
			IncomingMessageIds.SecDefOptParamsEnd,
			cancellationToken
		);
}
