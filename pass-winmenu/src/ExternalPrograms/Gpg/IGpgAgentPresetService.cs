namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal enum PresetResult
	{
		/// <summary>The passphrase was cached in gpg-agent.</summary>
		Success,

		/// <summary>gpg-agent does not allow presetting (allow-preset-passphrase is not enabled).</summary>
		NotAllowed,

		/// <summary>gpg-preset-passphrase could not be run, or failed for another reason.</summary>
		Failed,
	}

	internal interface IGpgAgentPresetService
	{
		/// <summary>
		/// Seeds gpg-agent's cache with the passphrase for the given keygrip, so subsequent
		/// decryptions do not prompt. The caller owns <paramref name="passphrase"/>.
		/// </summary>
		PresetResult Preset(string keygrip, char[] passphrase);
	}
}
