using PassWinmenu.ExternalPrograms.Gpg;

namespace PassWinmenuTests.Utilities
{
	internal class GpgInstallationBuilder
	{

		public GpgInstallation Build()
		{
			var fileSystem = new MockFileSystemBuilder().Build();
			return new GpgInstallation
			(
				fileSystem.DirectoryInfo.New(@"C:\gpg\bin"),
				fileSystem.FileInfo.New(@"C:\gpg\bin\gpg.exe"),
				fileSystem.FileInfo.New(@"C:\gpg\bin\gpg-agent.exe"),
				fileSystem.FileInfo.New(@"C:\gpg\bin\gpg-connect-agent.exe"),
				fileSystem.FileInfo.New(@"C:\gpg\bin\gpgconf.exe"),
				fileSystem.FileInfo.New(@"C:\gpg\bin\gpg-preset-passphrase.exe")
			);
		}
	}
}
