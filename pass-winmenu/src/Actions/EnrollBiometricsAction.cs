using System;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;
using PassWinmenu.Notifications;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	/// <summary>
	/// Sets up (or re-sets) Windows Hello unlock: prompts for the GPG passphrase once and
	/// stores it encrypted under a TPM-backed Windows Hello key.
	/// </summary>
	internal class EnrollBiometricsAction : IAction
	{
		private readonly IBiometricVault vault;
		private readonly DialogCreator dialogCreator;
		private readonly IDialogService dialogService;
		private readonly INotificationService notificationService;

		public EnrollBiometricsAction(
			IBiometricVault vault,
			DialogCreator dialogCreator,
			IDialogService dialogService,
			INotificationService notificationService)
		{
			this.vault = vault;
			this.dialogCreator = dialogCreator;
			this.dialogService = dialogService;
			this.notificationService = notificationService;
		}

		public HotkeyAction ActionType => HotkeyAction.EnrollBiometrics;

		public void Execute()
		{
			// Wait on a background thread while pumping the UI dispatcher, so the Windows Hello
			// prompt window (shown via the dispatcher) doesn't deadlock against this wait.
			var available = UiThread.RunBlocking(() => vault.IsAvailableAsync());
			if (!available)
			{
				dialogService.ShowErrorWindow(
					"Windows Hello is not available on this device. Set up a fingerprint or PIN in "
					+ "Windows settings (Sign-in options) first, then try again.");
				return;
			}

			var passphrase = dialogCreator.RequestPassphrase(
				"Enter your GPG passphrase. It will be encrypted with a TPM-backed Windows Hello key so "
				+ "you can unlock your passwords with your fingerprint.");
			if (passphrase == null)
			{
				return; // user cancelled the passphrase prompt
			}

			try
			{
				UiThread.RunBlocking(() => vault.EnrollAsync(passphrase));
				notificationService.Raise(
					"Windows Hello unlock is set up. You can now unlock your passwords with your fingerprint.",
					Severity.Info);
			}
			catch (BiometricException e)
			{
				dialogService.ShowErrorWindow($"Could not set up Windows Hello unlock: {e.Message}");
			}
			finally
			{
				Array.Clear(passphrase, 0, passphrase.Length);
			}
		}
	}
}
