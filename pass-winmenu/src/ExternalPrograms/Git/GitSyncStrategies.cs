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
			string? gitPath = null;
			if (syncMode == SyncMode.Auto)
			{
				try
				{
					gitPath = executablePathResolver.Resolve(config.GitPath);
					syncMode = SyncMode.NativeGit;
				}
				catch (ExecutableNotFoundException)
				{
					syncMode = SyncMode.Builtin;
				}
			}

			if (syncMode == SyncMode.NativeGit)
			{
				// Resolve to an absolute path so the git process is started from the resolved
				// location rather than through the CreateProcess search order, which checks the
				// application and current working directories first and could pick up a planted git.exe.
				gitPath ??= executablePathResolver.Resolve(config.GitPath);
				return new NativeGitSyncStrategy(repositoryPath, gitPath, config);
			}
			else
			{
				return new LibGit2SharpSyncStrategy(repository);
			}
		}

	}
}
