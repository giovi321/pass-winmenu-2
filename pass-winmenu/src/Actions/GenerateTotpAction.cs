using System;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

#nullable enable
namespace PassWinmenu.Actions
{
	internal class GenerateTotpAction
	{
		private readonly IPasswordManager passwordManager;
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;
		private readonly DialogCreator dialogCreator;
		private readonly TemporaryClipboard clipboard;
		private readonly Config config;

		public GenerateTotpAction(
			IPasswordManager passwordManager,
			INotificationService notificationService,
			IDialogService dialogService,
			DialogCreator dialogCreator,
			TemporaryClipboard clipboard,
			Config config)
		{
			this.passwordManager = passwordManager;
			this.notificationService = notificationService;
			this.dialogService = dialogService;
			this.dialogCreator = dialogCreator;
			this.clipboard = clipboard;
			this.config = config;
		}

		/// <summary>
		/// Asks the user to choose a password file, decrypts it,
		/// generates an OTP code from the secret in the totp field, and copies the resulting value to the clipboard.
		/// </summary>
		public void GenerateTotpCode(bool copyToClipboard, bool typeTotpCode)
		{
			var selectedFile = dialogCreator.RequestPasswordFile();
			// If the user cancels their selection, the password decryption should be cancelled too.
			if (selectedFile == null)
			{
				return;
			}

			KeyedPasswordFile passwordFile;
			try
			{
				passwordFile = passwordManager.DecryptPassword(selectedFile, true);
			}
			catch (Exception e) when (e is GpgError || e is GpgException || e is ConfigurationException)
			{
				dialogService.ShowErrorWindow("TOTP decryption failed: " + e.Message);
				return;
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow($"TOTP decryption failed: An error occurred: {e.GetType().Name}: {e.Message}");
				return;
			}

			var totp = TotpGenerator.GenerateTotpCode(passwordFile, DateTime.UtcNow);
			totp.Match(
				code =>
				{
					if (copyToClipboard)
					{
						var timeout = clipboard.Place(code);
						if (config.Notifications.Types.TotpCopied)
						{
							notificationService.Raise($"The totp code has been copied to your clipboard.\nIt will be cleared in {timeout.TotalSeconds:0.##} seconds.", Severity.Info);
						}
					}

					if (typeTotpCode)
					{
						KeyboardEmulator.EnterText(code);
					}
				},
				() =>
				{
					dialogService.ShowErrorWindow($"TOTP decryption failed: Failed to find an OTP secret.");
				}
			);

		}
	}
}
