using System;
using System.IO;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{

	/// <summary>
	/// Adds a new password to the password store.
	/// </summary>
	internal class AddPasswordAction : IAction
	{
		private readonly DialogCreator dialogCreator;
		private readonly IPasswordManager passwordManager;
		private readonly ISyncService? syncService;
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;
		private readonly TemporaryClipboard clipboard;
		private readonly Config config;

		public HotkeyAction ActionType => HotkeyAction.AddPassword;

		public AddPasswordAction(
			DialogCreator dialogCreator,
			IPasswordManager passwordManager,
			Option<ISyncService> syncService,
			INotificationService notificationService,
			IDialogService dialogService,
			TemporaryClipboard clipboard,
			Config config)
		{
			this.dialogCreator = dialogCreator;
			this.passwordManager = passwordManager;
			this.syncService = syncService.ValueOrDefault();
			this.notificationService = notificationService;
			this.dialogService = dialogService;
			this.clipboard = clipboard;
			this.config = config;
		}

		public void Execute()
		{
			Helpers.AssertOnUiThread();

			var passwordFilePath = dialogCreator.ShowFileSelectionWindow();
			// passwordFileName will be null if no file was selected
			if (passwordFilePath == null)
			{
				return;
			}

			// Display the password generation window.
			var passwordWindow = new PasswordWindow(Path.GetFileNameWithoutExtension(passwordFilePath), config.PasswordStore.PasswordGeneration);
			passwordWindow.ShowDialog();
			if (!passwordWindow.DialogResult.GetValueOrDefault())
			{
				return;
			}
			var password = passwordWindow.Password.Text;
			var metadata = passwordWindow.ExtraContent.Text.Replace(Environment.NewLine, "\n");

			PasswordFile passwordFile;
			try
			{
				passwordFile = passwordManager.AddPassword(passwordFilePath, password, metadata);
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow("Unable to encrypt your password: " + e.Message);
				return;
			}
			// Copy the newly generated password.
			var timeout = clipboard.Place(password);
			if (config.Notifications.Types.PasswordGenerated)
			{
				notificationService.Raise($"The new password has been copied to your clipboard.\nIt will be cleared in {timeout.TotalSeconds:0.##} seconds.", Severity.Info);
			}

			try
			{
				// Add the password to Git
				syncService?.AddPassword(passwordFile.FullPath);
			}
			catch (Exception e)
			{
				Log.Send($"Failed to commit {passwordFile.FullPath}");
				Log.ReportException(e);
				dialogService.ShowErrorWindow("Unable to commit your changes: " + e.Message);
			}
		}
	}
}
