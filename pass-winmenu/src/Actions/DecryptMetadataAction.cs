using System;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions;

internal class DecryptMetadataAction
{
	private readonly DialogCreator dialogCreator;
	private readonly IPasswordManager passwordManager;
	private readonly IDialogService dialogService;
	private readonly INotificationService notificationService;
	private readonly TemporaryClipboard clipboard;
	private readonly Config config;

	public DecryptMetadataAction(
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
	
	public void DecryptMetadata(bool copyToClipboard, bool type)
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
			dialogService.ShowErrorWindow(
				$"Password decryption failed: An error occurred: {e.GetType().Name}: {e.Message}");
			return;
		}

		if (copyToClipboard)
		{
			var timeout = clipboard.Place(passFile.Metadata);
			if (config.Notifications.Types.PasswordCopied)
			{
				notificationService.Raise(
					$"The key has been copied to your clipboard.\nIt will be cleared in {timeout.TotalSeconds:0.##} seconds.",
					Severity.Info);
			}
		}

		if (type)
		{
			KeyboardEmulator.EnterText(passFile.Metadata);
		}
	}
}
