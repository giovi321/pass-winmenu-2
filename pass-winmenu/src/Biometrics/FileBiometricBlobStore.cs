using System.IO.Abstractions;
using System.Security.Cryptography;
using PassWinmenu.Configuration;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Stores the encrypted passphrase blob next to the configuration file
/// (<c>biometric.blob</c>), keeping all of pass-winmenu's user state in one place.
/// The (already Hello-wrapped) blob is additionally protected with DPAPI in user scope, so the
/// file on disk is bound to the Windows user account and is useless if copied to another account
/// or machine, even before the Windows Hello layer is considered.
/// </summary>
internal sealed class FileBiometricBlobStore : IBiometricBlobStore
{
	private const string BlobFileName = "biometric.blob";

	private readonly IFileSystem fileSystem;
	private readonly string blobPath;

	public FileBiometricBlobStore(IFileSystem fileSystem, ConfigurationFile configurationFile)
	{
		this.fileSystem = fileSystem;

		var directory = fileSystem.Path.GetDirectoryName(configurationFile.Path);
		blobPath = string.IsNullOrEmpty(directory)
			? BlobFileName
			: fileSystem.Path.Combine(directory, BlobFileName);
	}

	public bool Exists() => fileSystem.File.Exists(blobPath);

	public byte[] Read()
	{
		var protectedBlob = fileSystem.File.ReadAllBytes(blobPath);
		// Throws CryptographicException if the blob belongs to another user, the DPAPI keys have
		// changed, or it is a pre-DPAPI blob — the vault treats that as needing re-enrolment.
		return ProtectedData.Unprotect(protectedBlob, null, DataProtectionScope.CurrentUser);
	}

	public void Write(byte[] blob)
	{
		var protectedBlob = ProtectedData.Protect(blob, null, DataProtectionScope.CurrentUser);
		fileSystem.File.WriteAllBytes(blobPath, protectedBlob);
	}

	public void Delete()
	{
		if (fileSystem.File.Exists(blobPath))
		{
			fileSystem.File.Delete(blobPath);
		}
	}
}
