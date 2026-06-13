using System;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	/// <summary>
	/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
	/// </summary>
	internal class DecryptPasswordAction : IParameterisedAction
	{
		private readonly IPasswordManager passwordManager;
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;
		private readonly DialogCreator dialogCreator;
		private readonly PasswordFileParser passwordFileParser;
		private readonly TemporaryClipboard clipboard;
		private readonly PasswordStoreConfig passwordStoreConfig;
		private readonly NotificationConfig notificationConfig;

		public DecryptPasswordAction(
			IPasswordManager passwordManager,
			INotificationService notificationService,
			IDialogService dialogService,
			DialogCreator dialogCreator,
			PasswordFileParser passwordFileParser,
			TemporaryClipboard clipboard,
			PasswordStoreConfig passwordStoreConfig,
			NotificationConfig notificationConfig)
		{
			this.passwordManager = passwordManager;
			this.notificationService = notificationService;
			this.dialogService = dialogService;
			this.dialogCreator = dialogCreator;
			this.passwordFileParser = passwordFileParser;
			this.clipboard = clipboard;
			this.passwordStoreConfig = passwordStoreConfig;
			this.notificationConfig = notificationConfig;
		}

		public void Execute(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			var selectedFile = dialogCreator.RequestPasswordFile();
			// If the user cancels their selection, the password decryption should be cancelled too.
			if (selectedFile == null)
			{
				return;
			}

			KeyedPasswordFile passFile;
			try
			{
				passFile = passwordManager.DecryptPassword(selectedFile, passwordStoreConfig.FirstLineOnly);
			}
			catch (Exception e) when (e is GpgError || e is GpgException || e is ConfigurationException)
			{
				dialogService.ShowErrorWindow("Password decryption failed: " + e.Message);
				return;
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow($"Password decryption failed: An error occurred: {e.GetType().Name}: {e.Message}");
				return;
			}

			if (copyToClipboard)
			{
				var timeout = clipboard.Place(passFile.Password);
				if (notificationConfig.Types.PasswordCopied)
				{
					notificationService.Raise($"The password has been copied to your clipboard.\nIt will be cleared in {timeout.TotalSeconds:0.##} seconds.", Severity.Info);
				}
			}
			var usernameEntered = false;
			if (typeUsername)
			{
				try
				{
					var username = passwordFileParser.GetUsername(passFile);
					if (username != null)
					{
						KeyboardEmulator.EnterText(username);
						usernameEntered = true;
					}
				}
				catch (Exception e)
				{
					dialogService.ShowErrorWindow($"Could not retrieve your username: {e.Message}");
					return;
				}
			}
			if (typePassword)
			{
				// If a username has also been entered, press Tab to switch to the password field.
				if (usernameEntered)
				{
					KeyboardEmulator.EnterTab();
				}

				KeyboardEmulator.EnterText(passFile.Password);
			}
		}

	}
}
