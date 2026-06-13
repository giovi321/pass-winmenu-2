using PassWinmenu.ExternalPrograms.Gpg;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.ExternalPrograms.Gpg
{
	public class PercentEscapeTests
	{
		[Fact]
		public void UnEscape_NoPercentsInText_DoesNothing()
		{
			var result = PercentEscape.UnEscape("abc");

			result.ShouldBe("abc");
		}

		[Fact]
		public void UnEscape_TextContainsEscapedBytes_ReplacesThem()
		{
			var result = PercentEscape.UnEscape("a%3ab");

			result.ShouldBe("a:b");
		}


		[Fact]
		public void UnEscape_TextContainsMultipleEscapedBytes_ReplacesThem()
		{
			var result = PercentEscape.UnEscape("a%3ab\n\r\nc%3ad");

			result.ShouldBe("a:b\n\r\nc:d");
		}
	}
}
