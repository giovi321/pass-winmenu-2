using System;
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
		public void Decrypt_AgentCacheWarm_DecryptsWithoutPromptingHello()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null))
				.Returns(() => new GpgResultBuilder().WithStdout("secret").Build());
			var provider = StubPassphraseProvider.Provides("hunter2");
			// The probe (the only VerifyDecryption call) succeeds: the agent cache is warm.
			var verifier = new ScriptedVerifier(() => { });

			var gpg = new GPG(transportMock.Object, verifier, new GpgConfig(), provider);

			gpg.Decrypt("file").ShouldBe("secret");

			// The passphrase is served straight from gpg-agent's cache: no Hello gesture, no loopback.
			provider.GetPassphraseCount.ShouldBe(0);
			transportMock.Verify(t => t.CallGpg(It.Is<string>(a => a.Contains("--pinentry-mode cancel")), null), Times.Once);
			transportMock.Verify(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
		}

		[Fact]
		public void Decrypt_AgentCacheCold_PromptsHelloAndUsesLoopback()
		{
			var transportMock = new Mock<IGpgTransport>();
			// Capture a copy of the secret bytes at call time, because GPG.Decrypt zeroes the
			// original array in its finally block (which is the desired hygiene behaviour).
			byte[]? capturedSecret = null;
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			transportMock.Setup(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>()))
				.Callback<string, byte[]>((args, secret) => capturedSecret = (byte[])secret.Clone())
				.Returns(() => new GpgResultBuilder().WithStdout("secret").Build());
			var provider = StubPassphraseProvider.Provides("hunter2");
			// The probe misses (cold cache); the loopback decrypt then succeeds.
			var verifier = new ScriptedVerifier(() => throw new GpgError("cache miss"), () => { });

			var gpg = new GPG(transportMock.Object, verifier, new GpgConfig(), provider);

			gpg.Decrypt("file").ShouldBe("secret");

			provider.GetPassphraseCount.ShouldBe(1);
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
		public void Decrypt_HelloDeclined_FailsClosedWithoutFallingBackToNormalDecrypt()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var provider = StubPassphraseProvider.Declines();
			// The probe misses (cold cache); the user then declines the Hello gesture.
			var verifier = new ScriptedVerifier(() => throw new GpgError("cache miss"));

			var gpg = new GPG(transportMock.Object, verifier, new GpgConfig(), provider);

			Should.Throw<GpgException>(() => gpg.Decrypt("file"));

			// Only the probe ran. Crucially there is no fallback to a normal (agent-cacheable) decrypt.
			transportMock.Verify(t => t.CallGpg(It.Is<string>(a => a.Contains("--pinentry-mode cancel")), null), Times.Once);
			transportMock.Verify(t => t.CallGpg(It.Is<string>(a => !a.Contains("--pinentry-mode cancel")), null), Times.Never);
			transportMock.Verify(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
		}

		[Fact]
		public void Decrypt_HelloUnavailable_FallsBackToNormalDecrypt()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null))
				.Returns(() => new GpgResultBuilder().WithStdout("secret").Build());
			var provider = StubPassphraseProvider.Unavailable();
			// The probe misses; Windows Hello is unavailable, so we fall back to a normal decrypt.
			var verifier = new ScriptedVerifier(() => throw new GpgError("cache miss"), () => { });

			var gpg = new GPG(transportMock.Object, verifier, new GpgConfig(), provider);

			gpg.Decrypt("file").ShouldBe("secret");

			transportMock.Verify(t => t.CallGpg(It.Is<string>(a => a.Contains("--pinentry-mode cancel")), null), Times.Once);
			transportMock.Verify(t => t.CallGpg(It.Is<string>(a => a.Contains("--decrypt") && !a.Contains("cancel")), null), Times.Once);
		}

		[Fact]
		public void Decrypt_Disabled_UsesNormalDecrypt()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig(),
				StubPassphraseProvider.Disabled());

			gpg.Decrypt("file");

			// No probe and no loopback: a disabled provider behaves exactly like no provider.
			transportMock.Verify(t => t.CallGpg(It.Is<string>(args => !args.Contains("loopback") && !args.Contains("cancel")), null), Times.Once);
		}

		[Fact]
		public void Decrypt_LoopbackBadPassphrase_InvalidatesProviderAndRetriesWithPinentry()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(GetSuccessResult);
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), It.IsAny<string>())).Returns(GetSuccessResult);
			var provider = StubPassphraseProvider.Provides("wrongpass");
			// Probe misses; the loopback attempt is rejected (bad passphrase); the normal pinentry retry succeeds.
			var verifier = new ScriptedVerifier(
				() => throw new GpgError("cache miss"),
				() => throw new BadPassphraseException("The supplied passphrase was incorrect."),
				() => { });

			var gpg = new GPG(transportMock.Object, verifier, new GpgConfig(), provider);

			gpg.Decrypt("file");

			provider.InvalidateCount.ShouldBe(1);
			transportMock.Verify(t => t.CallGpgWithSecret(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
			// The probe and the normal pinentry retry both go through CallGpg.
			transportMock.Verify(t => t.CallGpg(It.Is<string>(a => a.Contains("--pinentry-mode cancel")), It.IsAny<string>()), Times.Once);
			transportMock.Verify(t => t.CallGpg(It.Is<string>(a => !a.Contains("cancel") && a.Contains("--decrypt")), It.IsAny<string>()), Times.Once);
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

		private sealed class StubPassphraseProvider : IPassphraseProvider
		{
			private readonly char[]? passphrase;
			private readonly PassphraseOutcome outcome;

			private StubPassphraseProvider(bool isEnabled, PassphraseOutcome outcome, char[]? passphrase)
			{
				IsEnabled = isEnabled;
				this.outcome = outcome;
				this.passphrase = passphrase;
			}

			public static StubPassphraseProvider Disabled() => new(false, PassphraseOutcome.Unavailable, null);
			public static StubPassphraseProvider Provides(string passphrase) => new(true, PassphraseOutcome.Provided, passphrase.ToCharArray());
			public static StubPassphraseProvider Declines() => new(true, PassphraseOutcome.Declined, null);
			public static StubPassphraseProvider Unavailable() => new(true, PassphraseOutcome.Unavailable, null);

			public bool IsEnabled { get; }
			public int GetPassphraseCount { get; private set; }
			public int InvalidateCount { get; private set; }

			public PassphraseResult GetPassphrase()
			{
				GetPassphraseCount++;
				return outcome switch
				{
					PassphraseOutcome.Provided => PassphraseResult.Provided((char[])passphrase!.Clone()),
					PassphraseOutcome.Declined => PassphraseResult.Declined,
					_ => PassphraseResult.Unavailable,
				};
			}

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

		/// <summary>
		/// A verifier that runs a scripted sequence of behaviours across successive VerifyDecryption
		/// calls (e.g. throw on the probe to simulate a cache miss, then succeed on the loopback
		/// decrypt). Calls beyond the script are treated as success.
		/// </summary>
		private sealed class ScriptedVerifier : IGpgResultVerifier
		{
			private readonly Queue<Action> steps;

			public ScriptedVerifier(params Action[] steps) => this.steps = new Queue<Action>(steps);

			public void VerifyDecryption(GpgResult result)
			{
				if (steps.Count > 0)
				{
					steps.Dequeue()();
				}
			}

			public void VerifyEncryption(GpgResult result)
			{
			}
		}
	}
}
