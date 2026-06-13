using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace PassWinmenuTests.Utilities
{
	public class MockFileSystemBuilder
	{
		private readonly Dictionary<string, MockFileData> files;

		public MockFileSystemBuilder()
		{
			files = new Dictionary<string, MockFileData>
			{
				{@"C:\gpg\bin\gpg.exe", new MockFileData(string.Empty) },
				{@"C:\gpg\bin\gpg-agent.exe", new MockFileData(string.Empty) },
				{@"C:\gpg\bin\gpg-connect-agent.exe", new MockFileData(string.Empty) },
			};
		}

		public MockFileSystemBuilder WithEmptyFile(string path)
		{
			files[path] = new MockFileData(string.Empty);
			return this;
		}

		public MockFileSystemBuilder WithDirectory(string path)
		{
			files[path] = new MockDirectoryData();
			return this;
		}

		public MockFileSystemBuilder WithFile(string path, string content)
		{
			files[path] = new MockFileData(content);
			return this;
		}

		public MockFileSystem Build()
		{
			return new MockFileSystem(files);
		}
	}
}
