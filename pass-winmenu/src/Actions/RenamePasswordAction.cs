using System;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

#nullable enable
namespace PassWinmenu.Actions
{
	/// <summary>
	/// Renames (or moves) a password file within the password store.
	/// </summary>
	internal class RenamePasswordAction : IAction
	{
		private readonly DialogCreator dialogCreator;
		private readonly IPasswordManager passwordManager;
		private readonly ISyncService? syncService;
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;
		private readonly PathDisplayService pathDisplayService;
		private readonly Config config;

		public HotkeyAction ActionType => HotkeyAction.RenamePassword;

		public RenamePasswordAction(
			DialogCreator dialogCreator,
			IPasswordManager passwordManager,
			Option<ISyncService> syncService,
			INotificationService notificationService,
			IDialogService dialogService,
			PathDisplayService pathDisplayService,
			Config config)
		{
			this.dialogCreator = dialogCreator;
			this.passwordManager = passwordManager;
			this.syncService = syncService.ValueOrDefault();
			this.notificationService = notificationService;
			this.dialogService = dialogService;
			this.pathDisplayService = pathDisplayService;
			this.config = config;
		}

		public void Execute()
		{
			Helpers.AssertOnUiThread();

			var selectedFile = dialogCreator.RequestPasswordFile();
			if (selectedFile == null)
			{
				return;
			}

			// Ask the user for the new name, pre-filled with the current one.
			var newPath = dialogCreator.ShowFileSelectionWindow(
				$"Choose a new name for \"{pathDisplayService.GetDisplayPath(selectedFile)}\"...",
				pathDisplayService.GetDisplayPath(selectedFile));
			if (newPath == null)
			{
				return;
			}

			PasswordFile renamedFile;
			try
			{
				renamedFile = passwordManager.RenamePassword(selectedFile, newPath);
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow("Unable to rename the password file: " + e.Message);
				return;
			}

			if (renamedFile.FullPath == selectedFile.FullPath)
			{
				// The name was not changed, so there's nothing to commit.
				return;
			}

			try
			{
				// Commit the rename to Git
				syncService?.RenamePassword(selectedFile.FullPath, renamedFile.FullPath);
			}
			catch (Exception e)
			{
				Log.Send($"Failed to commit rename of {selectedFile.FullPath} to {renamedFile.FullPath}");
				Log.ReportException(e);
				dialogService.ShowErrorWindow("Unable to commit your changes: " + e.Message);
			}

			if (config.Notifications.Types.PasswordUpdated)
			{
				notificationService.Raise($"Password file \"{selectedFile.FileNameWithoutExtension}\" has been renamed to \"{renamedFile.FileNameWithoutExtension}\".", Severity.Info);
			}
		}
	}
}
