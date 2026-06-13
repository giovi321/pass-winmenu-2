using PassWinmenu.UpdateChecking.GitHub;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.UpdateChecking.GitHub
{
	public class GithubReleaseDeserialiserTests
	{
		[Fact]
		public void DeserialiseReleases_ValidJson_ReturnsReleases()
		{
			var json = GetReleasesJson();
			var releases = GithubReleaseDeserialiser.DeserialiseReleases(json);

			releases.Length.ShouldBe(2);
		}

		private string GetReleasesJson() => @"
[
  {
    ""url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/51163323"",
    ""assets_url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/51163323/assets"",
    ""upload_url"": ""https://uploads.github.com/repos/geluk/pass-winmenu/releases/51163323/assets{?name,label}"",
    ""html_url"": ""https://github.com/geluk/pass-winmenu/releases/tag/v1.12.1"",
    ""id"": 51163323,
    ""author"": {
      ""login"": ""geluk"",
      ""id"": 1516985,
      ""node_id"": ""MDQ6VXNlcjE1MTY5ODU="",
      ""avatar_url"": ""https://avatars.githubusercontent.com/u/1516985?v=4"",
      ""gravatar_id"": """",
      ""url"": ""https://api.github.com/users/geluk"",
      ""html_url"": ""https://github.com/geluk"",
      ""followers_url"": ""https://api.github.com/users/geluk/followers"",
      ""following_url"": ""https://api.github.com/users/geluk/following{/other_user}"",
      ""gists_url"": ""https://api.github.com/users/geluk/gists{/gist_id}"",
      ""starred_url"": ""https://api.github.com/users/geluk/starred{/owner}{/repo}"",
      ""subscriptions_url"": ""https://api.github.com/users/geluk/subscriptions"",
      ""organizations_url"": ""https://api.github.com/users/geluk/orgs"",
      ""repos_url"": ""https://api.github.com/users/geluk/repos"",
      ""events_url"": ""https://api.github.com/users/geluk/events{/privacy}"",
      ""received_events_url"": ""https://api.github.com/users/geluk/received_events"",
      ""type"": ""User"",
      ""site_admin"": false
    },
    ""node_id"": ""RE_kwDOAyibF84DDLC7"",
    ""tag_name"": ""v1.12.1"",
    ""target_commitish"": ""9c8bd22fa664595f4ab4937c2391c622129e0caa"",
    ""name"": ""v1.12.1"",
    ""draft"": false,
    ""prerelease"": false,
    ""created_at"": ""2021-10-10T17:24:50Z"",
    ""published_at"": ""2021-10-11T19:40:45Z"",
    ""assets"": [
      {
        ""url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/assets/46727921"",
        ""id"": 46727921,
        ""node_id"": ""RA_kwDOAyibF84CyQLx"",
        ""name"": ""pass-winmenu-nogpg.zip"",
        ""label"": """",
        ""uploader"": {
          ""login"": ""geluk"",
          ""id"": 1516985,
          ""node_id"": ""MDQ6VXNlcjE1MTY5ODU="",
          ""avatar_url"": ""https://avatars.githubusercontent.com/u/1516985?v=4"",
          ""gravatar_id"": """",
          ""url"": ""https://api.github.com/users/geluk"",
          ""html_url"": ""https://github.com/geluk"",
          ""followers_url"": ""https://api.github.com/users/geluk/followers"",
          ""following_url"": ""https://api.github.com/users/geluk/following{/other_user}"",
          ""gists_url"": ""https://api.github.com/users/geluk/gists{/gist_id}"",
          ""starred_url"": ""https://api.github.com/users/geluk/starred{/owner}{/repo}"",
          ""subscriptions_url"": ""https://api.github.com/users/geluk/subscriptions"",
          ""organizations_url"": ""https://api.github.com/users/geluk/orgs"",
          ""repos_url"": ""https://api.github.com/users/geluk/repos"",
          ""events_url"": ""https://api.github.com/users/geluk/events{/privacy}"",
          ""received_events_url"": ""https://api.github.com/users/geluk/received_events"",
          ""type"": ""User"",
          ""site_admin"": false
        },
        ""content_type"": ""application/octet-stream"",
        ""state"": ""uploaded"",
        ""size"": 2256616,
        ""download_count"": 447,
        ""created_at"": ""2021-10-11T19:23:49Z"",
        ""updated_at"": ""2021-10-11T19:23:49Z"",
        ""browser_download_url"": ""https://github.com/geluk/pass-winmenu/releases/download/v1.12.1/pass-winmenu-nogpg.zip""
      },
      {
        ""url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/assets/46727923"",
        ""id"": 46727923,
        ""node_id"": ""RA_kwDOAyibF84CyQLz"",
        ""name"": ""pass-winmenu.zip"",
        ""label"": """",
        ""uploader"": {
          ""login"": ""geluk"",
          ""id"": 1516985,
          ""node_id"": ""MDQ6VXNlcjE1MTY5ODU="",
          ""avatar_url"": ""https://avatars.githubusercontent.com/u/1516985?v=4"",
          ""gravatar_id"": """",
          ""url"": ""https://api.github.com/users/geluk"",
          ""html_url"": ""https://github.com/geluk"",
          ""followers_url"": ""https://api.github.com/users/geluk/followers"",
          ""following_url"": ""https://api.github.com/users/geluk/following{/other_user}"",
          ""gists_url"": ""https://api.github.com/users/geluk/gists{/gist_id}"",
          ""starred_url"": ""https://api.github.com/users/geluk/starred{/owner}{/repo}"",
          ""subscriptions_url"": ""https://api.github.com/users/geluk/subscriptions"",
          ""organizations_url"": ""https://api.github.com/users/geluk/orgs"",
          ""repos_url"": ""https://api.github.com/users/geluk/repos"",
          ""events_url"": ""https://api.github.com/users/geluk/events{/privacy}"",
          ""received_events_url"": ""https://api.github.com/users/geluk/received_events"",
          ""type"": ""User"",
          ""site_admin"": false
        },
        ""content_type"": ""application/octet-stream"",
        ""state"": ""uploaded"",
        ""size"": 6569770,
        ""download_count"": 223,
        ""created_at"": ""2021-10-11T19:23:49Z"",
        ""updated_at"": ""2021-10-11T19:23:50Z"",
        ""browser_download_url"": ""https://github.com/geluk/pass-winmenu/releases/download/v1.12.1/pass-winmenu.zip""
      }
    ],
    ""tarball_url"": ""https://api.github.com/repos/geluk/pass-winmenu/tarball/v1.12.1"",
    ""zipball_url"": ""https://api.github.com/repos/geluk/pass-winmenu/zipball/v1.12.1"",
    ""body"": ""\r\n\r\n\r\n\r\n## Changes\r\n - Improved the relative path generation algorithm (used to generate relative paths for commit messages) (#97)\r\n - Percentage dimensions (as used under the `style` configuration key) are now consistently parsed using a dot as the decimal separator. Previously, the culture-specific separator was used instead.\r\n - The 'agent responsive' check has been removed. This automated check has been present in pass-winmenu for a long time, and stems from a time when GPG on windows would occasionally lock up, leading to all password operations to hang indefinitely. The automated check would detect this situation and restart GPG when it occurs. As a result of a recent bug in the code (#99), and based on my belief that this bug in GPG has long since been fixed, I have decided to remove it. If you do run into lockups as I have described here, please let me know by opening an issue.\r\n - Improved error message clarity when the `user.name` and/or `user.email` git configuration variables are not set (#100).\r\n\r\n## Bugfixes\r\n - Fixed an issue that could cause the application to crash when manually checking for updates (#96, thanks @wfdewith!)\r\n - Fixed an error when setting the configuration key `git.ssh-path` if the `GIT_SSH` environment variable is also set (#98)\r\n "",
    ""mentions_count"": 1
  },
  {
    ""url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/46688435"",
    ""assets_url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/46688435/assets"",
    ""upload_url"": ""https://uploads.github.com/repos/geluk/pass-winmenu/releases/46688435/assets{?name,label}"",
    ""html_url"": ""https://github.com/geluk/pass-winmenu/releases/tag/v1.12"",
    ""id"": 46688435,
    ""author"": {
      ""login"": ""geluk"",
      ""id"": 1516985,
      ""node_id"": ""MDQ6VXNlcjE1MTY5ODU="",
      ""avatar_url"": ""https://avatars.githubusercontent.com/u/1516985?v=4"",
      ""gravatar_id"": """",
      ""url"": ""https://api.github.com/users/geluk"",
      ""html_url"": ""https://github.com/geluk"",
      ""followers_url"": ""https://api.github.com/users/geluk/followers"",
      ""following_url"": ""https://api.github.com/users/geluk/following{/other_user}"",
      ""gists_url"": ""https://api.github.com/users/geluk/gists{/gist_id}"",
      ""starred_url"": ""https://api.github.com/users/geluk/starred{/owner}{/repo}"",
      ""subscriptions_url"": ""https://api.github.com/users/geluk/subscriptions"",
      ""organizations_url"": ""https://api.github.com/users/geluk/orgs"",
      ""repos_url"": ""https://api.github.com/users/geluk/repos"",
      ""events_url"": ""https://api.github.com/users/geluk/events{/privacy}"",
      ""received_events_url"": ""https://api.github.com/users/geluk/received_events"",
      ""type"": ""User"",
      ""site_admin"": false
    },
    ""node_id"": ""MDc6UmVsZWFzZTQ2Njg4NDM1"",
    ""tag_name"": ""v1.12"",
    ""target_commitish"": ""b16d0684bc9eca894a0de2c598dfd93ee3a3edf8"",
    ""name"": ""v1.12"",
    ""draft"": false,
    ""prerelease"": false,
    ""created_at"": ""2021-09-02T21:54:21Z"",
    ""published_at"": ""2021-09-02T22:02:18Z"",
    ""assets"": [
      {
        ""url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/assets/43915602"",
        ""id"": 43915602,
        ""node_id"": ""MDEyOlJlbGVhc2VBc3NldDQzOTE1NjAy"",
        ""name"": ""pass-winmenu-nogpg.zip"",
        ""label"": """",
        ""uploader"": {
          ""login"": ""geluk"",
          ""id"": 1516985,
          ""node_id"": ""MDQ6VXNlcjE1MTY5ODU="",
          ""avatar_url"": ""https://avatars.githubusercontent.com/u/1516985?v=4"",
          ""gravatar_id"": """",
          ""url"": ""https://api.github.com/users/geluk"",
          ""html_url"": ""https://github.com/geluk"",
          ""followers_url"": ""https://api.github.com/users/geluk/followers"",
          ""following_url"": ""https://api.github.com/users/geluk/following{/other_user}"",
          ""gists_url"": ""https://api.github.com/users/geluk/gists{/gist_id}"",
          ""starred_url"": ""https://api.github.com/users/geluk/starred{/owner}{/repo}"",
          ""subscriptions_url"": ""https://api.github.com/users/geluk/subscriptions"",
          ""organizations_url"": ""https://api.github.com/users/geluk/orgs"",
          ""repos_url"": ""https://api.github.com/users/geluk/repos"",
          ""events_url"": ""https://api.github.com/users/geluk/events{/privacy}"",
          ""received_events_url"": ""https://api.github.com/users/geluk/received_events"",
          ""type"": ""User"",
          ""site_admin"": false
        },
        ""content_type"": ""application/octet-stream"",
        ""state"": ""uploaded"",
        ""size"": 2257854,
        ""download_count"": 301,
        ""created_at"": ""2021-09-02T21:59:59Z"",
        ""updated_at"": ""2021-09-02T21:59:59Z"",
        ""browser_download_url"": ""https://github.com/geluk/pass-winmenu/releases/download/v1.12/pass-winmenu-nogpg.zip""
      },
      {
        ""url"": ""https://api.github.com/repos/geluk/pass-winmenu/releases/assets/43915604"",
        ""id"": 43915604,
        ""node_id"": ""MDEyOlJlbGVhc2VBc3NldDQzOTE1NjA0"",
        ""name"": ""pass-winmenu.zip"",
        ""label"": """",
        ""uploader"": {
          ""login"": ""geluk"",
          ""id"": 1516985,
          ""node_id"": ""MDQ6VXNlcjE1MTY5ODU="",
          ""avatar_url"": ""https://avatars.githubusercontent.com/u/1516985?v=4"",
          ""gravatar_id"": """",
          ""url"": ""https://api.github.com/users/geluk"",
          ""html_url"": ""https://github.com/geluk"",
          ""followers_url"": ""https://api.github.com/users/geluk/followers"",
          ""following_url"": ""https://api.github.com/users/geluk/following{/other_user}"",
          ""gists_url"": ""https://api.github.com/users/geluk/gists{/gist_id}"",
          ""starred_url"": ""https://api.github.com/users/geluk/starred{/owner}{/repo}"",
          ""subscriptions_url"": ""https://api.github.com/users/geluk/subscriptions"",
          ""organizations_url"": ""https://api.github.com/users/geluk/orgs"",
          ""repos_url"": ""https://api.github.com/users/geluk/repos"",
          ""events_url"": ""https://api.github.com/users/geluk/events{/privacy}"",
          ""received_events_url"": ""https://api.github.com/users/geluk/received_events"",
          ""type"": ""User"",
          ""site_admin"": false
        },
        ""content_type"": ""application/octet-stream"",
        ""state"": ""uploaded"",
        ""size"": 6571010,
        ""download_count"": 129,
        ""created_at"": ""2021-09-02T22:00:00Z"",
        ""updated_at"": ""2021-09-02T22:00:00Z"",
        ""browser_download_url"": ""https://github.com/geluk/pass-winmenu/releases/download/v1.12/pass-winmenu.zip""
      }
    ],
    ""tarball_url"": ""https://api.github.com/repos/geluk/pass-winmenu/tarball/v1.12"",
    ""zipball_url"": ""https://api.github.com/repos/geluk/pass-winmenu/zipball/v1.12"",
    ""body"": ""----\r\n\r\n## New features\r\n - Re-encrypting the password store is now possible. When re-encrypting, pass-winmenu will check for any files that need to have their recipients updated. Those files will be re-encrypted for the correct recipients. All other files are left untouched. Re-encryption can be started by right-clicking the notification area icon and selecting `More Options -> Re-Encrypt Password Store`, and choosing a path to re-encrypt.\r\n - `pass` allows the use of the `PASSWORD_STORE_KEY` environment variable to override the recipients specified in `.gpg-id` when set. Pass-winmenu now also honours this variable (#86).\r\n - Custom gpg options can now be configured. These options will be passed to `gpg.exe` when passwords are encrypted or decrypted. Take a look at the [default configuration file](https://github.com/geluk/pass-winmenu/blob/e7b295dce7f13ebfa8c47b487e8b63c151b1d031/pass-winmenu/embedded/default-config.yaml#L153) to see how you can configure these. (#86).\r\n - Search hints are now displayed in the search field of the password menu.\r\n\r\n## Changes\r\n - The 'type password' feature now generates direct character input, instead of simulating key presses. This greatly improves compatibility with many keyboard layouts, especially those that have their own input methods. If you experienced issues with pass-winmenu typing the wrong characters, they have hopefully been resolved by this change. As a result of this change, the `dead-keys` option has become redundant and can be removed from the configuration file (#80).\r\n - When commiting a new or changed password, commit messages will now specify a path relative to the root of the password file instead of the full path (#89, #92, thanks @sbu-WBT!)\r\n - Error reporting has been improved in a few places.\r\n\r\n## Bugfixes\r\n - Fixed an issue causing decryption to hang when decrypting very large files (#90, thanks @batchmode!). \r\n - Password files are no longer deleted when encryption fails after editing.\r\n - Signed commits previously generated inconsistent line endings, which caused issues with some applications. This has been fixed (#94)."",
    ""mentions_count"": 2
  }
]
";
	}
}
