using System;
using System.Threading;
using Microsoft.Win32;
using PassWinmenu.Configuration;
using PassWinmenu.Notifications;
using PassWinmenu.Utilities;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Provides the GPG passphrase for loopback decryption in the "every-password" and "cache"
/// cadences. In once-per-session mode it returns null (that mode seeds gpg-agent at startup
/// instead). Must be a single instance so the cache survives across decryptions.
/// </summary>
internal sealed class BiometricPassphraseProvider : IPassphraseProvider, IDisposable
{
	private readonly BiometricConfig config;
	private readonly IBiometricVault vault;
	private readonly INotificationService notificationService;

	private readonly object sync = new();
	private char[]? cachedPassphrase;
	private DateTime cacheExpiryUtc;
	private Timer? expiryTimer;
	private bool disposed;

	public BiometricPassphraseProvider(
		BiometricConfig config,
		IBiometricVault vault,
		INotificationService notificationService)
	{
		this.config = config;
		this.vault = vault;
		this.notificationService = notificationService;

		// Proactively wipe the cached passphrase when the session locks or the machine suspends,
		// so a cached secret does not survive a lock/sleep.
		SystemEvents.SessionSwitch += OnSessionSwitch;
		SystemEvents.PowerModeChanged += OnPowerModeChanged;
	}

	public char[]? GetPassphrase()
	{
		if (!config.Enabled || config.Mode == BiometricUnlockMode.OncePerSession)
		{
			// Disabled, or handled by the startup preset job — nothing to inject here.
			return null;
		}

		lock (sync)
		{
			if (config.Mode == BiometricUnlockMode.Cache
				&& cachedPassphrase != null
				&& DateTime.UtcNow < cacheExpiryUtc)
			{
				return (char[])cachedPassphrase.Clone();
			}

			ClearCache();

			// Run on a background thread, pumping the UI dispatcher while we wait, so the Windows
			// Hello prompt window (shown via the dispatcher) doesn't deadlock against this wait.
			var result = UiThread.RunBlocking(() => vault.TryUnlockAsync());
			switch (result.Outcome)
			{
				case UnlockOutcome.Success:
					if (config.Mode == BiometricUnlockMode.Cache)
					{
						var seconds = Math.Max(1, config.CacheSeconds);
						cachedPassphrase = (char[])result.Passphrase!.Clone();
						cacheExpiryUtc = DateTime.UtcNow.AddSeconds(seconds);
						// Eagerly wipe at TTL rather than only on the next call.
						expiryTimer?.Dispose();
						expiryTimer = new Timer(_ => WipeCache(), null, TimeSpan.FromSeconds(seconds), Timeout.InfiniteTimeSpan);
					}

					return result.Passphrase;

				case UnlockOutcome.KeyInvalidated:
					vault.ClearEnrollment();
					notificationService.Raise(
						"Your Windows Hello key has changed, so saved passwords can no longer be unlocked. "
						+ "Please set up Windows Hello unlock again.",
						Severity.Warning);
					return null;

				case UnlockOutcome.Failed:
					notificationService.Raise($"Windows Hello unlock failed: {result.Error}", Severity.Error);
					return null;

				case UnlockOutcome.Cancelled:
				case UnlockOutcome.Unavailable:
				case UnlockOutcome.NotEnrolled:
				default:
					// Fall back to the normal pinentry prompt.
					return null;
			}
		}
	}

	public void Invalidate()
	{
		lock (sync)
		{
			ClearCache();
			vault.ClearEnrollment();
		}

		notificationService.Raise(
			"Your saved passphrase was rejected by GPG (it may have changed). Please set up "
			+ "Windows Hello unlock again.",
			Severity.Warning);
	}

	private void WipeCache()
	{
		lock (sync)
		{
			ClearCache();
		}
	}

	private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
	{
		if (e.Reason == SessionSwitchReason.SessionLock)
		{
			WipeCache();
		}
	}

	private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
	{
		if (e.Mode == PowerModes.Suspend)
		{
			WipeCache();
		}
	}

	private void ClearCache()
	{
		expiryTimer?.Dispose();
		expiryTimer = null;

		if (cachedPassphrase != null)
		{
			Array.Clear(cachedPassphrase, 0, cachedPassphrase.Length);
			cachedPassphrase = null;
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		SystemEvents.SessionSwitch -= OnSessionSwitch;
		SystemEvents.PowerModeChanged -= OnPowerModeChanged;
		WipeCache();
	}
}
