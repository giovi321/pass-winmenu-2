using System.Collections.Generic;
using System.IO.Abstractions;
using PassWinmenu.ExternalPrograms;

namespace PassWinmenuTests.Utilities
{
	internal class FakeCryptoService : ICryptoService
	{
		private readonly IFileSystem fileSystem;

		public FakeCryptoService(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public string Decrypt(string file)
		{
			return fileSystem.File.ReadAllText(file);
		}

		public void Encrypt(string data, string outputFile, bool overwrite, params string[] recipients)
		{
			fileSystem.File.WriteAllText(outputFile, data);
		}

		public string FindShortKeyId(string target)
		{
			return string.Empty;
		}

		public List<string> GetRecipients(string file)
		{
			return new List<string>();
		}
	}
}
