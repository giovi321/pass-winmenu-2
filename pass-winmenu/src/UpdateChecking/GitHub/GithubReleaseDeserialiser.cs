using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PassWinmenu.UpdateChecking.GitHub
{
	public static class GithubReleaseDeserialiser
	{
		private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new SnakeCaseNamingStrategy()
			}
		};

		public static Release[] DeserialiseReleases(string releasesJson) => 
			JsonConvert.DeserializeObject<Release[]>(releasesJson, Settings) ?? Array.Empty<Release>();
	}
}
