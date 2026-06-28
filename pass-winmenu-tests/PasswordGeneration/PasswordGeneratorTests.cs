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

		[Fact]
		public void GeneratePassword_SpecialCharactersEnd_AppendsConfiguredCount()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 10,
				SpecialCharacters = new SpecialCharacterConfig
				{
					Enabled = true,
					Characters = "!",
					Count = 2,
					Placement = SpecialCharacterPlacement.End,
				},
			};

			var pw = new PasswordGenerator(options).GeneratePassword();

			pw!.Length.ShouldBe(12);
			pw.EndsWith("!!").ShouldBeTrue();
		}

		[Fact]
		public void GeneratePassword_SpecialCharactersStart_PrependsConfiguredCount()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 10,
				SpecialCharacters = new SpecialCharacterConfig
				{
					Enabled = true,
					Characters = "!",
					Count = 1,
					Placement = SpecialCharacterPlacement.Start,
				},
			};

			var pw = new PasswordGenerator(options).GeneratePassword();

			pw!.Length.ShouldBe(11);
			pw.StartsWith("!").ShouldBeTrue();
		}

		[Fact]
		public void GeneratePassword_SpecialCharactersDisabled_DoesNotChangeLength()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 10,
				SpecialCharacters = new SpecialCharacterConfig { Enabled = false, Characters = "!", Count = 3 },
			};

			new PasswordGenerator(options).GeneratePassword()!.Length.ShouldBe(10);
		}

		[Fact]
		public void GeneratePassword_WithTargetLength_OverridesConfigLength()
		{
			var options = new PasswordGenerationConfig { Length = 20 };

			new PasswordGenerator(options).GeneratePassword(15, includeSpecialCharacters: false)!.Length.ShouldBe(15);
		}

		[Fact]
		public void GenerateBase_DoesNotApplySpecialCharacters()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 10,
				SpecialCharacters = new SpecialCharacterConfig
				{
					Enabled = true,
					Characters = "!",
					Count = 5,
					Placement = SpecialCharacterPlacement.End,
				},
			};

			new PasswordGenerator(options).GenerateBase(10)!.Length.ShouldBe(10);
		}

		[Fact]
		public void ApplySpecialCharacters_PreservesBaseAndAppendsAtEnd()
		{
			var options = new PasswordGenerationConfig
			{
				SpecialCharacters = new SpecialCharacterConfig
				{
					Characters = "!",
					Count = 2,
					Placement = SpecialCharacterPlacement.End,
				},
			};
			var generator = new PasswordGenerator(options);
			var basePassword = generator.GenerateBase(12);

			// Toggling specials must keep the same base and only add to it.
			generator.ApplySpecialCharacters(basePassword!).ShouldBe(basePassword + "!!");
		}

		[Fact]
		public void ComputeXkcdWordCount_GrowsWithTargetLength()
		{
			var options = new PasswordGenerationConfig { Style = PasswordGenerationStyle.Xkcd };
			options.Xkcd.MinWordLength = 4;
			options.Xkcd.MaxWordLength = 8;
			options.Xkcd.Separator = "-";
			var generator = new PasswordGenerator(options);

			generator.ComputeXkcdWordCount(8).ShouldBeGreaterThanOrEqualTo(1);
			generator.ComputeXkcdWordCount(64).ShouldBeGreaterThan(generator.ComputeXkcdWordCount(16));
		}
	}
}
