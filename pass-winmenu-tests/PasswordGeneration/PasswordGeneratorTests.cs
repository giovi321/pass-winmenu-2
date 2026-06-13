using PassWinmenu.Configuration;
using PassWinmenu.PasswordGeneration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.PasswordGeneration
{
	public class PasswordGeneratorTests
	{
		[Fact]
		public void GeneratePassword_MatchesRequiredLength()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 32
			};
			var generator = new PasswordGenerator(options);
			var password = generator.GeneratePassword();

			password.Length.ShouldBe(32);

		}

		[Fact]
		public void GeneratePassword_NoCharacterGroups_Null()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 32,
				CharacterGroups = new CharacterGroupConfig[0]
			};
			var generator = new PasswordGenerator(options);
			var password = generator.GeneratePassword();

			password.ShouldBeNull();
		}

		[Theory]
		[InlineData("0123456789")]
		[InlineData("abcABC")]
		[InlineData("1")]
		public void GeneratePassword_OnlyContainsAllowedCharacters(string allowedCharacters)
		{
			var options = new PasswordGenerationConfig
			{
				CharacterGroups = new []
				{
					new CharacterGroupConfig("test", allowedCharacters, true), 
				}
			};
			var generator = new PasswordGenerator(options);
			var password = generator.GeneratePassword();

			password.ShouldBeSubsetOf(allowedCharacters);
		}
	}
}
