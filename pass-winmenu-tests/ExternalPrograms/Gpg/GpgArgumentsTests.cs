using PassWinmenu.ExternalPrograms.Gpg;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.ExternalPrograms.Gpg
{
	public class GpgArgumentsTests
	{
		[Theory]
		[InlineData("file", "\"file\"")]
		[InlineData("a b", "\"a b\"")]
		[InlineData("", "\"\"")]
		[InlineData(@"C:\Program Files\gpg", "\"C:\\Program Files\\gpg\"")]
		public void Quote_WrapsValueInQuotes(string input, string expected)
		{
			GpgArguments.Quote(input).ShouldBe(expected);
		}

		[Fact]
		public void Quote_EscapesEmbeddedQuote()
		{
			// a"b  ->  "a\"b"
			GpgArguments.Quote("a\"b").ShouldBe("\"a\\\"b\"");
		}

		[Fact]
		public void Quote_DoublesTrailingBackslash()
		{
			// a\  ->  "a\\"  (so the backslash doesn't escape the closing quote)
			GpgArguments.Quote("a\\").ShouldBe("\"a\\\\\"");
		}

		[Fact]
		public void Quote_PreventsArgumentInjection()
		{
			// An attempt to break out of the quotes and inject extra gpg flags must stay inside a
			// single quoted token, with the injected quote escaped.
			GpgArguments.Quote("x\" --output evil").ShouldBe("\"x\\\" --output evil\"");
		}
	}
}
