using System;
using System.IO.Abstractions;
using PassWinmenu.Utilities;
using PassWinmenu.Utilities.ExtensionMethods;

#nullable enable
namespace PassWinmenu.PasswordManagement
{
	internal class GpgRecipientFinder : IRecipientFinder
	{
		internal const string GpgIdFileName = ".gpg-id";

		private readonly IDirectoryInfo passwordStore;
		private readonly EnvironmentVariables environmentVariables;
		private readonly IFileSystem fileSystem;

		public GpgRecipientFinder(IDirectoryInfo passwordStore, EnvironmentVariables environmentVariables)
		{
			this.passwordStore = passwordStore;
			this.environmentVariables = environmentVariables;
			fileSystem = passwordStore.FileSystem;
		}

		public string[] FindRecipients(PasswordFile file)
		{
			if (!string.IsNullOrWhiteSpace(environmentVariables.PasswordStoreKey))
			{
				return environmentVariables.PasswordStoreKey!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			}

			var current = file.Directory;

			// Walk up from the innermost directory, and keep moving up until an existing directory 
			// containing a gpg-id file is found.
			while (!current.Exists || !current.ContainsFile(GpgIdFileName))
			{
				if (current.Parent == null || current.PathEquals(passwordStore))
				{
					return Array.Empty<string>();
				}
				current = current.Parent;
			}

			return fileSystem.File.ReadAllLines(fileSystem.Path.Combine(current.FullName, GpgIdFileName));
		}
	}
}
