using PassWinmenu.Configuration;
using PassWinmenu.Notifications;
using PassWinmenu.Utilities;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Provides the GPG passphrase for loopback decryption via Windows Hello. It is asked for a
/// passphrase only when gpg-agent's own cache is cold (see <see cref="GPG.Decrypt"/>), so it keeps
/// no cache of its own: every call prompts for a fresh Hello gesture. The unlock cadence is
/// governed entirely by gpg-agent's cache configuration.
/// </summary>
internal sealed class BiometricPassphraseProvider : IPassphraseProvider
{
	private readonly BiometricConfig config;
	private readonly IBiometricVault vault;
	private readonly INotificationService notificationService;

	public BiometricPassphraseProvider(
		BiometricConfig config,
		IBiometricVault vault,
		INotificationService notificationService)
	{
		this.config = config;
		this.vault = vault;
		this.notificationService = notificationService;
	}

	public bool IsEnabled => config.Enabled;

	public PassphraseResult GetPassphrase()
	{
		if (!config.Enabled)
		{
			return PassphraseResult.Unavailable;
		}

		// Run the unlock on the UI (STA) thread, pumping the dispatcher, so the Windows Hello sign
		// call is made by the thread that owns the foreground window (required for the broker to focus).
		var result = UiThread.RunOnUi(() => vault.TryUnlockAsync());
		switch (result.Outcome)
		{
			case UnlockOutcome.Success:
				return PassphraseResult.Provided(result.Passphrase!);

			case UnlockOutcome.Cancelled:
				// The user actively declined the gesture. Fail closed (see PassphraseOutcome.Declined).
				return PassphraseResult.Declined;

			case UnlockOutcome.KeyInvalidated:
				vault.ClearEnrollment();
				notificationService.Raise(
					"Your Windows Hello key has changed, so saved passwords can no longer be unlocked. "
					+ "Please set up Windows Hello unlock again.",
					Severity.Warning);
				return PassphraseResult.Unavailable;

			case UnlockOutcome.Failed:
				notificationService.Raise($"Windows Hello unlock failed: {result.Error}", Severity.Error);
				return PassphraseResult.Unavailable;

			case UnlockOutcome.NotEnrolled:
			case UnlockOutcome.Unavailable:
			default:
				// Windows Hello is not usable; fall back to the normal pinentry prompt.
				return PassphraseResult.Unavailable;
		}
	}

	public void Invalidate()
	{
		vault.ClearEnrollment();
		notificationService.Raise(
			"Your saved passphrase was rejected by GPG (it may have changed). Please set up "
			+ "Windows Hello unlock again.",
			Severity.Warning);
	}
}
