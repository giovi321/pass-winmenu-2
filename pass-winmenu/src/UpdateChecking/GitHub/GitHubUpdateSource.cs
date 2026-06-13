using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

#nullable enable
namespace PassWinmenu.UpdateChecking.GitHub
{
	internal class GitHubUpdateSource : IUpdateSource
	{
		private static readonly Uri UpdateUrl = new Uri("https://api.github.com/repos/giovi321/pass-winmenu-2/releases");
		private static readonly Uri DefaultDownloadUrl = new Uri("https://github.com/giovi321/pass-winmenu-2/releases");

		public bool RequiresConnectivity => true;

		private ProgramVersion ToProgramVersion(Release release)
		{
			var importantRegex = new Regex(@"\*\*\s*important\s+release:?\s*\*\*", RegexOptions.IgnoreCase);
			var important = importantRegex.IsMatch(release.Body ?? string.Empty);

			return new ProgramVersion
			{
				VersionNumber = release.ParseVersion(),
				DownloadLink = release.HtmlUrl ?? DefaultDownloadUrl,
				ReleaseDate = release.PublishedAt,
				ReleaseNotes = release.HtmlUrl ?? DefaultDownloadUrl,
				IsPrerelease = release.Prerelease,
				Important = important,
			};
		}

		public IEnumerable<ProgramVersion> GetAllReleases()
		{
			var releases = FetchReleases();
			return releases.Select(ToProgramVersion);
		}

		private Release[] FetchReleases()
		{
			var rq = WebRequest.CreateHttp(UpdateUrl);
			rq.ServerCertificateValidationCallback += ValidateCertificate;
			rq.UserAgent = $"pass-winmenu-2/{Program.Version}";

			using var stream = rq.GetResponse().GetResponseStream();
			if (stream == null)
			{
				throw new UpdateException("Unable to fetch response stream.");
			}
			using var reader = new StreamReader(stream);
			var responseText = reader.ReadToEnd();
			return GithubReleaseDeserialiser.DeserialiseReleases(responseText);
		}

		private bool ValidateCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
		{
			if (sslPolicyErrors == SslPolicyErrors.None)
			{
				return true;
			}

			Log.Send($"Server certificate failed to validate: {sslPolicyErrors}", LogLevel.Warning);
			return false;
		}
	}
}
