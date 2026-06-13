using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using PassWinmenu.WinApi;

namespace PassWinmenu.Utilities
{
	internal class DirectoryAutocomplete
	{
		private readonly IDirectoryInfo baseDirectoryInfo;
		private readonly string baseDirectory;

		public DirectoryAutocomplete(IDirectoryInfo baseDirectory)
		{
			baseDirectoryInfo = baseDirectory;
			// Ensure consistency of directory separators.
			// We can't use Path.Combine() here because it doesn't concatenate drive letters properly.
			this.baseDirectory = string.Join(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), baseDirectoryInfo.FullName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));
			// Append a directory separator so Path.GetDirectoryName(baseDirectory) will correctly return the full base directory, instead of its parent directory.
			this.baseDirectory += Path.DirectorySeparatorChar;
		}

		public List<string> GetCompletionList(string input)
		{
			// Ensure the directory separators in the input string are correct
			input = Path.Combine(input.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));

			var fullPath = Path.Combine(baseDirectory, input);
			var directory = Path.GetDirectoryName(fullPath);
			var file = Path.GetFileName(fullPath);
			
			// If the directory to look in doesn't exist, we can't suggest anything for it.
			if (!Directory.Exists(directory))
			{
				return new List<string>();
			}

			var suggestions = SuggestionsFor(directory, file + "*");

			// If we have no suggestions, try showing suggestions for just the parent directory.
			if (!suggestions.Any())
			{
				suggestions = SuggestionsFor(directory, "*");
			}
			// If we have only one suggestion and that suggestion is a directory, 
			// add suggestions for the files inside that directory.
			else if (suggestions.Count == 1 && Directory.Exists(suggestions.First()))
			{
				suggestions.AddRange(SuggestionsFor(suggestions.First(), "*"));
			}

			// Append a directory separator char to all directories to make it clear we're suggesting a directory, not a file,
			// and transform directory suggestions to relative paths for convenience.
			return suggestions.Select(AddSeparatorCharIfDirectory).Select(MakeRelative).ToList();
		}

		private string MakeRelative(string suggestion)
		{
			return PathUtilities.MakeRelativePathForDisplay(baseDirectoryInfo, suggestion);
		}

		private string AddSeparatorCharIfDirectory(string suggestion)
		{
			if (Directory.Exists(suggestion))
			{
				return suggestion + Path.DirectorySeparatorChar;
			}

			return suggestion;
		}

		private static List<string> SuggestionsFor(string directory, string searchPattern)
		{
			return Directory.GetFileSystemEntries(directory, searchPattern)
				// Dotfiles should be filtered out.
				.Where(suggestion => !Path.GetFileName(suggestion).StartsWith(".", StringComparison.Ordinal))
				.ToList();
		}
	}
}
