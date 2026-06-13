using System;
using System.IO.Abstractions;
using LibGit2Sharp;

using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;

#nullable enable
namespace PassWinmenu.ExternalPrograms
{
	internal class SyncServiceFactory
	{
		private readonly GitConfig config;
		private readonly IDirectoryInfo passwordStore;
		private readonly ISignService signService;
		private readonly GitSyncStrategies gitSyncStrategies;

		public SyncServiceStatus Status { get; private set; }

		public SyncServiceFactory(GitConfig config, IDirectoryInfo passwordStore, ISignService signService, GitSyncStrategies gitSyncStrategies)
		{
			this.config = config;
			this.passwordStore = passwordStore;
			this.signService = signService;
			this.gitSyncStrategies = gitSyncStrategies;
		}
		
		public ISyncService? BuildSyncService()
		{
			if (config.UseGit)
			{
				try
				{
					var repository = new Repository(passwordStore.FullName);

					var strategy = gitSyncStrategies.ChooseSyncStrategy(passwordStore.FullName, repository, config);
					var git = new Git(repository, passwordStore, strategy, signService);
					Status = SyncServiceStatus.GitSupportEnabled;
					return git;
				}
				catch (RepositoryNotFoundException)
				{
					Log.Send("The password store does not appear to be a Git repository; " +
					         "Git support will be disabled");
				}
				catch (TypeInitializationException e) when (e.InnerException is DllNotFoundException)
				{
					Status = SyncServiceStatus.GitLibraryNotFound;
				}
			}
			else
			{
				Status = SyncServiceStatus.GitSupportDisabled;
			}

			return null;
		}
	}

	internal enum SyncServiceStatus
	{
		GitSupportEnabled,
		GitLibraryNotFound,
		GitSupportDisabled,
	}
}
