using System.IO;
using PassWinmenu.Configuration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Configuration
{
	public class ConfigManagerTests
	{
		[Fact]
		public void Load_EmptyFile_NeedsUpgrade()
		{
			var tempFile = Path.GetTempFileName();
			var result = ConfigManager.Load(tempFile, allowCreate: false);
			File.Delete(tempFile);

			result.ShouldBeOfType<LoadResult.NeedsUpgrade>();
		}

		[Fact]
		public void Load_NonexistentFileAndCreationAllowed_Created()
		{
			var tempFile = Path.GetTempFileName();
			File.Delete(tempFile);

			var result = ConfigManager.Load(tempFile, allowCreate: true);
			File.Delete(tempFile);

			result.ShouldBeOfType<LoadResult.NewFileCreated>();
		}
		
		[Fact]
		public void Load_NonexistentFileAndCreationNotAllowed_NotFound()
		{
			var tempFile = Path.GetTempFileName();
			File.Delete(tempFile);

			var result = ConfigManager.Load(tempFile, allowCreate: false);

			result.ShouldBeOfType<LoadResult.NotFound>();
		}
	}
}
