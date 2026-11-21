using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

#nullable enable
namespace PassWinmenu.UpdateChecking.GitHub
{
	internal class GitHubUpdateSource : IUpdateSource
	{
		private static readonly Uri UpdateUrl = new Uri("https://api.github.com/repos/giovi321/pass-winmenu-2/releases");
		private static readonly HttpClient HttpClient = CreateHttpClient();
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
			using var request = new HttpRequestMessage(HttpMethod.Get, UpdateUrl);
			request.Headers.UserAgent.ParseAdd($"pass-winmenu-2/{Program.Version}");

			using var response = HttpClient.Send(request);
			response.EnsureSuccessStatusCode();

			using var stream = response.Content.ReadAsStream();
			using var reader = new StreamReader(stream);
			var responseText = reader.ReadToEnd();
			return GithubReleaseDeserialiser.DeserialiseReleases(responseText);
		}

		private static HttpClient CreateHttpClient()
		{
			var handler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = ValidateCertificate,
			};
			return new HttpClient(handler);
		}

		private static bool ValidateCertificate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
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
