using System.Linq;
using System.Text.RegularExpressions;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordGeneration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.PasswordGeneration
{
	public class XkcdPasswordGeneratorTests
	{
		// Lengths: alpha=5, bravo=5, charlie=7, delta=5, echo=4, foxtrot=7
		private static readonly string[] Words = { "alpha", "bravo", "charlie", "delta", "echo", "foxtrot" };

		private static XkcdConfig Config(XkcdCapitalisation caps = XkcdCapitalisation.None) => new()
		{
			WordCount = 4,
			Separator = "-",
			Capitalisation = caps,
			MinWordLength = 1,
			MaxWordLength = 99,
		};

		[Fact]
		public void GeneratesRequestedNumberOfWordsFromTheList()
		{
			var pw = new XkcdPasswordGenerator(Config(), Words).GeneratePassword();

			pw.ShouldNotBeNull();
			var parts = pw!.Split('-');
			parts.Length.ShouldBe(4);
			parts.All(w => Words.Contains(w)).ShouldBeTrue();
		}

		[Fact]
		public void Capitalisation_First_TitleCasesEachWord()
		{
			var cfg = Config(XkcdCapitalisation.First);
			cfg.Separator = " ";

			var pw = new XkcdPasswordGenerator(cfg, Words).GeneratePassword();

			pw!.Split(' ').All(w => char.IsUpper(w[0])).ShouldBeTrue();
		}

		[Fact]
		public void Capitalisation_Upper_UppercasesEverything()
		{
			var pw = new XkcdPasswordGenerator(Config(XkcdCapitalisation.Upper), Words).GeneratePassword();

			pw!.ShouldBe(pw.ToUpperInvariant());
		}

		[Fact]
		public void Capitalisation_Random_MakesEachWordEntirelyUpperOrLowerCase()
		{
			var cfg = Config(XkcdCapitalisation.Random);
			cfg.WordCount = 8;
			cfg.Separator = " ";

			// Run several times to exercise the per-word random branch.
			for (var attempt = 0; attempt < 50; attempt++)
			{
				var pw = new XkcdPasswordGenerator(cfg, Words).GeneratePassword();

				var parts = pw!.Split(' ');
				parts.Length.ShouldBe(8);
				parts.All(w => w == w.ToUpperInvariant() || w == w.ToLowerInvariant()).ShouldBeTrue();
			}
		}

		[Fact]
		public void RandomNumberSeparator_WrapsEachWordWithADigit()
		{
			var cfg = Config();
			cfg.WordCount = 5;
			cfg.RandomNumberSeparator = true;

			// <digit><word><digit><word>...<word><digit> — a digit before each word and a trailing one.
			var pattern = new Regex("^([0-9][a-z]+)+[0-9]$");

			for (var attempt = 0; attempt < 50; attempt++)
			{
				var pw = new XkcdPasswordGenerator(cfg, Words).GeneratePassword();

				pw.ShouldNotBeNull();
				pattern.IsMatch(pw!).ShouldBeTrue($"'{pw}' should be digit-word-...-word-digit");
				// One digit before each word plus a trailing digit.
				pw!.Count(char.IsDigit).ShouldBe(6);
			}
		}

		[Fact]
		public void RandomNumberSeparator_OverridesIncludeNumber()
		{
			var cfg = Config();
			cfg.WordCount = 2;
			cfg.RandomNumberSeparator = true;
			cfg.IncludeNumber = true; // ignored in this mode

			// Exactly digit word digit word digit (no extra appended number).
			var pattern = new Regex("^[0-9][a-z]+[0-9][a-z]+[0-9]$");

			for (var attempt = 0; attempt < 50; attempt++)
			{
				var pw = new XkcdPasswordGenerator(cfg, Words).GeneratePassword();

				pw.ShouldNotBeNull();
				pattern.IsMatch(pw!).ShouldBeTrue($"'{pw}' should be digit word digit word digit");
			}
		}

		[Fact]
		public void IncludeNumber_AppendsATwoDigitNumber()
		{
			var cfg = Config();
			cfg.WordCount = 2;
			cfg.IncludeNumber = true;

			var pw = new XkcdPasswordGenerator(cfg, Words).GeneratePassword();

			var parts = pw!.Split('-');
			parts.Length.ShouldBe(3);
			int.TryParse(parts[2], out var number).ShouldBeTrue();
			number.ShouldBeInRange(10, 99);
		}

		[Fact]
		public void WordLengthFilter_RestrictsThePool()
		{
			var cfg = Config();
			cfg.WordCount = 5;
			cfg.MinWordLength = 7;
			cfg.MaxWordLength = 7;

			var pw = new XkcdPasswordGenerator(cfg, Words).GeneratePassword();

			pw!.Split('-').All(w => w == "charlie" || w == "foxtrot").ShouldBeTrue();
		}

		[Fact]
		public void EmptyWordList_ReturnsNull()
		{
			new XkcdPasswordGenerator(Config(), new string[0]).GeneratePassword().ShouldBeNull();
		}
	}
}
