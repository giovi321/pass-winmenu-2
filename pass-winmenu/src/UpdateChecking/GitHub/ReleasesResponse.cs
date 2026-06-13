using System;
using McSherry.SemanticVersioning;

#nullable enable
namespace PassWinmenu.UpdateChecking.GitHub
{
	public class Release
	{
		public Uri? HtmlUrl { get; set; }
		public string? TagName { get; set; }
		public bool Prerelease { get; set; }
		public DateTime PublishedAt { get; set; }
		public string? Body { get; set; }
		public SemanticVersion ParseVersion() => SemanticVersion.Parse(TagName, ParseMode.Lenient);
	}
}
