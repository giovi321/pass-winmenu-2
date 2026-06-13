using PassWinmenu.WinApi;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.WinApi
{
	public class PathUtilitiesTests
	{
		[Fact]
		public void MakeRelativePathForDisplay_IsChildPath_MakesRelativeToBase()
		{
			var filesystem = new MockFileSystemBuilder().Build();

			var parent = filesystem.DirectoryInfo.New(@"C:\parent");
			var child = @"C:\parent\child";

			PathUtilities.MakeRelativePathForDisplay(parent, child).ShouldBe("child");
		}

		[Fact]
		public void MakeRelativePathForDisplay_TrailingSlash_PreservesTrailingSlash()
		{
			var filesystem = new MockFileSystemBuilder().Build();

			var parent = filesystem.DirectoryInfo.New(@"C:\parent");
			var child = @"C:\parent\child\";

			PathUtilities.MakeRelativePathForDisplay(parent, child).ShouldBe("child/");
		}

		[Fact]
		public void MakeRelativePathForDisplay_SamePath_ProducesSingleDot()
		{
			var filesystem = new MockFileSystemBuilder().Build();

			var parent = filesystem.DirectoryInfo.New(@"C:\parent");
			var child = filesystem.DirectoryInfo.New(@"C:\parent");

			PathUtilities.MakeRelativePathForDisplay(parent, child).ShouldBe(".");
		}

		[Fact]
		public void MakeRelativePathForDisplay_NotAParent_ReturnsFullPath()
		{
			var filesystem = new MockFileSystemBuilder().Build();

			var parent = filesystem.DirectoryInfo.New(@"C:\parent");
			var child = filesystem.DirectoryInfo.New(@"C:\child");

			PathUtilities.MakeRelativePathForDisplay(parent, child).ShouldBe(@"C:\child");
		}

		[Fact]
		public void MakeRelativePathForDisplay_Directory_AddsTrailingSlash()
		{
			var filesystem = new MockFileSystemBuilder().Build();

			var parent = filesystem.DirectoryInfo.New(@"C:\parent");
			var child = filesystem.DirectoryInfo.New(@"C:\parent\sub\child");

			PathUtilities.MakeRelativePathForDisplay(parent, child).ShouldBe("sub/child/");
		}

		[Fact]
		public void MakeRelativePathForDisplay_File_NoTrailingSlash()
		{
			var filesystem = new MockFileSystemBuilder().Build();

			var parent = filesystem.DirectoryInfo.New(@"C:\parent");
			var child = filesystem.FileInfo.New(@"C:\parent\sub\child");

			PathUtilities.MakeRelativePathForDisplay(parent, child).ShouldBe("sub/child");
		}
	}
}
