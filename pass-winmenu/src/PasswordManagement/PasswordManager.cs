using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.Utilities;

#nullable enable
namespace PassWinmenu.PasswordManagement
{
	internal class PasswordManager : IPasswordManager
	{
		private readonly ICryptoService cryptoService;
		private readonly IRecipientFinder recipientFinder;
		private readonly PasswordFileParser passwordFileParser;
		private readonly PasswordStoreConfig configuration;

		private IFileSystem FileSystem => PasswordStore.FileSystem;
		public IDirectoryInfo PasswordStore { get; }

		public PasswordManager(
			IDirectoryInfo passwordStore,
			ICryptoService cryptoService,
			IRecipientFinder recipientFinder,
			PasswordFileParser passwordFileParser,
			PasswordStoreConfig configuration)
		{
			PasswordStore = passwordStore;
			this.cryptoService = cryptoService;
			this.recipientFinder = recipientFinder;
			this.passwordFileParser = passwordFileParser;
			this.configuration = configuration;
		}

		/// <summary>
		/// Encrypts a password file at the specified path.
		/// If the path contains directories that do not exist, they will be created automatically.
		/// </summary>
		/// <param name="file">
		/// A <see cref="KeyedPasswordFile"/> instance specifying the contents
		/// of the password file to be encrypted.
		/// </param>
		public PasswordFile EncryptPassword(DecryptedPasswordFile file)
		{
			return EncryptPasswordInternal(file, true);
		}

		/// <summary>
		/// Adds a new password file at the specified path.
		/// </summary>
		/// <param name="path">A path, relative to the password store, indicating where the password should be created.</param>
		/// <param name="password">The password to be encrypted.</param>
		/// <param name="metadata">Any metadata that should be added.</param>
		/// <exception cref="InvalidOperationException">If a file already exists at the given location.</exception>
		public PasswordFile AddPassword(string path, string password, string? metadata)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}
			if (FileSystem.Path.IsPathRooted(path))
			{
				throw new ArgumentException("Path to the password file must be relative.");
			}

			var file = CreatePasswordFileFromPath(path);
			var parsed = new ParsedPasswordFile(file, password, metadata);
			return EncryptPasswordInternal(parsed, false);
		}
		
		/// <summary>
		/// Get the content from an encrypted password file.
		/// </summary>
		/// <param name="file">A <see cref="PasswordFile"/> specifying the password file to be decrypted.</param>
		/// <param name="passwordOnFirstLine">Should be true if the first line of the file contains the password.
		/// Any content in the remaining lines will be considered metadata.
		/// If set to false, the contents of the entire file are considered to be the password.</param>
		public KeyedPasswordFile DecryptPassword(PasswordFile file, bool passwordOnFirstLine)
		{
			if (!file.FileInfo.Exists)
			{
				throw new ArgumentException($"The password file \"{file.FullPath}\" does not exist.");
			}

			var content = cryptoService.Decrypt(file.FullPath);
			return passwordFileParser.Parse(file, content, !passwordOnFirstLine);
		}

		/// <summary>
		/// Returns all password files in the store
		/// </summary>
		/// <exception cref="DirectoryNotFoundException">If the password store directory does not exist.</exception>
		public IEnumerable<PasswordFile> GetPasswordFiles()
		{
			var patternRegex = new Regex(configuration.PasswordFileMatch);

			var files = PasswordStore.EnumerateFiles("*", SearchOption.AllDirectories);
			var matchingFiles = files.Where(f => patternRegex.IsMatch(f.Name));
			var passwordFiles = matchingFiles.Select(CreatePasswordFile);

			return passwordFiles;
		}

		public Option<PasswordFile> QueryPasswordFile(string path)
		{
			if (FileSystem.Path.IsPathRooted(path))
			{
				throw new ArgumentException("Path to the password file must be relative.");
			}

			var joined = FileSystem.Path.Join(PasswordStore.FullName, path);

			var fileInfo = FileSystem.FileInfo.New(joined);

			if (fileInfo.Exists)
			{
				return Option.Some(CreatePasswordFile(fileInfo));
			}

			return default;
		}

		private PasswordFile EncryptPasswordInternal(DecryptedPasswordFile file, bool overwrite)
		{
			file.Directory.Create();
			if (!overwrite && file.FileInfo.Exists)
			{
				throw new InvalidOperationException("A password file already exists at the specified location.");
			}
			cryptoService.Encrypt(file.Content, file.FullPath, overwrite, recipientFinder.FindRecipients(file));
			return new PasswordFile(file);
		}

		private PasswordFile CreatePasswordFile(IFileInfo file)
		{
			return new PasswordFile(file, PasswordStore);
		}

		private PasswordFile CreatePasswordFileFromPath(string relativePath)
		{
			var fullPath = FileSystem.Path.Combine(PasswordStore.FullName, relativePath);
			return new PasswordFile(FileSystem.FileInfo.New(fullPath), PasswordStore);
		}
	}
}
