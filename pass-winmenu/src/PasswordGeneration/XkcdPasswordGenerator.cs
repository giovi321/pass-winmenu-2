using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PassWinmenu.Configuration;

#nullable enable
namespace PassWinmenu.PasswordGeneration
{
	/// <summary>
	/// Generates an XKCD-style passphrase ("correct horse battery staple"): several random words
	/// from a word list, joined by a separator, with configurable capitalisation and an optional
	/// trailing number. Word selection uses a cryptographically secure RNG.
	/// </summary>
	internal class XkcdPasswordGenerator
	{
		private readonly XkcdConfig config;
		private readonly string[] words;

		public XkcdPasswordGenerator(XkcdConfig config, string[] words)
		{
			this.config = config;
			this.words = words;
		}

		public string? GeneratePassword()
		{
			var pool = words
				.Where(w => w.Length >= config.MinWordLength && w.Length <= config.MaxWordLength)
				.ToArray();

			// If the length filter excludes everything, fall back to the full list rather than fail.
			if (pool.Length == 0)
			{
				pool = words;
			}

			if (pool.Length == 0)
			{
				return null;
			}

			var wordCount = Math.Max(1, config.WordCount);
			var builder = new StringBuilder();

			if (config.RandomNumberSeparator)
			{
				// A fresh random digit wraps every word, including the ends:
				//   <digit><word><digit><word>...<word><digit>   e.g. 8WORD1balcony9COLONY0
				// This overrides 'separator' and 'include-number'.
				for (var i = 0; i < wordCount; i++)
				{
					builder.Append(RandomDigit());
					builder.Append(Capitalise(NextWord(pool)));
				}

				builder.Append(RandomDigit());
			}
			else
			{
				var separator = config.Separator ?? string.Empty;
				for (var i = 0; i < wordCount; i++)
				{
					if (i > 0)
					{
						builder.Append(separator);
					}

					builder.Append(Capitalise(NextWord(pool)));
				}

				if (config.IncludeNumber)
				{
					builder.Append(separator);
					builder.Append(RandomNumberGenerator.GetInt32(10, 100));
				}
			}

			return builder.ToString();
		}

		private static string NextWord(string[] pool) => pool[RandomNumberGenerator.GetInt32(pool.Length)];

		private static string RandomDigit() => RandomNumberGenerator.GetInt32(10).ToString(CultureInfo.InvariantCulture);

		private string Capitalise(string word)
		{
			if (word.Length == 0)
			{
				return word;
			}

			switch (config.Capitalisation)
			{
				case XkcdCapitalisation.Upper:
					return word.ToUpperInvariant();
				case XkcdCapitalisation.First:
					return char.ToUpperInvariant(word[0]) + word.Substring(1);
				case XkcdCapitalisation.Random:
					// Each word is randomly either entirely upper-case or entirely lower-case.
					return RandomNumberGenerator.GetInt32(2) == 0
						? word.ToLowerInvariant()
						: word.ToUpperInvariant();
				case XkcdCapitalisation.None:
				default:
					return word;
			}
		}
	}
}
