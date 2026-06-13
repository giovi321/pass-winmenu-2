using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Moq;
using PassWinmenu.PasswordManagement;
using PassWinmenuTests.Utilities.ExtensionMethods;
using Xunit;

namespace PassWinmenuTests.PasswordManagement
{
	public class TotpGeneratorTests
	{
		private static readonly PasswordFile FakeFile = new(Mock.Of<IFileInfo>(), Mock.Of<IDirectoryInfo>());

		[Fact]
		public void GenerateTotpCode_NoTotp_None()
		{
			var keys = new List<KeyValuePair<string, string>>() {
				new KeyValuePair<string, string>("Key", "Value")
			};

			var file = new KeyedPasswordFile(FakeFile, string.Empty, null, keys);
			var code = TotpGenerator.GenerateTotpCode(file, DateTime.Now);

			code.ShouldBeNone();
		}

		[Fact]
		public void GenerateTotpCode_ValidTotp_ValidCode()
		{
			var keys = new List<KeyValuePair<string, string>>() {
				new KeyValuePair<string, string>("TOTP", "ABCDEF")
			};

			var file = new KeyedPasswordFile(FakeFile, string.Empty, null, keys);
			var code = TotpGenerator.GenerateTotpCode(file, new DateTime(2021, 10, 13, 15, 15, 30, DateTimeKind.Utc));

			code.ShouldBeSome("235025");
		}

		[Fact]
		public void GenerateTotpCode_ValidOtpAuth_ValidCode()
		{
			var keys = new List<KeyValuePair<string, string>>() {
				new KeyValuePair<string, string>("OTPAUTH", "otpauth://otp/account?secret=HELLOTHERE&digits=6")
			};

			var file = new KeyedPasswordFile(FakeFile, string.Empty, null, keys);
			var code = TotpGenerator.GenerateTotpCode(file, new DateTime(2021, 10, 13, 20, 26, 30, DateTimeKind.Utc));

			code.ShouldBeSome("514271");
		}
	}
}
