namespace PassWinmenu.Biometrics;

/// <summary>
/// A no-op provider: Windows Hello unlock is never active, so GPG always uses the normal
/// gpg-agent/pinentry flow. Used as the default when no biometric provider is wired in (e.g. in
/// tests).
/// </summary>
internal sealed class NullPassphraseProvider : IPassphraseProvider
{
	public bool IsEnabled => false;

	public PassphraseResult GetPassphrase() => PassphraseResult.Unavailable;

	public void Invalidate()
	{
	}
}
