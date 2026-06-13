using System;
using System.IO;
using System.IO.Abstractions;
using System.Windows;
using System.Windows.Controls;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities;

namespace PassWinmenu.Windows
{
	internal class FileSelectionWindow : SelectionWindow
	{
		private readonly DirectoryAutocomplete autocomplete;
		private readonly IDirectoryInfo baseDirectory;

		private bool hasSuggestionForEnteredFileName;

		public FileSelectionWindow(IDirectoryInfo baseDirectory, InterfaceConfig interfaceConfig,
			string hint) : base(interfaceConfig, hint)
		{
			this.baseDirectory = baseDirectory;
			autocomplete = new DirectoryAutocomplete(baseDirectory);
			var completions = autocomplete.GetCompletionList("");
			ResetItems(completions);
		}

		protected override void OnSearchTextChanged(object sender, TextChangedEventArgs e)
		{
			var query = SearchBox.Text;
			var completions = autocomplete.GetCompletionList(query);

			var isValidNonexistentFileName = !string.IsNullOrWhiteSpace(query)
				&& !SearchBox.Text.EndsWith("/", StringComparison.Ordinal)
				&& !completions.Contains(query);

			if (isValidNonexistentFileName)
			{
				completions.Insert(0, query);
			}

			hasSuggestionForEnteredFileName = isValidNonexistentFileName;
			ResetItems(completions);
		}

		protected override void HandleConfirm()
		{
			var selection = SelectionText;

			var existingFileSuggestionsStartAt = hasSuggestionForEnteredFileName ? 1 : 0;
			var hasSelectedExistingFileSuggestion = Labels.IndexOf(SelectedLabel) >= existingFileSuggestionsStartAt;

			if (hasSelectedExistingFileSuggestion)
			{
				selection = TrimEncryptedFileExtension(selection);
				SetSearchBoxText(selection);
				return;
			}

			if (SearchBox.Text.EndsWith("/", StringComparison.Ordinal))
			{
				return;
			}

			if (selection.EndsWith(Program.EncryptedFileExtension, StringComparison.Ordinal))
			{
				MessageBox.Show("A .gpg extension will be added automatically and does not need to be entered here.");
				selection = TrimEncryptedFileExtension(selection);
				SetSearchBoxText(selection);
				return;
			}

			if (File.Exists(Path.Combine(baseDirectory.FullName, selection + Program.EncryptedFileExtension)))
			{
				MessageBox.Show($"The password file \"{selection + Program.EncryptedFileExtension}\" already exists.");
				return;
			}

			Success = true;
			Close();
		}

		private string TrimEncryptedFileExtension(string selection)
		{
			if (selection.EndsWith(Program.EncryptedFileExtension, StringComparison.Ordinal))
			{
				return selection.Substring(0, selection.Length - Program.EncryptedFileExtension.Length);
			}

			return selection;
		}
	}
}
