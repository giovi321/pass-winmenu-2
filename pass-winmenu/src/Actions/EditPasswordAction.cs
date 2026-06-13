using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
	/// Edit an existing password.
	/// </summary>
	internal class EditPasswordAction : IAction
	{
		private readonly IPasswordManager passwordManager;
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;
		private readonly ISyncService? syncService;
		private readonly DialogCreator dialogCreator;
		private readonly PathDisplayService pathDisplayService;
		private readonly Config config;

		public HotkeyAction ActionType => HotkeyAction.EditPassword;

		public EditPasswordAction(
			IPasswordManager passwordManager,
			INotificationService notificationService,
			IDialogService dialogService,
			Option<ISyncService> syncService,
			DialogCreator dialogCreator,
			PathDisplayService pathDisplayService,
			Config config
			)
		{
			this.dialogCreator = dialogCreator;
			this.passwordManager = passwordManager;
			this.notificationService = notificationService;
			this.dialogService = dialogService;
			this.syncService = syncService.ValueOrDefault();
			this.pathDisplayService = pathDisplayService;
			this.config = config;
		}

		public void Execute()
		{
			var selectedFile = dialogCreator.RequestPasswordFile();
			if (selectedFile == null)
			{
				return;
			}

			if (config.Interface.PasswordEditor.UseBuiltin)
			{
				DecryptedPasswordFile decryptedFile;
				try
				{
					decryptedFile = passwordManager.DecryptPassword(selectedFile, false);
				}
				catch (Exception e)
				{
					dialogService.ShowErrorWindow($"Unable to edit your password (decryption failed): {e.Message}");
					return;
				}
				EditWithEditWindow(decryptedFile);
			}
			else
			{
				EditWithTextEditor(selectedFile);
			}
		}

		private void EditWithEditWindow(DecryptedPasswordFile file)
		{
			Helpers.AssertOnUiThread();

			var window = new EditWindow(pathDisplayService.GetDisplayPath(file), file.Content, config.PasswordStore.PasswordGeneration);
			if (!window.ShowDialog() ?? true)
			{
				return;
			}

			var newFile = new DecryptedPasswordFile(file, window.PasswordContent.Text.Replace(Environment.NewLine, "\n"));
			try
			{
				passwordManager.EncryptPassword(newFile);

				syncService?.EditPassword(newFile.FullPath);
				if (config.Notifications.Types.PasswordUpdated)
				{
					notificationService.Raise($"Password file \"{newFile.FileNameWithoutExtension}\" has been updated.", Severity.Info);
				}
			}
			catch (GitException e)
			{
				dialogService.ShowErrorWindow($"Unable to commit your changes: {e.Message}");
				EditWithEditWindow(newFile);
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow($"Unable to save your password (encryption failed): {e.Message}");
				EditWithEditWindow(newFile);
			}
		}
		private void EditWithTextEditor(PasswordFile selectedFile)
		{
			// Generate a random plaintext filename.
			var plaintextFile = CreateTemporaryPlaintextFile();
			try
			{
				var passwordFile = passwordManager.DecryptPassword(selectedFile, false);
				File.WriteAllText(plaintextFile, passwordFile.Content);
			}
			catch (Exception e)
			{
				EnsureRemoval(plaintextFile);
				dialogService.ShowErrorWindow($"Unable to edit your password (decryption failed): {e.Message}");
				return;
			}

			// Open the file in the user's default editor
			try
			{
				Process.Start(plaintextFile);
			}
			catch (Win32Exception e)
			{
				EnsureRemoval(plaintextFile);
				dialogService.ShowErrorWindow($"Unable to open an editor to edit your password file ({e.Message}).");
				return;
			}

			var result = MessageBox.Show(
				"Please keep this window open until you're done editing the password file.\n" +
				"Then click Yes to save your changes, or No to discard them.",
				$"Save changes to {selectedFile.FileNameWithoutExtension}?",
				MessageBoxButton.YesNo,
				MessageBoxImage.Information);

			if (result == MessageBoxResult.Yes)
			{
				// Fetch the content from the file, and delete it.
				var content = File.ReadAllText(plaintextFile);
				EnsureRemoval(plaintextFile);

				var newPasswordFile = new DecryptedPasswordFile(selectedFile, content);
				try
				{
					passwordManager.EncryptPassword(newPasswordFile);
					syncService?.EditPassword(selectedFile.FullPath);

					if (config.Notifications.Types.PasswordUpdated)
					{
						notificationService.Raise($"Password file \"{selectedFile}\" has been updated.", Severity.Info);
					}
				}
				catch (GitException e)
				{
					dialogService.ShowErrorWindow($"Unable to commit your changes: {e.Message}");
					EditWithTextEditor(newPasswordFile);
				}
				catch (Exception e)
				{
					dialogService.ShowErrorWindow($"Unable to save your password (encryption failed): {e.Message}");
					EditWithTextEditor(newPasswordFile);
				}
			}
			else
			{
				File.Delete(plaintextFile);
			}
		}

		/// <summary>
		/// Ensures the file at the given path is deleted, warning the user if deletion failed.
		/// </summary>
		private void EnsureRemoval(string path)
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow(
					$"Unable to delete the plaintext file at {path}.\n" +
					$"An error occurred: {e.GetType().Name} ({e.Message}).\n\n" +
					$"Please navigate to the given path and delete it manually.", "Plaintext file not deleted.");
			}
		}

		private string CreateTemporaryPlaintextFile()
		{
			var tempDir = config.Interface.PasswordEditor.TemporaryFileDirectory;

			if (string.IsNullOrWhiteSpace(tempDir))
			{
				Log.Send("No temporary file directory specified, using default.", LogLevel.Warning);
				tempDir = Path.GetTempPath();
			}

			if (!Directory.Exists(tempDir))
			{
				Log.Send($"Temporary directory \"{tempDir}\" does not exist, it will be created.", LogLevel.Info);
				Directory.CreateDirectory(tempDir);
			}

			var tempFile = Path.GetRandomFileName();
			var tempPath = Path.Combine(tempDir, tempFile + Program.PlaintextFileExtension);
			return tempPath;
		}
	}
}
