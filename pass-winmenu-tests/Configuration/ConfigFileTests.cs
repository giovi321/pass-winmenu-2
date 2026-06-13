using System.IO;
using Xunit;
using YamlDotNet.Serialization;

namespace PassWinmenuTests.Configuration
{
		public class ConfigFileTests
	{
		[Fact]
		public void ConfigFile_IsValidYaml()
		{
			var des = new DeserializerBuilder()
				.Build();

			// Will throw an exception if the file does not contain valid YAML
			des.Deserialize(File.OpenText(@"..\..\..\..\pass-winmenu\embedded\default-config.yaml"));
		}
	}
}
