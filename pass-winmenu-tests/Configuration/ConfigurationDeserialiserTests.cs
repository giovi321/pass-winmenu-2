using System.IO;
using System.Linq;
using System.Text;
using PassWinmenu.Configuration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Configuration
{
	public class ConfigurationDeserialiserTests
	{
		[Fact]
		public void Deserialise_IntoHotkey_NullValue_RemovesNullValue()
		{
			var source = @"
hotkeys:
  -
  - null
  - hotkey: ctrl alt p
    action: decrypt-password";

			var config = ConfigurationDeserialiser.Deserialise<Config>(source.IntoReader());
			var hotkeys = config.Hotkeys.ToList();

			hotkeys.Count.ShouldBe(1);
			hotkeys[0].Action.ShouldBe(HotkeyAction.DecryptPassword);
		}
	}

	internal static class StringExtensions
	{
		internal static StreamReader IntoReader(this string source)
		{
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(source));
			return new StreamReader(stream);
		}

	}
}
