using System;
using System.IO.Abstractions.TestingHelpers;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.ExternalPrograms.Gpg
{
	public class GpgHomedirResolverTests
	{
		private static readonly GpgInstallation Installation = new GpgInstallationBuilder().Build();

		[Fact]
		public void Resolve_NullHomeOverride_ReturnsLocationSpecifiedByGpg()
		{
			var config = new GpgConfig { GnupghomeOverride = null };
			var resolver = new GpgHomeDirResolver(
				config,
				Installation,
				ReturnsHomeDir(@"homedir:C%3a\Users\Test\AppData\gnupg"));

			var homeDir = resolver.GetHomeDir();

			homeDir.Path.ShouldBe(@"C:\Users\Test\AppData\gnupg");
			homeDir.IsOverride.ShouldBeFalse();
		}

		[Fact]
		public void Resolve_HomeOverrideSet_ReturnsHomeOverride()
		{
			var config = new GpgConfig { GnupghomeOverride = @"C:\Users\Test\.gpg" };
			var resolver = new GpgHomeDirResolver(
				config,
				Installation,
				ReturnsHomeDir(@"homedir:C%3a\Users\Test\AppData\gnupg"));

			var homeDir = resolver.GetHomeDir();

			homeDir.Path.ShouldBe(@"C:\Users\Test\.gpg");
			homeDir.IsOverride.ShouldBeTrue();
		}

		private static FakeProcesses ReturnsHomeDir(string homeDir)
		{
			return new FakeProcesses(new FakeProcessBuilder().WithStandardOutput(homeDir).Build());
		}
	}
}
