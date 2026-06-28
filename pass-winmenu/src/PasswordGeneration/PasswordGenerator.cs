using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PassWinmenu.Configuration;

#nullable enable
namespace PassWinmenu.PasswordGeneration
{
	internal class PasswordGenerator
	{
		public PasswordGenerationConfig Options { get; }

		public PasswordGenerator(PasswordGenerationConfig options)
		{
			Options = options;
		}

		public string? GeneratePassword()
		{
			return GeneratePassword(Options.Length, Options.SpecialCharacters.Enabled);
		}

		/// <summary>
		/// Generates a password of approximately <paramref name="targetLength"/> characters. For the
		/// random generator this is the exact character count; for the XKCD generator the word count is
		/// derived from it (see <see cref="ComputeXkcdWordCount"/>), so the realised length is close but
		/// not exact. When <paramref name="includeSpecialCharacters"/> is true the configured
		/// special-character rule is applied on top of the generated password.
		/// </summary>
		public string? GeneratePassword(int targetLength, bool includeSpecialCharacters)
		{
			var password = GenerateBase(targetLength);
			if (password == null)
			{
				return null;
			}

			return includeSpecialCharacters ? ApplySpecialCharacters(password) : password;
		}

		/// <summary>
		/// Generates just the base password (no special characters) of approximately
		/// <paramref name="targetLength"/> characters. The UI keeps this base separate so that toggling
		/// the special-character switch only adds/removes characters instead of regenerating.
		/// </summary>
		public string? GenerateBase(int targetLength)
		{
			return Options.Style == PasswordGenerationStyle.Xkcd
				? GenerateXkcd(ComputeXkcdWordCount(targetLength))
				: GenerateRandom(targetLength);
		}

		/// <summary>
		/// Derives how many words an XKCD passphrase needs to reach roughly <paramref name="targetLength"/>
		/// characters, using the configured separator and the midpoint of the word-length band.
		/// </summary>
		internal int ComputeXkcdWordCount(int targetLength)
		{
			var xkcd = Options.Xkcd;
			var separatorLength = xkcd.RandomNumberSeparator ? 1 : (xkcd.Separator?.Length ?? 0);
			var midWordLength = Math.Max(1, (xkcd.MinWordLength + xkcd.MaxWordLength) / 2);
			var wordCount = (int)Math.Round((double)(targetLength + separatorLength) / (midWordLength + separatorLength));
			return Math.Max(1, wordCount);
		}

		private string? GenerateXkcd(int wordCount)
		{
			var words = LoadWordList(Options.Xkcd.WordListFile);
			return new XkcdPasswordGenerator(Options.Xkcd, words, wordCount).GeneratePassword();
		}

		private static string[] LoadWordList(string? wordListFile)
		{
			if (!string.IsNullOrWhiteSpace(wordListFile) && File.Exists(wordListFile))
			{
				return File.ReadAllLines(wordListFile)
					.Select(w => w.Trim())
					.Where(w => w.Length > 0)
					.ToArray();
			}

			return EmbeddedResources.LoadWordList();
		}

		private string? GenerateRandom(int length)
		{
			if (!Options.CharacterGroups.Any(g => g.Enabled))
			{
				return null;
			}

			// Build a complete set of all characters in all enabled groups
			var completeCharSet = new HashSet<int>();
			foreach (var group in Options.CharacterGroups.Where(g => g.Enabled))
			{
				completeCharSet.UnionWith(group.CharacterSet);
			}

			// Transform the set into a list, to assign an index to each character.
			var charList = completeCharSet.ToList();

			// Generate as many random list indices as we need to build a password.
			var indices = GetIntegers(charList.Count, length);

			// Transform the list of indices into a list of characters.
			var characters = indices.Select(i => charList[i]).ToArray();

			var password = string.Join("", characters.Select(char.ConvertFromUtf32));
			return password;
		}

		/// <summary>
		/// Adds the configured special characters to <paramref name="password"/>. The caller decides
		/// whether the rule is active; this method only applies the count/placement.
		/// </summary>
		public string ApplySpecialCharacters(string password)
		{
			var special = Options.SpecialCharacters;
			if (special.Count <= 0 || string.IsNullOrEmpty(special.Characters))
			{
				return password;
			}

			var pool = special.Characters;
			var result = new StringBuilder(password);
			for (var i = 0; i < special.Count; i++)
			{
				var ch = pool[(int)GetRandomInteger((uint)pool.Length)];
				switch (special.Placement)
				{
					case SpecialCharacterPlacement.Start:
						result.Insert(0, ch);
						break;
					case SpecialCharacterPlacement.Random:
						result.Insert((int)GetRandomInteger((uint)(result.Length + 1)), ch);
						break;
					case SpecialCharacterPlacement.End:
					default:
						result.Append(ch);
						break;
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// Generates a list of cryptographically secure randomly generated integers.
		/// </summary>
		private IEnumerable<int> GetIntegers(int max, int count)
		{
			for (var i = 0; i < count; i++)
			{
				yield return (int)GetRandomInteger((uint)max);
			}
		}

		/// <summary>
		/// Generates a cryptographically secure random number less than the given maximum value.
		/// </summary>
		private uint GetRandomInteger(uint maxValue)
		{
			var randomInteger = GetRandomUint64();

			// Example: Generating five random bits and interpreting them as an integer
			// would result in a number >= 0 and < 32. If maxValue is 15 here,
			// taking the remainder of the random number and maxValue limits its value to 14.
			// There are only 2 values that result in 14 (14 and 29). See:
			//    14 % 15 == 14
			//    29 % 15 == 14
			// (the next possible value, 44, cannot be generated from 5 random bits).
			// However, there are 3 values that produce 1:
			//	  1  % 15 == 1
			//    16 % 15 == 1
			//    31 % 15 == 1
			// (the same applies to zero, with the values of 0, 15, and 30).
			// This means that the the set of all inputs from 0 up to 32 would generate three
			// zeros, three ones, and two of all other numbers. In other words, when generating
			// a random input, its output is 50% more likely to be a zero than some number
			// that is not zero or one.
			// The solution is to discard the outputs of 30 and 31, and to try again.
			// To find the range that should be discarded, first find out how long this range is,
			// by taking the remainder of its length, and the value to be generated.
			// (31 % 30 == 1)
			var incompleteRangeLength = ulong.MaxValue % maxValue;
			// Then subtract that value from the maximum:
			// (31 - 1 == 30)
			var incompleteRangeStart = ulong.MaxValue - incompleteRangeLength;
			// If the number we generated is greater than or equal to the start of the incomplete
			// range, we should try again.
			// (if randomInteger >= 30)
			if (randomInteger >= incompleteRangeStart)
			{
				return GetRandomInteger(maxValue);
			}

			// To limit the chance of this happening, we don't use 5-bit integers, and instead use
			// the largest possible integer size on which we can still easily perform integer math,
			// which is ulong (int64).

			return (uint) (randomInteger % maxValue);
		}

		private ulong GetRandomUint64()
		{
			var bytes = RandomNumberGenerator.GetBytes(8);
			return BitConverter.ToUInt64(bytes, 0);
		}
	}
}
