using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenu.Jobs;

public class UpdateGpgAgentConfig : IStartupJob
{
	private readonly GpgAgentConfigFile gpgConfig;
	private readonly GpgAgentConfigUpdater gpgAgentConfigUpdater;

	public UpdateGpgAgentConfig(GpgAgentConfigFile gpgConfig, GpgAgentConfigUpdater gpgAgentConfigUpdater)
	{
		this.gpgConfig = gpgConfig;
		this.gpgAgentConfigUpdater = gpgAgentConfigUpdater;
	}

	public void Run()
	{
		if (gpgConfig.AllowConfigManagement)
		{
			gpgAgentConfigUpdater.UpdateAgentConfig(gpgConfig.Keys);
		}
	}
}
