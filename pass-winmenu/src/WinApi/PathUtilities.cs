using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using PassWinmenu.Utilities.ExtensionMethods;

namespace PassWinmenu.WinApi
{
	// TODO: Bring functionality into PathDisplayService and deal with nullability properly.
	internal static class PathUtilities
	{
		/// <summary>
		/// Reformats a file path as relative to the specified base directory.
		/// If the path does not point to a child of the base directory,
		/// the full path is returned.
		/// This function is intended for usage in situations where relative
		/// paths are displayed to the user, so it produces UNIX-style
		/// directory separators in its output.
		/// </summary>
		public static string MakeRelativePathForDisplay(IDirectoryInfo baseDir, string child)
		{
			var fileInfo = baseDir.FileSystem.FileInfo.New(child);
			return MakeRelativePathForDisplay(baseDir, fileInfo);
		}

		/// <summary>
		/// Reformats a file path as relative to the specified base directory.
		/// If the path does not point to a child of the base directory,
		/// the full path is returned.
		/// This function is intended for usage in situations where relative
		/// paths are displayed to the user, so it produces UNIX-style
		/// directory separators in its output.
		/// </summary>
		public static string MakeRelativePathForDisplay(IDirectoryInfo baseDir, IFileInfo child)
		{
			if (!baseDir.IsChild(child))
			{
				return child.FullName;
			}
			var parent = child.Directory!;
			var entries = new List<string>();
			entries.Insert(0, child.Name);
			while (!parent.PathEquals(baseDir))
			{
				entries.Insert(0, parent.Name);
				parent = parent.Parent!;
			}
			return string.Join("/", entries);
		}

		/// <summary>
		/// Reformats a directory path as relative to the specified base directory.
		/// If the path does not point to a child of the base directory,
		/// the full path is returned.
		/// This function is intended for usage in situations where relative
		/// paths are displayed to the user, so it produces UNIX-style
		/// directory separators in its output.
		/// </summary>
		public static string MakeRelativePathForDisplay(IDirectoryInfo baseDir, IDirectoryInfo child)
		{
			if (!baseDir.IsChildOrSelf(child))
			{
				return child.FullName;
			}

			var path = new StringBuilder();
			var current = child;
			while (!current.PathEquals(baseDir))
			{
				path.Insert(0, current.Name + "/");
				current = current.Parent!;
			}
			if (path.Length == 0)
			{
				return ".";
			}
			return path.ToString();
		}

		/// <summary>
		/// Normalises a directory, replacing all AltDirectorySeparatorChars with DirectorySeparatorChars
		/// and stripping any trailing directory separators.
		/// </summary>
		/// <param name="directory">The directory to be normalised.</param>
		/// <returns>The normalised directory.</returns>
		internal static string NormaliseDirectory(string directory)
		{
			var normalised = directory.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			var stripped = normalised.TrimEnd(Path.DirectorySeparatorChar);
			return stripped;
		}

	}
}
