using PassWinmenu.ExternalPrograms;
using PassWinmenu.Utilities;

namespace PassWinmenu.Jobs;

public class StartRemoteUpdateChecker : IStartupJob
{
	private readonly Option<RemoteUpdateChecker> remoteUpdateChecker;

	public StartRemoteUpdateChecker(Option<RemoteUpdateChecker> remoteUpdateChecker)
	{
		this.remoteUpdateChecker = remoteUpdateChecker;
	}

	public void Run()
	{
		this.remoteUpdateChecker.Apply(c => c.Start());
	}
}
