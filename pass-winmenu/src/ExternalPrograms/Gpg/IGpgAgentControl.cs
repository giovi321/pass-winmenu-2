namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal interface IGpgAgentControl
	{
		/// <summary>
		/// Ensures gpg-agent is running and has re-read its configuration (so a freshly added
		/// <c>allow-preset-passphrase</c> takes effect).
		/// </summary>
		void ReloadAgent();
	}
}
