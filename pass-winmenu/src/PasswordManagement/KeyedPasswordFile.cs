using System.Collections.Generic;

#nullable enable
namespace PassWinmenu.PasswordManagement
{
	/// <summary>
	/// Represents a decrypted password file of the 'password store' format
	/// which additionally stores a number of key-value pairs in its metadata.
	/// </summary>
	internal class KeyedPasswordFile : ParsedPasswordFile
	{
		public List<KeyValuePair<string, string>> Keys { get; }

		public KeyedPasswordFile(PasswordFile original, string password, string? metadata, List<KeyValuePair<string, string>>? keys)
			: base(original, password, metadata)
		{
			Keys = keys ?? new List<KeyValuePair<string, string>>();
		}
	}
}
