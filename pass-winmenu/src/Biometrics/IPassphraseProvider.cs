namespace PassWinmenu.Biometrics;

/// <summary>
/// Supplies the GPG passphrase for loopback decryption via Windows Hello. The re-authentication
/// cadence is governed solely by gpg-agent's own passphrase cache: <see cref="GPG.Decrypt"/>
/// probes that cache first and only asks this provider for a passphrase on a cache miss. When the
/// user declines the Hello gesture the decryption fails closed rather than falling back to a
/// (potentially cached) normal decrypt.
/// </summary>
internal interface IPassphraseProvider
{
	/// <summary>
	/// Whether Windows Hello unlock is enabled. When false, GPG decrypts normally and lets
	/// gpg-agent/pinentry handle the passphrase.
	/// </summary>
	bool IsEnabled { get; }

	/// <summary>
	/// Prompts for a Windows Hello gesture and returns the result. Called only after a gpg-agent
	/// cache miss. The caller owns any returned passphrase and must clear it.
	/// </summary>
	PassphraseResult GetPassphrase();

	/// <summary>
	/// Signals that a passphrase returned by <see cref="GetPassphrase"/> was rejected by GPG
	/// (e.g. the GPG passphrase was changed). Implementations should discard any stored passphrase
	/// so the user is prompted to re-enrol.
	/// </summary>
	void Invalidate();
}
