using System.IO.Abstractions;
using PassWinmenu.WinApi;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class FileSystemInfoExtensions
	{
		/// <summary>
		/// Checks for path equality between two IFileSystemInfo objects.
		/// Unlike a direct comparison of their FullName properties,
		/// this method ignores trailing slashes.
		/// </summary>
		/// <returns>True if both objects point to the same path, false otherwise.</returns>
		internal static bool PathEquals(this IFileSystemInfo a, IFileSystemInfo b)
		{
			var pathA = PathUtilities.NormaliseDirectory(a.FullName);
			var pathB = PathUtilities.NormaliseDirectory(b.FullName);
			return pathA == pathB;
		}
	}
}
