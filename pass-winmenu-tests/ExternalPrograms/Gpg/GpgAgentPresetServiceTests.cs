using System.Diagnostics;
using System.IO;
using System.Text;
using Moq;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.ExternalPrograms.Gpg
{
	public class GpgAgentPresetServiceTests
	{
		[Fact]
		public void Preset_WritesLiteralPassphraseToStdin_AndKeygripToArgs_NeverPassphraseToArgs()
		{
			var installation = new GpgInstallationBuilder().Build();
			var processes = new Mock<IProcesses>();
			var inputStream = new MemoryStream();
			ProcessStartInfo? captured = null;
			processes.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
				.Callback<ProcessStartInfo>(psi => captured = psi)
				.Returns(new FakeProcessBuilder()
					.WithStandardInput(inputStream)
					.WithStandardError("")
					.WithExitCode(0)
					.Build());

			var service = new GpgAgentPresetService(installation, new GpgHomeDirectory(@"C:\gpghome"), processes.Object);

			var result = service.Preset("KEYGRIP123", "hunter2".ToCharArray());

			result.ShouldBe(PresetResult.Success);
			inputStream.ToArray().ShouldBe(Encoding.UTF8.GetBytes("hunter2"));
			captured.ShouldNotBeNull();
			captured!.FileName.ShouldEndWith("gpg-preset-passphrase.exe");
			captured.Arguments.ShouldContain("KEYGRIP123");
			captured.Arguments.ShouldNotContain("hunter2");
		}

		[Fact]
		public void Preset_WhenAgentRejects_ReturnsNotAllowed()
		{
			var installation = new GpgInstallationBuilder().Build();
			var processes = new Mock<IProcesses>();
			processes.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
				.Returns(new FakeProcessBuilder()
					.WithStandardInput(new MemoryStream())
					.WithStandardError("gpg-preset-passphrase: caching passphrase failed: Not supported")
					.WithExitCode(2)
					.Build());

			var service = new GpgAgentPresetService(installation, new GpgHomeDirectory(@"C:\gpghome"), processes.Object);

			service.Preset("KEYGRIP123", "hunter2".ToCharArray()).ShouldBe(PresetResult.NotAllowed);
		}
	}
}
