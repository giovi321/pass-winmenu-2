using LibGit2Sharp;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Notifications;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;

#nullable enable
namespace PassWinmenu.Actions
{
	internal class RetrieveChangesAction : IAction
	{
		private readonly ISyncService? syncService;
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;

		public HotkeyAction ActionType => HotkeyAction.GitPull;

		public RetrieveChangesAction(
			Option<ISyncService> syncService,
			INotificationService notificationService,
			IDialogService dialogService)
		{
			this.syncService = syncService.ValueOrDefault();
			this.notificationService = notificationService;
			this.dialogService = dialogService;
		}

		public void Execute()
		{
			if (syncService == null)
			{
				notificationService.Raise(
					"Unable to update the password store: pass-winmenu is not configured to use Git.",
					Severity.Warning);
				return;
			}

			try
			{
				syncService.Fetch();
				var details = syncService.GetTrackingDetails();
				if (details.BehindBy > 0)
				{
					syncService.Rebase();
					notificationService.Raise($"Pulled {details.BehindBy} new changes.", Severity.Info);
				}
				else
				{
					notificationService.Raise($"Your local repository already contains the latest changes.", Severity.Info);
				}
			}
			catch (LibGit2SharpException e) when (e.Message == "unsupported URL protocol")
			{
				dialogService.ShowErrorWindow(
					"Unable to update the password store: Remote uses an unknown protocol.\n\n" +
					"If your remote URL is an SSH URL, try setting sync-mode to native-git in your configuration file.");
			}
			catch (LibGit2SharpException e)
			{
				dialogService.ShowErrorWindow($"Unable to update the password store:\n{e.Message}");
			}
			catch (GitException e)
			{
				if (e.GitError != null)
				{
					dialogService.ShowErrorWindow(
						$"Unable to fetch the latest changes: Git returned an error.\n\n{e.GitError}");
				}
				else
				{
					dialogService.ShowErrorWindow($"Unable to fetch the latest changes: {e.Message}");
				}
			}
		}
	}
}
