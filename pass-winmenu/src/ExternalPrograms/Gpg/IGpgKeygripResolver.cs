namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal interface IGpgKeygripResolver
	{
		/// <summary>
		/// Returns the keygrip of the encryption-capable secret (sub)key, which is the one
		/// gpg-agent needs to decrypt password files. Returns null when no secret key is found.
		/// </summary>
		/// <param name="keyId">Optional key id to restrict the search to a specific key.</param>
		string? GetEncryptionKeygrip(string? keyId = null);
	}
}
