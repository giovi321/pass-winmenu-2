using PassWinmenu.Configuration;
using PassWinmenu.UpdateChecking;

namespace PassWinmenu.Jobs;

public class StartUpdateChecker : IStartupJob
{
	private readonly UpdateChecker updateChecker;
	private readonly UpdateCheckingConfig config;

	public StartUpdateChecker(UpdateChecker updateChecker, UpdateCheckingConfig config)
	{
		this.updateChecker = updateChecker;
		this.config = config;
	}

	public void Run()
	{
		if (config.CheckForUpdates)
		{
			updateChecker.Start();
		}
	}
}
