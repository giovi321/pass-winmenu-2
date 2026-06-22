namespace PassWinmenu.Biometrics;

/// <summary>
/// What <see cref="GPG"/> should do after asking an <see cref="IPassphraseProvider"/> for a
/// passphrase on a gpg-agent cache miss.
/// </summary>
internal enum PassphraseOutcome
{
	/// <summary>A passphrase was unlocked; inject it into the decryption via loopback pinentry.</summary>
	Provided,

	/// <summary>
	/// The user actively declined the Windows Hello gesture. The decryption must fail closed
	/// rather than fall back to a normal decrypt, which would silently bypass Windows Hello if
	/// gpg-agent happened to hold the passphrase.
	/// </summary>
	Declined,

	/// <summary>
	/// Windows Hello could not be used (disabled, not enrolled, no hardware, or its key was
	/// reset). Fall back to the normal gpg-agent/pinentry flow so the user is not locked out.
	/// </summary>
	Unavailable,
}

/// <summary>
/// The result of asking <see cref="IPassphraseProvider.GetPassphrase"/> for a passphrase. When
/// <see cref="Outcome"/> is <see cref="PassphraseOutcome.Provided"/>, <see cref="Passphrase"/>
/// holds the passphrase; the caller owns it and must clear it after use.
/// </summary>
internal readonly struct PassphraseResult
{
	public PassphraseOutcome Outcome { get; }
	public char[]? Passphrase { get; }

	private PassphraseResult(PassphraseOutcome outcome, char[]? passphrase)
	{
		Outcome = outcome;
		Passphrase = passphrase;
	}

	public static PassphraseResult Provided(char[] passphrase) => new(PassphraseOutcome.Provided, passphrase);
	public static PassphraseResult Declined { get; } = new(PassphraseOutcome.Declined, null);
	public static PassphraseResult Unavailable { get; } = new(PassphraseOutcome.Unavailable, null);
}
