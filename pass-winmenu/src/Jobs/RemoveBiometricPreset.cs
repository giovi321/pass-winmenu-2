using System;
using System.Threading.Tasks;
using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenu.Jobs;

/// <summary>
/// Windows Hello unlock used to preset the GPG passphrase into gpg-agent (the old
/// "once-per-session" mode), which required enabling <c>allow-preset-passphrase</c> in the user's
/// gpg-agent.conf. Decryption now uses loopback on a cache miss instead, governed solely by
/// gpg-agent's own cache, so presetting is no longer used. This startup job removes the
/// pass-winmenu-managed <c>allow-preset-passphrase</c> key that older versions added, so the
/// removed feature does not keep weakening the user's gpg-agent after they upgrade.
/// </summary>
internal class RemoveBiometricPreset : IStartupJob
{
	private const string AllowPresetPassphraseKey = "allow-preset-passphrase";

	private readonly GpgAgentConfigUpdater agentConfigUpdater;
	private readonly IGpgAgentControl agentControl;

	public RemoveBiometricPreset(GpgAgentConfigUpdater agentConfigUpdater, IGpgAgentControl agentControl)
	{
		this.agentConfigUpdater = agentConfigUpdater;
		this.agentControl = agentControl;
	}

	public void Run()
	{
		// Don't block startup; this only touches gpg-agent.conf when the legacy key is present.
		Task.Run(RevertAgentConfig);
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
}
