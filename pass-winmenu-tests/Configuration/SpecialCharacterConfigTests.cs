using PassWinmenu.Configuration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Configuration
{
	public class SpecialCharacterConfigTests
	{
		[Fact]
		public void Defaults_AreSafeAndDisabled()
		{
			var sc = new PasswordGenerationConfig().SpecialCharacters;

			sc.ShouldNotBeNull();
			sc.Enabled.ShouldBeFalse();
			sc.Count.ShouldBe(1);
			sc.Characters.ShouldBe("!@#$%^&*");
			sc.Placement.ShouldBe(SpecialCharacterPlacement.End);
		}

		[Fact]
		public void Deserialise_ReadsAllFields()
		{
			var source = @"
password-store:
  password-generation:
    special-characters:
      enabled: true
      characters: '!@#'
      count: 3
      placement: random";

			var config = ConfigurationDeserialiser.Deserialise<Config>(source.IntoReader());
			var sc = config.PasswordStore.PasswordGeneration.SpecialCharacters;

			sc.Enabled.ShouldBeTrue();
			sc.Characters.ShouldBe("!@#");
			sc.Count.ShouldBe(3);
			sc.Placement.ShouldBe(SpecialCharacterPlacement.Random);
		}

		[Fact]
		public void Deserialise_WhenOmitted_UsesDefaults()
		{
			var source = @"
password-store:
  password-generation:
    length: 24";

			var config = ConfigurationDeserialiser.Deserialise<Config>(source.IntoReader());
			var sc = config.PasswordStore.PasswordGeneration.SpecialCharacters;

			sc.Enabled.ShouldBeFalse();
			sc.Placement.ShouldBe(SpecialCharacterPlacement.End);
		}
	}
}
