using LibGit2Sharp;
using PassWinmenu.Configuration;
using PassWinmenu.WinApi;

#nullable enable
namespace PassWinmenu.ExternalPrograms
{
	internal class GitSyncStrategies
	{
		private readonly IExecutablePathResolver executablePathResolver;

		public GitSyncStrategies(IExecutablePathResolver executablePathResolver)
		{
			this.executablePathResolver = executablePathResolver;
		}

		public IGitSyncStrategy ChooseSyncStrategy(string repositoryPath, Repository repository, GitConfig config)
		{
			var syncMode = config.SyncMode;
			if (syncMode == SyncMode.Auto)
			{
				try
				{
					executablePathResolver.Resolve(config.GitPath);
					syncMode = SyncMode.NativeGit;
				}
				catch (ExecutableNotFoundException)
				{
					syncMode = SyncMode.Builtin;
				}
			}

			if (syncMode == SyncMode.NativeGit)
			{
				return new NativeGitSyncStrategy(repositoryPath, config);
			}
			else
			{
				return new LibGit2SharpSyncStrategy(repository);
			}
		}

	}
}
