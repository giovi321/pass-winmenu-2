using System;
using System.Windows;
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
	/// Deletes a password from the password store.
	/// </summary>
	internal class DeletePasswordAction : IAction
	{
		private readonly DialogCreator dialogCreator;
		private readonly IPasswordManager passwordManager;
		private readonly ISyncService? syncService;
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;
		private readonly Config config;

		public HotkeyAction ActionType => HotkeyAction.DeletePassword;

		public DeletePasswordAction(
			DialogCreator dialogCreator,
			IPasswordManager passwordManager,
			Option<ISyncService> syncService,
			INotificationService notificationService,
			IDialogService dialogService,
			Config config)
		{
			this.dialogCreator = dialogCreator;
			this.passwordManager = passwordManager;
			this.syncService = syncService.ValueOrDefault();
			this.notificationService = notificationService;
			this.dialogService = dialogService;
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

			var confirmed = dialogService.ShowYesNoWindow(
				$"Are you sure you want to permanently delete \"{selectedFile.FileNameWithoutExtension}\"?\n" +
				"This cannot be undone.",
				$"Delete {selectedFile.FileNameWithoutExtension}?",
				MessageBoxImage.Warning);
			if (!confirmed)
			{
				return;
			}

			try
			{
				passwordManager.DeletePassword(selectedFile);
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow("Unable to delete the password file: " + e.Message);
				return;
			}

			try
			{
				// Commit the deletion to Git
				syncService?.DeletePassword(selectedFile.FullPath);
			}
			catch (Exception e)
			{
				Log.Send($"Failed to commit {selectedFile.FullPath}");
				Log.ReportException(e);
				dialogService.ShowErrorWindow("Unable to commit your changes: " + e.Message);
			}

			if (config.Notifications.Types.PasswordUpdated)
			{
				notificationService.Raise($"Password file \"{selectedFile.FileNameWithoutExtension}\" has been deleted.", Severity.Info);
			}
		}
	}
}
