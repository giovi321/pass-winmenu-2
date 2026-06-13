using System.Threading.Tasks;

namespace PassWinmenu.Biometrics;

/// <summary>
/// The outcome of a <see cref="IBiometricVault.TryUnlockAsync"/> attempt.
/// </summary>
internal enum UnlockOutcome
{
	/// <summary>The passphrase was unlocked successfully.</summary>
	Success,

	/// <summary>No passphrase has been enrolled yet.</summary>
	NotEnrolled,

	/// <summary>Windows Hello is not available/configured on this device.</summary>
	Unavailable,

	/// <summary>The user cancelled the Windows Hello prompt (or chose password).</summary>
	Cancelled,

	/// <summary>The Hello key changed (PIN/biometric reset) or the blob is corrupt — re-enrollment is required.</summary>
	KeyInvalidated,

	/// <summary>An unexpected error occurred.</summary>
	Failed,
}

/// <summary>
/// The result of an unlock attempt. When <see cref="Outcome"/> is
/// <see cref="UnlockOutcome.Success"/>, <see cref="Passphrase"/> holds the decrypted
/// passphrase; the caller owns it and should clear it after use.
/// </summary>
internal readonly struct UnlockResult
{
	public UnlockOutcome Outcome { get; }
	public char[]? Passphrase { get; }
	public string? Error { get; }

	private UnlockResult(UnlockOutcome outcome, char[]? passphrase, string? error)
	{
		Outcome = outcome;
		Passphrase = passphrase;
		Error = error;
	}

	public static UnlockResult Succeeded(char[] passphrase) => new(UnlockOutcome.Success, passphrase, null);
	public static UnlockResult Of(UnlockOutcome outcome, string? error = null) => new(outcome, null, error);
}

/// <summary>
/// Orchestrates enrolment and unlocking of the GPG passphrase via Windows Hello.
/// </summary>
internal interface IBiometricVault
{
	/// <summary>Whether Windows Hello is available on this device.</summary>
	Task<bool> IsAvailableAsync();

	/// <summary>Whether a passphrase has been enrolled (a blob exists).</summary>
	bool IsEnrolled { get; }

	/// <summary>
	/// Enrols the given passphrase: creates the Hello credential, signs the challenge, and
	/// stores the encrypted passphrase. Throws <see cref="BiometricException"/> on failure.
	/// The caller owns <paramref name="passphrase"/> and should clear it afterwards.
	/// </summary>
	Task EnrollAsync(char[] passphrase);

	/// <summary>
	/// Attempts to unlock the passphrase, prompting for a Windows Hello gesture. Never throws
	/// for expected conditions — inspect <see cref="UnlockResult.Outcome"/>.
	/// </summary>
	Task<UnlockResult> TryUnlockAsync();

	/// <summary>Removes the stored enrolment (forces re-enrolment next time).</summary>
	void ClearEnrollment();
}
