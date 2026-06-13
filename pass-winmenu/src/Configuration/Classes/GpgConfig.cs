using System;
using PassWinmenu.WinApi;

#nullable enable
namespace PassWinmenu.Configuration
{
	public class GpgConfig
	{
		private string? gpgPath = @"C:\Program Files (x86)\GnuPG\bin\gpg.exe";
		public string? GpgPath
		{
			get => gpgPath;
			set
			{
				if (value == null)
				{
					gpgPath = null;
				}
				else
				{
					var expanded = Environment.ExpandEnvironmentVariables(value);
					gpgPath = PathUtilities.NormaliseDirectory(expanded);
				}
			}
		}

		private string? gnupghomeOverride;
		public string? GnupghomeOverride
		{
			get => gnupghomeOverride;
			set
			{
				if (value == null)
				{
					gnupghomeOverride = null;
				}
				else
				{
					var expanded = Environment.ExpandEnvironmentVariables(value);
					gnupghomeOverride = PathUtilities.NormaliseDirectory(expanded);
				}

			}
		}

		[Obsolete("This key is no longer used, but it is still present to allow older" +
			"configuration files to be deserialised successfully.", true)]
		public bool PinentryFix { get; set; } = false;

		public AdditionalOptionsConfig AdditionalOptions { get; set; } = new AdditionalOptionsConfig();

		public GpgAgentConfig GpgAgent { get; set; } = new GpgAgentConfig();

		public BiometricConfig Biometrics { get; set; } = new BiometricConfig();
	}
}
