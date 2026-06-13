using System;
using System.Collections.Generic;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	/// <summary>
	/// Lets the user pick a password file, decrypts it, and shows every field (the password
	/// plus each metadata key) separately in the same menu interface used to select a password.
	/// Selecting a field copies its value to the clipboard, making it easy to grab a single field.
	/// </summary>
	internal class ShowPasswordAction : IAction
	{
		private const string PasswordFieldName = "Password";

		private readonly DialogCreator dialogCreator;
		private readonly IPasswordManager passwordManager;
		private readonly IDialogService dialogService;
		private readonly INotificationService notificationService;
		private readonly TemporaryClipboard clipboard;
		private readonly Config config;

		public ShowPasswordAction(
			DialogCreator dialogCreator,
			IPasswordManager passwordManager,
			IDialogService dialogService,
			INotificationService notificationService,
			TemporaryClipboard clipboard,
			Config config)
		{
			this.dialogCreator = dialogCreator;
			this.passwordManager = passwordManager;
			this.dialogService = dialogService;
			this.notificationService = notificationService;
			this.clipboard = clipboard;
			this.config = config;
		}

		public HotkeyAction ActionType => HotkeyAction.ShowPassword;

		public void Execute()
		{
			var selectedFile = dialogCreator.RequestPasswordFile();
			if (selectedFile == null)
			{
				return;
			}

			KeyedPasswordFile passFile;
			try
			{
				passFile = passwordManager.DecryptPassword(selectedFile, config.PasswordStore.FirstLineOnly);
			}
			catch (GpgError e)
			{
				dialogService.ShowErrorWindow("Password decryption failed: " + e.Message);
				return;
			}
			catch (GpgException e)
			{
				dialogService.ShowErrorWindow("Password decryption failed. " + e.Message);
				return;
			}
			catch (ConfigurationException e)
			{
				dialogService.ShowErrorWindow("Password decryption failed: " + e.Message);
				return;
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow($"Password decryption failed: An error occurred: {e.GetType().Name}: {e.Message}");
				return;
			}

			// Present the password and every metadata key as individual rows, each showing its value.
			// The password is masked until revealed; clicking any value copies it.
			var rows = new List<PasswordFieldRow>
			{
				new(PasswordFieldName, passFile.Password, isSecret: true),
			};
			foreach (var pair in passFile.Keys)
			{
				rows.Add(new PasswordFieldRow(pair.Key, pair.Value, isSecret: false));
			}

			var window = new PasswordDetailsWindow(selectedFile.FileNameWithoutExtension, rows, CopyField, config.Interface);
			window.ShowDialog();
		}

		private void CopyField(PasswordFieldRow row)
		{
			var timeout = clipboard.Place(row.Value);
			if (config.Notifications.Types.PasswordCopied)
			{
				notificationService.Raise(
					$"The '{row.Name}' field has been copied to your clipboard.\n"
					+ $"It will be cleared in {timeout.TotalSeconds:0.##} seconds.",
					Severity.Info);
			}
		}
	}
}
