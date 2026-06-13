using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using PassWinmenu.Configuration;
using PassWinmenu.WinApi;

#nullable enable
namespace PassWinmenu.Actions
{
	internal class ActionDispatcher
	{
		private readonly ILifetimeScope container;
		private readonly IDialogService dialogService;

		public ActionDispatcher(
			ILifetimeScope container,
			IDialogService dialogService)
		{
			this.container = container;
			this.dialogService = dialogService;
		}

		/// <summary>
		/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
		/// </summary>
		public void DecryptPassword(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			using var scope = container.BeginLifetimeScope();
			var decryptPasswordAction = scope.Resolve<DecryptPasswordAction>();

			Try(
				() => decryptPasswordAction.Execute(copyToClipboard, typeUsername, typePassword),
				"Unable to decrypt password");
		}

		/// <summary>
		/// Asks the user to choose a password file, decrypts it,
		/// generates an OTP code from the secret in the totp field, and copies the resulting value to the clipboard.
		/// </summary>
		public void GenerateTotpCode(bool copyToClipboard, bool typeTotpCode)
		{
			using var scope = container.BeginLifetimeScope();
			var generateTotpAction = scope.Resolve<GenerateTotpAction>();

			Try(
				() => generateTotpAction.GenerateTotpCode(copyToClipboard, typeTotpCode),
				"Unable to generate TOTP code");
		}

		public void DecryptMetadata(bool copyToClipboard, bool type)
		{
			using var scope = container.BeginLifetimeScope();
			var decryptMetadataAction = scope.Resolve<DecryptMetadataAction>();

			Try(() => decryptMetadataAction.DecryptMetadata(copyToClipboard, type), "Unable to decrypt metadata");
		}

		public void DecryptPasswordField(bool copyToClipboard, bool type, string? fieldName = null)
		{
			using var scope = container.BeginLifetimeScope();
			var getKeyAction = scope.Resolve<GetKeyAction>();

			Try(() => getKeyAction.GetKey(copyToClipboard, type, fieldName), "Unable to decrypt password field");
		}

		public void Dispatch(HotkeyAction hotkeyAction)
		{
			using var scope = container.BeginLifetimeScope();
			var actions = scope.Resolve<IEnumerable<IAction>>().ToDictionary(a => a.ActionType);

			if (actions.TryGetValue(hotkeyAction, out var action))
			{
				Try(() => action.Execute(), $"Action '{hotkeyAction}' failed");
			}
			else
			{
				dialogService.ShowErrorWindow($"No handler for action '{hotkeyAction}' exists");
			}
		}

		private void Try(Action action, string baseMessage)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow($"{baseMessage}: " + e.Message);
			}
		}
	}
}
