using System;
using System.Threading.Tasks;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.WinApi;

namespace PassWinmenu.Jobs;

internal class PreloadGpgAgent : IStartupJob
{
	private readonly GPG gpg;
	private readonly IDialogService dialogService;
	private readonly GpgAgentConfig gpgAgentConfig;

	public PreloadGpgAgent(GPG gpg, IDialogService dialogService, GpgAgentConfig gpgAgentConfig)
	{
		this.gpg = gpg;
		this.dialogService = dialogService;
		this.gpgAgentConfig = gpgAgentConfig;
	}
	
	public void Run()
	{
		try
		{
			Log.Send("Using GPG version " + gpg.GetVersion());
		}
		catch (System.ComponentModel.Win32Exception)
		{
			dialogService.ShowErrorWindow("Could not find GPG. Make sure your gpg-path is set correctly.");
			App.Exit();
			return;
		}
		catch (Exception e)
		{
			dialogService.ShowErrorWindow($"Failed to initialise GPG. {e.GetType().Name}: {e.Message}");
			App.Exit();
			return;
		}

		if (gpgAgentConfig.Preload)
		{
			Task.Run(
				() =>
				{
					try
					{
						gpg.StartAgent();
					}
					catch (GpgError err)
					{
						dialogService.ShowErrorWindow(err.Message);
					}
					// Ignore other exceptions. If it turns out GPG is misconfigured,
					// these errors will surface upon decryption/encryption.
					// The reason we catch GpgErrors here is so we can notify the user
					// if we don't detect any decryption keys.
				});
		}
	}
}
