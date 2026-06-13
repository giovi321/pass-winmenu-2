using System;

#nullable enable
namespace PassWinmenu.Utilities
{
	public class EnvironmentVariables
	{
		public string? PasswordStoreKey { get; set; }

		public static EnvironmentVariables LoadFromEnvironment()
		{
			return new EnvironmentVariables()
			{
				PasswordStoreKey = Environment.GetEnvironmentVariable("PASSWORD_STORE_KEY"),
			};
		}
	}
}
