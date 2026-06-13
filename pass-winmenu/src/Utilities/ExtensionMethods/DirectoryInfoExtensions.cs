using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class DirectoryInfoExtensions
	{
		/// <summary>
		/// Checks whether the directory contains the given file.
		/// </summary>
		/// <returns>True if the directory contains the file, false otherwise.</returns>
		internal static bool ContainsFile(this IDirectoryInfo directory, string name)
		{
			return directory.EnumerateFiles(name).Any();
		}

		internal static bool IsChildOrSelf(this IDirectoryInfo parent, IDirectoryInfo candidate)
		{
			var current = candidate;
			do
			{
				if (current.PathEquals(parent))
				{
					return true;
				}
				current = current.Parent;
			} while (current != null);

			return false;
		}

		internal static bool IsChild(this IDirectoryInfo directory, IFileInfo child)
		{
			return IsChildOrSelf(directory, child.Directory ?? throw new Exception($"Unable to find parent directory of '{child.FullName}'"));
		}

		public static IEnumerable<IDirectoryInfo> EnumerateParentsUpTo(this IDirectoryInfo directory, IDirectoryInfo parent)
		{
			var current = directory;
			while (current != null && !current.PathEquals(parent))
			{
				yield return current;
				current = current.Parent;
			}
		}
	}
}
