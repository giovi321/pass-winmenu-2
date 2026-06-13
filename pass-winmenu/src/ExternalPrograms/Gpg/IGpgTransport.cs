#nullable enable
namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal interface IGpgTransport
	{
		/// <summary>
		/// Invokes gpg, optionally writing <paramref name="input"/> to stdin as text.
		/// </summary>
		GpgResult CallGpg(string arguments, string? input = null);

		/// <summary>
		/// Invokes gpg, writing <paramref name="secretStdin"/> to stdin as raw bytes. Used for the
		/// passphrase on the loopback path so it never becomes an un-zeroable string. The caller owns
		/// and clears the byte array.
		/// </summary>
		GpgResult CallGpgWithSecret(string arguments, byte[] secretStdin);
	}
}
