using PassWinmenu.ExternalPrograms.Gpg;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.ExternalPrograms.Gpg
{
	public class GpgKeygripResolverTests
	{
		// Helper: build a colon record with the keygrip at field 9.
		private static string Grp(string keygrip) =>
			string.Join(':', new[] { "grp", "", "", "", "", "", "", "", "", keygrip });

		// Helper: build a sec/ssb record with capabilities at field 11.
		private static string Key(string type, string keyId, string caps) =>
			string.Join(':', new[] { type, "u", "4096", "1", keyId, "1700000000", "", "", "", "", "", caps });

		[Fact]
		public void ParseEncryptionKeygrip_SelectsEncryptionSubkeyNotPrimary()
		{
			// Primary key is sign+certify (caps "scESC", uppercase E describes the whole key);
			// the subkey is the encryption key (lowercase "e").
			var output = string.Join("\n",
				Key("sec", "PRIMARYID", "scESC"),
				Grp("PRIMARY_GRIP"),
				Key("ssb", "SUBKEYID", "e"),
				Grp("ENCRYPTION_GRIP"));

			GpgKeygripResolver.ParseEncryptionKeygrip(output).ShouldBe("ENCRYPTION_GRIP");
		}

		[Fact]
		public void ParseEncryptionKeygrip_NoEncryptionKey_FallsBackToFirstKeygrip()
		{
			var output = string.Join("\n",
				Key("sec", "PRIMARYID", "sc"),
				Grp("PRIMARY_GRIP"));

			GpgKeygripResolver.ParseEncryptionKeygrip(output).ShouldBe("PRIMARY_GRIP");
		}

		[Fact]
		public void ParseEncryptionKeygrip_HandlesCrLfLineEndings()
		{
			var output = string.Join("\r\n",
				Key("sec", "PRIMARYID", "scESC"),
				Grp("PRIMARY_GRIP"),
				Key("ssb", "SUBKEYID", "e"),
				Grp("ENCRYPTION_GRIP"));

			GpgKeygripResolver.ParseEncryptionKeygrip(output).ShouldBe("ENCRYPTION_GRIP");
		}

		[Fact]
		public void ParseEncryptionKeygrip_EmptyOutput_ReturnsNull()
		{
			GpgKeygripResolver.ParseEncryptionKeygrip("").ShouldBeNull();
		}
	}
}
