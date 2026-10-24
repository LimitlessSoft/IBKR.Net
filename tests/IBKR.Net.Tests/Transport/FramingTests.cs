using IBKR.Net.Transport;

namespace IBKR.Net.Tests.Transport;

public class FramingTests
{
	[Fact]
	public void Fields_are_nul_terminated_behind_a_big_endian_length()
	{
		var frame = Framing.EncodeFields(["49", "1"]);

		Assert.Equal([0, 0, 0, 5, (byte)'4', (byte)'9', 0, (byte)'1', 0], frame);
	}

	[Fact]
	public void Empty_fields_survive_a_round_trip()
	{
		var frame = Framing.EncodeFields(["71", "2", "7", ""]);

		Assert.Equal(["71", "2", "7", ""], Framing.DecodeFields(frame.AsSpan(4)));
	}

	[Fact]
	public async Task A_frame_is_read_from_a_stream()
	{
		using var stream = new MemoryStream([
			.. Framing.EncodeFields(["49", "1", "1792800000"]),
			.. Framing.EncodeFields(["4"]),
		]);

		Assert.Equal(
			["49", "1", "1792800000"],
			(await Framing.ReadFieldsAsync(stream, TestContext.Current.CancellationToken))!
		);
		Assert.Equal(
			["4"],
			(await Framing.ReadFieldsAsync(stream, TestContext.Current.CancellationToken))!
		);
		Assert.Null(await Framing.ReadFieldsAsync(stream, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task A_frame_cut_short_is_an_error()
	{
		using var stream = new MemoryStream(Framing.EncodeFields(["49", "1"])[..6]);

		await Assert.ThrowsAsync<EndOfStreamException>(() =>
			Framing.ReadFieldsAsync(stream, TestContext.Current.CancellationToken)
		);
	}

	[Fact]
	public async Task An_absurd_length_is_rejected_instead_of_allocated()
	{
		using var stream = new MemoryStream([0x7F, 0xFF, 0xFF, 0xFF]);

		await Assert.ThrowsAsync<InvalidDataException>(() =>
			Framing.ReadFieldsAsync(stream, TestContext.Current.CancellationToken)
		);
	}
}
