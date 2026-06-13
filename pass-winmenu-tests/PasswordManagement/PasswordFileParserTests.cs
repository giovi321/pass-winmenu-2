using System.IO.Abstractions.TestingHelpers;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordManagement;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.PasswordManagement
{
	public class PasswordFileParserTests
	{
		private readonly PasswordFile dummyFile;
		private PasswordFileParser parser = new PasswordFileParser(new UsernameDetectionConfig());

		public PasswordFileParserTests()
		{
			var fileSystem = new MockFileSystem();
			var dirInfo = new MockDirectoryInfo(fileSystem, "\\password-store");
			var fileInfo = new MockFileInfo(fileSystem, "\\password-store\\dummy-password");
			dummyFile = new PasswordFile(fileInfo, dirInfo);
		}

		[Theory]
		[InlineData(-999, false)]
		[InlineData(-1, false)]
		[InlineData(-0, false)]
		[InlineData(1, false)]
		[InlineData(2, true)]
		[InlineData(3, true)]
		[InlineData(4, true)]
		[InlineData(100, true)]
		public void GetUsername_LineNumberInvalid_ThrowsException(int lineNumber, bool isValid)
		{
			var passwordFile = new ParsedPasswordFile(new PasswordFile(null, null), "password", "username");
			parser = new PasswordFileParser(new UsernameDetectionConfig
			{
				MethodString = "line-number",
				Options = new UsernameDetectionOptions
				{
					LineNumber = lineNumber
				}
			});

			if (isValid)
			{
				Should.NotThrow(() => parser.GetUsername(passwordFile));
			}
			else
			{
				Should.Throw<PasswordParseException>(() => parser.GetUsername(passwordFile));
			}
		}

		[Fact]
		public void Test_EmptyFile()
		{
			var text = "";
			var parsed = parser.Parse(dummyFile, text, false);

			Assert.Equal(parsed.Password, string.Empty);
			Assert.Equal(parsed.Metadata, string.Empty);
		}

		[Fact]
		public void Test_LineEndings_Metadata()
		{
			var crlf = "password\r\nmeta-data";
			var cr = "password\rmeta-data";
			var lf = "password\nmeta-data";

			var parsedCrlf = parser.Parse(dummyFile, crlf, false);
			Assert.Equal("password", parsedCrlf.Password);
			Assert.Equal("meta-data", parsedCrlf.Metadata);

			var parsedCr = parser.Parse(dummyFile, cr, false);
			Assert.Equal("password", parsedCr.Password);
			Assert.Equal("meta-data", parsedCr.Metadata);

			var parsedLf = parser.Parse(dummyFile, lf, false);
			Assert.Equal("password", parsedLf.Password);
			Assert.Equal("meta-data", parsedLf.Metadata);
		}

		[Fact]
		public void Test_LineEndings_PasswordOnly()
		{
			var crlf = "password\r\n";
			var cr = "password\r";
			var lf = "password\n";
			var none = "password";

			var parsedCrlf = parser.Parse(dummyFile, crlf, false);
			Assert.Equal("password", parsedCrlf.Password);
			Assert.Equal(parsedCrlf.Metadata, string.Empty);

			var parsedCr = parser.Parse(dummyFile, cr, false);
			Assert.Equal("password", parsedCr.Password);
			Assert.Equal(parsedCr.Metadata, string.Empty);

			var parsedLf = parser.Parse(dummyFile, lf, false);
			Assert.Equal("password", parsedLf.Password);
			Assert.Equal(parsedLf.Metadata, string.Empty);

			var parsedNone = parser.Parse(dummyFile, none, false);
			Assert.Equal("password", parsedNone.Password);
			Assert.Equal(parsedNone.Metadata, string.Empty);
		}

		[Fact]
		public void Test_Metadata_LineEndings()
		{
			const string crlf = "password\r\n" +
			                    "Username: user\r\n" +
			                    "Key: value";
			const string cr = "password\r" +
			                  "Username: user\r" +
			                  "Key: value";
			const string lf = "password\n" +
			                  "Username: user\n" +
			                  "Key: value";
			const string mixed = "password\r\n" +
			                     "Username: user\n" +
			                     "Key: value\r";

			var parsedCrlf = parser.Parse(dummyFile, crlf, false);
			parsedCrlf.Keys[0].Key.ShouldBe("Username");
			parsedCrlf.Keys[0].Value.ShouldBe("user");
			parsedCrlf.Keys[1].Key.ShouldBe("Key");
			parsedCrlf.Keys[1].Value.ShouldBe("value");

			var parsedCr = parser.Parse(dummyFile, cr, false);
			parsedCr.Keys[0].Key.ShouldBe("Username");
			parsedCr.Keys[0].Value.ShouldBe("user");
			parsedCr.Keys[1].Key.ShouldBe("Key");
			parsedCr.Keys[1].Value.ShouldBe("value");

			var parsedLf = parser.Parse(dummyFile, lf, false);
			parsedLf.Keys[0].Key.ShouldBe("Username");
			parsedLf.Keys[0].Value.ShouldBe("user");
			parsedLf.Keys[1].Key.ShouldBe("Key");
			parsedLf.Keys[1].Value.ShouldBe("value");

			var parsedMixed = parser.Parse(dummyFile, mixed, false);
			parsedMixed.Keys[0].Key.ShouldBe("Username");
			parsedMixed.Keys[0].Value.ShouldBe("user");
			parsedMixed.Keys[1].Key.ShouldBe("Key");
			parsedMixed.Keys[1].Value.ShouldBe("value");
		}

		[Fact]
		public void Test_Metadata_KeyFormat()
		{
			var content = "password\r\n" +
				"Username: user\r\n" +
				"With-Dash: value\r\n" +
				"Without-Space:value2\r\n" +
				"Multiple-Spaces:  value3\r\n" +
				"_WithUnderline: value4\r\n";

			var parsed = parser.Parse(dummyFile, content, false);

			parsed.Keys[0].Key.ShouldBe("Username");
			parsed.Keys[0].Value.ShouldBe("user");
			parsed.Keys[1].Key.ShouldBe("With-Dash");
			parsed.Keys[1].Value.ShouldBe("value");
			parsed.Keys[2].Key.ShouldBe("Without-Space");
			parsed.Keys[2].Value.ShouldBe("value2");
			parsed.Keys[3].Key.ShouldBe("Multiple-Spaces");
			parsed.Keys[3].Value.ShouldBe(" value3");
			parsed.Keys[4].Key.ShouldBe("_WithUnderline");
			parsed.Keys[4].Value.ShouldBe("value4");
		}

		[Fact]
		public void Test_Metadata_Multiple_Keys()
		{
			var content = "password\r\n" +
				"Username: user\r\n" +
				"Duplicate: value1\r\n" +
				"Duplicate: value2\r\n" +
				"Duplicate: value3\r\n";

			var parsed = parser.Parse(dummyFile, content, false);

			parsed.Keys[0].Key.ShouldBe("Username");
			parsed.Keys[0].Value.ShouldBe("user");
			parsed.Keys[1].Key.ShouldBe("Duplicate");
			parsed.Keys[1].Value.ShouldBe("value1");
			parsed.Keys[2].Key.ShouldBe("Duplicate");
			parsed.Keys[2].Value.ShouldBe("value2");
			parsed.Keys[3].Key.ShouldBe("Duplicate");
			parsed.Keys[3].Value.ShouldBe("value3");
		}
	}
}
