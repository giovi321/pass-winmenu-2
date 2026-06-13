using PassWinmenu.Configuration;

namespace PassWinmenu.Jobs;

public class EnableConfigReloading : IStartupJob
{
	private readonly ConfigManager configManager;
	private readonly ApplicationConfig config;

	public EnableConfigReloading(ConfigManager configManager, ApplicationConfig config)
	{
		this.configManager = configManager;
		this.config = config;
	}

	public void Run()
	{
		if (config.ReloadConfig)
		{
			configManager.EnableAutoReloading();
		}
	}
}
