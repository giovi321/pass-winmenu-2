using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Notifications;

namespace PassWinmenu.Jobs;

/// <summary>
/// For the "once per session" cadence: at startup, authenticate once with Windows Hello and
/// seed gpg-agent's cache so the rest of the session decrypts silently. Other cadences
/// (every-password, cache) are handled at decryption time and are no-ops here.
/// </summary>
internal class BiometricUnlockJob : IStartupJob
{
	private readonly BiometricConfig config;
	private readonly IBiometricVault vault;
	private readonly IGpgKeygripResolver keygripResolver;
	private readonly IGpgAgentPresetService presetService;
	private readonly IGpgAgentControl agentControl;
	private readonly GpgAgentConfigUpdater agentConfigUpdater;
	private readonly INotificationService notificationService;

	public BiometricUnlockJob(
		BiometricConfig config,
		IBiometricVault vault,
		IGpgKeygripResolver keygripResolver,
		IGpgAgentPresetService presetService,
		IGpgAgentControl agentControl,
		GpgAgentConfigUpdater agentConfigUpdater,
		INotificationService notificationService)
	{
		this.config = config;
		this.vault = vault;
		this.keygripResolver = keygripResolver;
		this.presetService = presetService;
		this.agentControl = agentControl;
		this.agentConfigUpdater = agentConfigUpdater;
		this.notificationService = notificationService;
	}

	private const string AllowPresetPassphraseKey = "allow-preset-passphrase";

	public void Run()
	{
		if (!config.Enabled || config.Mode != BiometricUnlockMode.OncePerSession)
		{
			// The preset path is not in use, so undo the global gpg-agent setting we may have
			// enabled in a previous run. This stops a disabled/removed feature from permanently
			// weakening the user's gpg-agent for every other GPG tool.
			Task.Run(RevertAgentConfig);
			return;
		}

		// Don't block startup; the Windows Hello prompt appears shortly after launch.
		Task.Run(PresetForSession);
	}

	private void RevertAgentConfig()
	{
		try
		{
			if (agentConfigUpdater.RemoveManagedKeys(new[] { AllowPresetPassphraseKey }))
			{
				agentControl.ReloadAgent();
			}
		}
		catch (Exception e)
		{
			Log.Send($"Could not revert gpg-agent config: {e.GetType().Name}: {e.Message}", LogLevel.Warning);
		}
	}

	private void PresetForSession()
	{
		try
		{
			if (!vault.IsAvailableAsync().GetAwaiter().GetResult())
			{
				return;
			}

			if (!vault.IsEnrolled)
			{
				notificationService.Raise(
					"Windows Hello unlock is enabled but not set up yet. Use the tray menu "
					+ "(More Actions → Set up Windows Hello unlock) to enrol.",
					Severity.Info);
				return;
			}

			// Presetting requires gpg-agent to allow it; ensure the option is set and live.
			agentConfigUpdater.UpdateAgentConfig(new Dictionary<string, string> { [AllowPresetPassphraseKey] = "" });
			agentControl.ReloadAgent();

			var result = vault.TryUnlockAsync().GetAwaiter().GetResult();
			switch (result.Outcome)
			{
				case UnlockOutcome.Success:
					PresetPassphrase(result.Passphrase!);
					break;
				case UnlockOutcome.KeyInvalidated:
					vault.ClearEnrollment();
					notificationService.Raise(
						"Your Windows Hello key has changed, so saved passwords can no longer be unlocked. "
						+ "Please set up Windows Hello unlock again.",
						Severity.Warning);
					break;
				case UnlockOutcome.Failed:
					notificationService.Raise($"Windows Hello unlock failed: {result.Error}", Severity.Error);
					break;
				case UnlockOutcome.Cancelled:
				case UnlockOutcome.Unavailable:
				case UnlockOutcome.NotEnrolled:
				default:
					// Fall back silently to the normal pinentry prompt for this session.
					break;
			}
		}
		catch (Exception e)
		{
			// Never let a startup job crash the application.
			Log.Send($"Biometric unlock job failed: {e.GetType().Name}: {e.Message}", LogLevel.Warning);
			Log.ReportException(e);
		}
	}

	private void PresetPassphrase(char[] passphrase)
	{
		try
		{
			var keygrip = keygripResolver.GetEncryptionKeygrip(config.KeyId);
			if (keygrip == null)
			{
				notificationService.Raise(
					"Could not find a GPG encryption key to unlock. Windows Hello unlock is inactive.",
					Severity.Warning);
				return;
			}

			var presetResult = presetService.Preset(keygrip, passphrase);
			switch (presetResult)
			{
				case PresetResult.NotAllowed:
					notificationService.Raise(
						"gpg-agent rejected presetting the passphrase. Windows Hello unlock could not be activated.",
						Severity.Warning);
					break;
				case PresetResult.Failed:
					notificationService.Raise(
						"Could not preset the passphrase into gpg-agent. Windows Hello unlock is inactive.",
						Severity.Warning);
					break;
				case PresetResult.Success:
				default:
					break;
			}
		}
		finally
		{
			Array.Clear(passphrase, 0, passphrase.Length);
		}
	}
}
