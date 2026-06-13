using System;
using System.Linq;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions;

internal class GetKeyAction
{
	private DialogCreator dialogCreator;
	private readonly IPasswordManager passwordManager;
	private readonly IDialogService dialogService;
	private readonly INotificationService notificationService;
	private readonly TemporaryClipboard clipboard;
	private readonly Config config;

	public GetKeyAction(
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

	public void GetKey(bool copyToClipboard, bool type, string? key)
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

		if (string.IsNullOrWhiteSpace(key))
		{
			key = dialogCreator.ShowPasswordMenu(passFile.Keys, k => k.Key, "Choose a key...").ValueOrDefault().Key;
			if (key == null)
			{
				return;
			}
		}

		var values = passFile.Keys.Where(k => k.Key == key).ToList();
		if (values.Count == 0)
		{
			return;
		}

		string chosenValue;
		if (values.Count > 1)
		{
			var choice = dialogCreator.ShowPasswordMenu(values, v => v.Value, "Multiple keys found, choose a value...");
			if (choice.IsNone)
			{
				return;
			}

			chosenValue = choice.ValueOrDefault().Value;
		}
		else
		{
			chosenValue = values[0].Value;
		}

		if (copyToClipboard)
		{
			var timeout = clipboard.Place(chosenValue);
			if (config.Notifications.Types.PasswordCopied)
			{
				notificationService.Raise($"The key has been copied to your clipboard.\nIt will be cleared in {timeout.TotalSeconds:0.##} seconds.", Severity.Info);
			}
		}
		if (type)
		{
			// Strip control characters so a crafted/imported field value can't emit unintended
			// keystrokes (e.g. tabs/escapes) into the focused window.
			KeyboardEmulator.EnterText(new string(chosenValue.Where(c => !char.IsControl(c)).ToArray()));
		}
	}
}
