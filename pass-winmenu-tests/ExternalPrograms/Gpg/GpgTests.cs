using System.Collections.Generic;
using Moq;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.ExternalPrograms.Gpg
{
	public class GpgTests
	{
		[Fact]
		public void Decrypt_CallsGpg()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Decrypt("file");

			transportMock.Verify(t => t.CallGpg(It.IsAny<string>(), null), Times.Once);
		}

		[Theory]
		[InlineData("password")]
		[InlineData("password\nline2")]
		[InlineData("password\r\nline2")]
		[InlineData("\npassword\r\n")]
		public void Decrypt_ReturnsFileContent(string fileContent)
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), null))
				.Returns(new GpgResultBuilder()
					.WithStdout(fileContent)
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var decryptedContent = gpg.Decrypt("file");

			decryptedContent.ShouldBe(fileContent);
		}

		[Fact]
		public void Decrypt_WithExtraOptions_IncludesOptionsInGpgCall()
		{
			var config = new GpgConfig
			{
				AdditionalOptions = new AdditionalOptionsConfig()
				{
					Always = new Dictionary<string, string>
					{
						{
							"verbose", ""
						}
					},
					Decrypt = new Dictionary<string, string>
					{
						{
							"try-secret-key", "mysecret"
						}
					}
				}
			};
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, config);

			gpg.Decrypt("file");

			transportMock.Verify(t => t.CallGpg("--verbose --try-secret-key \"mysecret\" --decrypt \"file\"", null), Times.Once);
		}

		[Fact]
		public void Decrypt_WithPassphraseProvider_UsesLoopbackAndWritesPassphraseToStdin()
		{
			var transportMock = new Mock<IGpgTransport>();
			// Capture a copy of the secret bytes at call time, because GPG.Decrypt zeroes the
			// original array in its finally block (which is the desired hygiene behaviour).
			byte[]? capturedSecret = null;
			transportMock.Setup(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>()))
				.Callback<string, byte[]>((args, secret) => capturedSecret = (byte[])secret.Clone())
				.Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig(),
				new StubPassphraseProvider("hunter2".ToCharArray()));

			gpg.Decrypt("file");

			// The passphrase is sent as raw bytes on a dedicated secret-stdin path (never as a string argument or input).
			transportMock.Verify(t => t.CallGpgWithSecret(
				It.Is<string>(args => args.Contains("--pinentry-mode loopback")
					&& args.Contains("--passphrase-fd 0")
					&& args.Contains("--decrypt \"file\"")),
				It.IsAny<byte[]>()), Times.Once);
			capturedSecret.ShouldNotBeNull();
			System.Text.Encoding.UTF8.GetString(capturedSecret).ShouldStartWith("hunter2");
		}

		[Fact]
		public void Decrypt_WithNullPassphraseProvider_UsesNormalDecrypt()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig(),
				new StubPassphraseProvider(null));

			gpg.Decrypt("file");

			transportMock.Verify(t => t.CallGpg(It.Is<string>(args => !args.Contains("loopback")), null), Times.Once);
		}

		[Fact]
		public void Decrypt_LoopbackBadPassphrase_InvalidatesProviderAndRetriesWithPinentry()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(GetSuccessResult);
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), It.IsAny<string>())).Returns(GetSuccessResult);
			var provider = new StubPassphraseProvider("wrongpass".ToCharArray());
			// Reject the first (loopback) attempt, accept the second (normal pinentry) attempt.
			var verifier = new FailFirstWithBadPassphraseVerifier();

			var gpg = new GPG(transportMock.Object, verifier, new GpgConfig(), provider);

			gpg.Decrypt("file");

			provider.InvalidateCount.ShouldBe(1);
			// First the loopback attempt (rejected), then a normal pinentry retry.
			transportMock.Verify(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
			transportMock.Verify(t => t.CallGpg(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_NoRecipients_CallsGpg()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", false);

			transportMock.Verify(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_NullRecipients_CallsGpg()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", false, null);

			transportMock.Verify(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_Recipients_CallsGpgWithRecipients()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", false, "rcp_0", "rcp_1");

			transportMock.Verify(t => t.CallGpg(It.Is<string>(args =>
				args.Contains("rcp_0") && args.Contains("rcp_1")), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_Overwrite_CallsGpgWithYesOption()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", true);

			transportMock.Verify(t => t.CallGpg(It.Is<string>(args =>
				args.Contains("--yes")), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_WithExtraOptions_IncludesOptionsInGpgCall()
		{
			var config = new GpgConfig
			{
				AdditionalOptions = new AdditionalOptionsConfig()
				{
					Always = new Dictionary<string, string>
					{
						{
							"verbose", ""
						}
					},
				}
			};
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, config);

			gpg.Encrypt("data", "file", true, "rcp_0");

			transportMock.Verify(t => t.CallGpg(It.Is<string>(args => args.Contains("--verbose")), It.IsAny<string>()), Times.Once);
		}

		[Fact]
		public void StartAgent_CallsListSecretKeys()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("secret keys")
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.StartAgent();

			transportMock.Verify(t => t.CallGpg(It.IsRegex("--list-secret-keys"), null), Times.Once);
		}

		[Fact]
		public void StartAgent_NoSecretKeys_ThrowsGpgError()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("")
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			Should.Throw<GpgError>(() => gpg.StartAgent());
		}

		[Fact]
		public void GetVersion_ReturnsFirstOutputLine()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("GPG version 1.0\r\nmore info")
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var version = gpg.GetVersion();

			version.ShouldBe("GPG version 1.0");
		}

		[Fact]
		public void GetRecipients_ReturnsRecipients()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("GPG version 1.0\r\nmore info")
					.WithStatusMessage(GpgStatusCode.ENC_TO, "user0 0 0")
					.WithStatusMessage(GpgStatusCode.ENC_TO, "user1 0 0")
					.Build()); ;
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var recipients = gpg.GetRecipients("testFile");

			recipients.ShouldBe(new[] { "user0", "user1" });
		}

		[Fact]
		public void FindShortKeyId_ReturnsKeyId()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("uid:u::::1627027253::B3B2807670A468499DF1292C1140265C5D4B56E1::Test User <test@geluk.io>::::::::::0:" +
					"\r\nsub:u:3072:1:EDE97135FC244819:1627027253:1690099253:::::e::::::23:" +
					"\r\nfpr:::::::::63BAC0DAFD648D28BC675FF2EDE97135FC244819:")
					.Build()); ;
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var shortKeyId = gpg.FindShortKeyId("testFile");

			shortKeyId.ShouldBe("EDE97135FC244819");
		}

		private GpgResult GetSuccessResult()
		{
			return new GpgResultBuilder().Build();
		}

		private class StubPassphraseProvider : IPassphraseProvider
		{
			private readonly char[]? passphrase;

			public StubPassphraseProvider(char[]? passphrase)
			{
				this.passphrase = passphrase;
			}

			public char[]? GetPassphrase() => passphrase == null ? null : (char[])passphrase.Clone();

			public int InvalidateCount { get; private set; }

			public void Invalidate() => InvalidateCount++;
		}

		private class StubGpgResultVerifier : IGpgResultVerifier
		{
			private readonly bool valid;

			private StubGpgResultVerifier(bool valid)
			{
				this.valid = valid;
			}

			public void VerifyDecryption(GpgResult result)
			{
				if (!valid)
				{
					throw new GpgError("Invalid result.");
				}
			}

			public void VerifyEncryption(GpgResult result)
			{
				if (!valid)
				{
					throw new GpgError("Invalid result.");
				}
			}

			public static IGpgResultVerifier AlwaysValid => new StubGpgResultVerifier(true);
		}

		private class FailFirstWithBadPassphraseVerifier : IGpgResultVerifier
		{
			private int calls;

			public void VerifyDecryption(GpgResult result)
			{
				if (calls++ == 0)
				{
					throw new BadPassphraseException("The supplied passphrase was incorrect.");
				}
			}

			public void VerifyEncryption(GpgResult result)
			{
			}
		}
	}
}
