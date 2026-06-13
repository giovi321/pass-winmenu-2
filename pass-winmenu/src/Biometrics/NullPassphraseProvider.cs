namespace PassWinmenu.Biometrics;

/// <summary>
/// A no-op provider: always falls back to the normal gpg-agent/pinentry flow. Used as the
/// default when no biometric provider is wired in (e.g. in tests).
/// </summary>
internal sealed class NullPassphraseProvider : IPassphraseProvider
{
	public char[]? GetPassphrase() => null;

	public void Invalidate()
	{
	}
}
