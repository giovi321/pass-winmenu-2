using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenu.Utilities.ExtensionMethods;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	internal class ReencryptPasswordStoreAction : IAction
	{
		private readonly INotificationService notificationService;
		private readonly IDialogService dialogService;
		private readonly IPasswordManager passwordManager;
		private readonly ICryptoService cryptoService;
		private readonly IRecipientFinder recipientFinder;
		private readonly DialogCreator dialogCreator;

		public HotkeyAction ActionType => HotkeyAction.ReencryptPasswordStore;

		public ReencryptPasswordStoreAction(
			INotificationService notificationService,
			IDialogService dialogService,
			IPasswordManager passwordManager,
			ICryptoService cryptoService,
			IRecipientFinder recipientFinder,
			DialogCreator dialogCreator)
		{
			this.notificationService = notificationService;
			this.dialogService = dialogService;
			this.passwordManager = passwordManager;
			this.cryptoService = cryptoService;
			this.recipientFinder = recipientFinder;
			this.dialogCreator = dialogCreator;
		}

		public void Execute()
		{
			Helpers.AssertOnUiThread();

			var result = dialogService.ShowYesNoWindow("The password store will now be re-encrypted. " +
				"If you have added or removed any recipients from your .gpg-id files, " +
				"your password files will be updated to match the recipients specified in those files. " +
				"Note that if your password store contains many passwords, this may take a while.\n\n" +
				"You will also need to have the keys of all recipients in your keyring, and they must be valid " +
				"(unexpired, and trusted).\n\n" +
				"Would you like to continue?", "Re-encrypt your password store");

			if (!result)
			{
				return;
			}

			var files = passwordManager.GetPasswordFiles().ToList();
			var directories = files
				.SelectMany(f => f.Directory.EnumerateParentsUpTo(passwordManager.PasswordStore))
				.DistinctBy(d => d.FullName)
				.ConcatSingle(passwordManager.PasswordStore)
				.OrderBy(p => p.FullName)
				.ToList();

			var selection = dialogCreator
				.ShowPasswordMenu(directories, k => PathUtilities.MakeRelativePathForDisplay(passwordManager.PasswordStore, k), "Select a directory to re-encrypt...")
				.ValueOrDefault();
			if (selection == null)
			{
				return;
			}

			var viewer = new LogViewer("Re-encryption progress", "Re-encrypting the password store...\n");
			viewer.Show();
			Task.Run(() =>
			{
				foreach (var file in files.Where(f => selection.IsChild(f.FileInfo)))
				{
					try
					{
						ReencryptSingleFile(file, viewer);
					}
					catch (Exception e)
					{
						var message = $"Failed to re-encrypt {file.FullPath}. An error occurred.\n\n" +
							$"{e.GetType().Name}: {e.Message}";
						viewer.AddMessage(message);
						if (!dialogService.ShowYesNoWindow($"{message}\n\nDo you want to continue?", "An error occurred.", MessageBoxImage.Error))
						{
							viewer.AddMessage("Re-encryption aborted.");
							return;
						}
					}
				}
				viewer.AddMessage("Re-encryption finished.");
			});
		}

		private void ReencryptSingleFile(PasswordFile file, LogViewer viewer)
		{
			var existingRecipients = cryptoService.GetRecipients(file.FullPath);
			var requestedRecipients = recipientFinder.FindRecipients(file)
				.Select(cryptoService.FindShortKeyId)
				.Where(i => i != null)
				.ToList();

			var removed = existingRecipients.Except(requestedRecipients).ToList();
			var added = requestedRecipients.Except(existingRecipients).ToList();
			if (removed.Any() || added.Any())
			{
				var removedKeys = string.Join(", ", removed);
				var addedKeys = string.Join(", ", added);
				var decrypted = passwordManager.DecryptPassword(file, false);
				passwordManager.EncryptPassword(decrypted);
				viewer.AddMessage($"Re-encrypted {file.FullPath} (removed: [{removedKeys}], added: [{addedKeys}])");
			}
			else
			{
				viewer.AddMessage($"Skipped {file.FullPath} (file already encrypted to required recipients)");
			}
		}
	}
}
