using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using PassWinmenu.Biometrics;

namespace PassWinmenuTests.Utilities
{
	/// <summary>
	/// A deterministic, in-memory <see cref="IBiometricKeyStore"/> for tests. Signing depends
	/// only on <see cref="SignatureSeed"/> and the challenge, so it is repeatable. Changing the
	/// seed simulates a Windows Hello key being reset/invalidated.
	/// </summary>
	internal sealed class FakeBiometricKeyStore : IBiometricKeyStore
	{
		public bool Available { get; set; } = true;
		public bool Exists { get; set; }
		public byte[] SignatureSeed { get; set; } = { 1, 2, 3, 4 };

		/// <summary>Number of times <see cref="SignAsync"/> was called (i.e. Hello gestures requested).</summary>
		public int SignCount { get; private set; }

		public void ResetSignCount() => SignCount = 0;

		public Task<bool> IsAvailableAsync() => Task.FromResult(Available);

		public Task<bool> ExistsAsync(string credentialName) => Task.FromResult(Exists);

		public Task CreateAsync(string credentialName)
		{
			Exists = true;
			return Task.CompletedTask;
		}

		public Task<byte[]> SignAsync(string credentialName, byte[] challenge)
		{
			SignCount++;
			var input = new byte[SignatureSeed.Length + challenge.Length];
			Buffer.BlockCopy(SignatureSeed, 0, input, 0, SignatureSeed.Length);
			Buffer.BlockCopy(challenge, 0, input, SignatureSeed.Length, challenge.Length);
			return Task.FromResult(SHA256.HashData(input));
		}

		public Task DeleteAsync(string credentialName)
		{
			Exists = false;
			return Task.CompletedTask;
		}
	}

	/// <summary>An in-memory <see cref="IBiometricBlobStore"/> for tests.</summary>
	internal sealed class InMemoryBlobStore : IBiometricBlobStore
	{
		private byte[]? blob;

		public bool Exists() => blob != null;

		public byte[] Read() => blob ?? throw new InvalidOperationException("No blob stored.");

		public void Write(byte[] value) => blob = value;

		public void Delete() => blob = null;
	}
}
