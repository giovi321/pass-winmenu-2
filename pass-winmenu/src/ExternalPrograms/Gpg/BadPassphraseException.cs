namespace PassWinmenu.ExternalPrograms.Gpg
{
	/// <summary>
	/// Thrown when GPG reports that the supplied passphrase was wrong (BAD_PASSPHRASE). This is
	/// distinguished from generic decryption failures so callers (e.g. the biometric path) can
	/// react by re-enrolling and falling back to a normal pinentry prompt.
	/// </summary>
	internal class BadPassphraseException : GpgError
	{
		public BadPassphraseException(string message) : base(message) { }
	}
}
