using System.IO;
using System.IO.Abstractions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PassWinmenu.Utilities
{
	/// <summary>
	/// Writes files that hold secret material (e.g. the biometric blob) with an
	/// owner-only ACL, so other users on the same machine cannot read them even if
	/// the containing directory grants broader access.
	/// </summary>
	internal static class OwnerOnlyFile
	{
		public static void WriteAllBytes(IFileSystem fileSystem, string path, byte[] content)
		{
			// Only the real filesystem supports ACLs; mock filesystems in tests take the plain path.
			if (fileSystem is not FileSystem)
			{
				fileSystem.File.WriteAllBytes(path, content);
				return;
			}

			var security = new FileSecurity();
			// Protect the file from inheriting parent directory permissions.
			security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
			security.AddAccessRule(new FileSystemAccessRule(
				WindowsIdentity.GetCurrent().User!,
				FileSystemRights.FullControl,
				AccessControlType.Allow));

			var fileInfo = new FileInfo(path);
			using (var stream = fileInfo.Create(FileMode.Create, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.None, security))
			{
				stream.Write(content, 0, content.Length);
			}
		}
	}
}
