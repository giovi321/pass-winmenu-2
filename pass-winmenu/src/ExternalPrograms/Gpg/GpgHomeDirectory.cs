namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgHomeDirectory
	{
		/// <summary>
		/// The path to the GPG home directory.
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// True if the home directory path was overridden in pass-winmenu.yml.
		/// </summary>
		public bool IsOverride { get; set; }

		public GpgHomeDirectory(string path, bool isOverride = false)
		{
			Path = path;
			IsOverride = isOverride;
		}
	}
}
