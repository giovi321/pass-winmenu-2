using System.IO;
using System.Text;
using PassWinmenu.Configuration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Configuration
{
	public class BiometricConfigBackCompatTests
	{
		[Fact]
		public void Deserialise_LegacyBiometricsBlock_IgnoresRemovedKeysAndStillLoads()
		{
			// A pre-1.8 config still carries the removed 'mode' and 'cache-seconds' keys. They must
			// deserialise without throwing (into the obsolete shim properties) so that an existing
			// user's configuration file keeps loading after upgrade.
			var yaml =
				"gpg:\n" +
				"    biometrics:\n" +
				"        enabled: true\n" +
				"        mode: every-password\n" +
				"        cache-seconds: 1200\n" +
				"        credential-name: pass-winmenu-gpg\n" +
				"config-version: 1.8\n";

			using var stream = new MemoryStream(Encoding.UTF8.GetBytes(yaml));
			using var reader = new StreamReader(stream);

			var config = ConfigurationDeserialiser.Deserialise<Config>(reader);

			config.Gpg.Biometrics.Enabled.ShouldBeTrue();
			config.Gpg.Biometrics.CredentialName.ShouldBe("pass-winmenu-gpg");
		}
	}
}
