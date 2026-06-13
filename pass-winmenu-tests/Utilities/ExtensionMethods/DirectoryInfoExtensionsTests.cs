using PassWinmenu.Utilities.ExtensionMethods;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Utilities.ExtensionMethods
{
	public class DirectoryInfoExtensionsTests
	{
		[Fact]
		public void IsChildOrSelf_IsChild_ReturnsTrue()
		{
			var fs = new MockFileSystemBuilder()
				.WithDirectory("C:/parent")
				.WithDirectory("C:/parent/sub/child")
				.Build();
			var parent = fs.DirectoryInfo.New("C:/parent");
			var child = fs.DirectoryInfo.New("C:/parent/sub/child");

			parent.IsChildOrSelf(child).ShouldBeTrue();

		}

		[Fact]
		public void IsChildOrSelf_IsSelf_ReturnsTrue()
		{
			var fs = new MockFileSystemBuilder()
				.WithDirectory("C:/parent")
				.Build();
			var parent = fs.DirectoryInfo.New("C:/parent");
			var child = fs.DirectoryInfo.New("C:/parent");

			parent.IsChildOrSelf(child).ShouldBeTrue();
		}

		[Fact]
		public void IsChildOrSelf_IsSibling_ReturnsFalse()
		{
			var fs = new MockFileSystemBuilder()
				.WithDirectory("C:/parent")
				.WithDirectory("C:/sibling")
				.Build();
			var parent = fs.DirectoryInfo.New("C:/parent");
			var sibling = fs.DirectoryInfo.New("C:/sibling");
			parent.IsChildOrSelf(sibling).ShouldBeFalse();
		}

		[Fact]
		public void IsChild_IsChild_ReturnsTrue()
		{
			var fs = new MockFileSystemBuilder()
				.WithDirectory("C:/parent")
				.WithEmptyFile("C:/parent/child")
				.Build();
			var parent = fs.DirectoryInfo.New("C:/parent");
			var child = fs.FileInfo.New("C:/parent/child");

			parent.IsChild(child).ShouldBeTrue();
		}
	}
}
