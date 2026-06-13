namespace PassWinmenu.Biometrics;

/// <summary>
/// Supplies a GPG passphrase to inject into a single decryption via loopback pinentry, used
/// for the "every-password" and "cache" cadences. Returns null to mean "do nothing special"
/// (use gpg-agent/pinentry as normal), which is the case when biometrics are disabled, in
/// once-per-session mode, or when Windows Hello is unavailable/cancelled.
/// </summary>
internal interface IPassphraseProvider
{
	/// <summary>
	/// Returns a passphrase for the next decryption (the caller owns it and must clear it),
	/// or null to fall back to the normal gpg-agent/pinentry flow.
	/// </summary>
	char[]? GetPassphrase();

	/// <summary>
	/// Signals that a passphrase returned by <see cref="GetPassphrase"/> was rejected by GPG
	/// (e.g. the GPG passphrase was changed). Implementations should discard any cached/stored
	/// passphrase so the user is prompted to re-enrol.
	/// </summary>
	void Invalidate();
}
