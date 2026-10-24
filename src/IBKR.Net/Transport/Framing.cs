using System.Buffers.Binary;
using System.Text;

namespace IBKR.Net.Transport;

/// <summary>
/// The socket's framing: a 4-byte big-endian payload length, then the payload. After the
/// handshake every payload is a list of NUL-terminated ASCII fields.
/// </summary>
internal static class Framing
{
	/// <summary>Frames above this are treated as a broken stream rather than allocated.</summary>
	public const int MaxPayloadLength = 16 * 1024 * 1024;

	public static byte[] EncodeFields(IReadOnlyList<string> fields)
	{
		var payload = new StringBuilder();
		foreach (var field in fields)
			payload.Append(field).Append('\0');
		return EncodeRaw(payload.ToString());
	}

	/// <summary>A frame whose payload is the text as is, e.g. the handshake's version range.</summary>
	public static byte[] EncodeRaw(string payload)
	{
		var bytes = Encoding.UTF8.GetBytes(payload);
		var frame = new byte[4 + bytes.Length];
		BinaryPrimitives.WriteInt32BigEndian(frame, bytes.Length);
		bytes.CopyTo(frame, 4);
		return frame;
	}

	public static string[] DecodeFields(ReadOnlySpan<byte> payload)
	{
		var text = Encoding.UTF8.GetString(payload);
		var fields = text.Split('\0');
		// Every field ends with NUL, so the split leaves one empty entry after the last
		return text.EndsWith('\0') ? fields[..^1] : fields;
	}

	/// <summary>Reads one frame's fields; null when the stream ended cleanly before a frame.</summary>
	public static async Task<string[]?> ReadFieldsAsync(
		Stream stream,
		CancellationToken cancellationToken
	)
	{
		var header = new byte[4];
		if (!await ReadExactlyOrEndAsync(stream, header, cancellationToken).ConfigureAwait(false))
			return null;

		var length = BinaryPrimitives.ReadInt32BigEndian(header);
		if (length is < 0 or > MaxPayloadLength)
			throw new InvalidDataException($"Invalid frame length {length}.");

		var payload = new byte[length];
		if (!await ReadExactlyOrEndAsync(stream, payload, cancellationToken).ConfigureAwait(false))
			throw new EndOfStreamException("The connection closed in the middle of a frame.");
		return DecodeFields(payload);
	}

	private static async Task<bool> ReadExactlyOrEndAsync(
		Stream stream,
		byte[] buffer,
		CancellationToken cancellationToken
	)
	{
		var read = 0;
		while (read < buffer.Length)
		{
			var count = await stream
				.ReadAsync(buffer.AsMemory(read), cancellationToken)
				.ConfigureAwait(false);
			if (count == 0)
			{
				if (read == 0)
					return false;
				throw new EndOfStreamException("The connection closed in the middle of a frame.");
			}
			read += count;
		}
		return true;
	}
}
