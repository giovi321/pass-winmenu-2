namespace PassWinmenu.Biometrics;

/// <summary>
/// Persists the encrypted passphrase blob. Abstracted over the filesystem so it can be
/// mocked in tests.
/// </summary>
internal interface IBiometricBlobStore
{
	bool Exists();
	byte[] Read();
	void Write(byte[] blob);
	void Delete();
}
