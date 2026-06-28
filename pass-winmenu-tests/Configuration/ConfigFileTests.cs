using System.IO;
using PassWinmenu.Configuration;
using Shouldly;
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

		[Fact]
		public void ConfigFile_SpecialCharactersBlock_Deserialises()
		{
			using var reader = File.OpenText(@"..\..\..\..\pass-winmenu\embedded\default-config.yaml");
			var config = ConfigurationDeserialiser.Deserialise<Config>(reader);

			var sc = config.PasswordStore.PasswordGeneration.SpecialCharacters;
			sc.Characters.ShouldBe("!@#$%^&*");
			sc.Placement.ShouldBe(SpecialCharacterPlacement.End);
		}
	}
}
