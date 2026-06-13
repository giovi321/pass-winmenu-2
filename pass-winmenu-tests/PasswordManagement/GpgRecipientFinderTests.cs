using System.IO.Abstractions.TestingHelpers;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.PasswordManagement
{
	public class GpgRecipientFinderTests
	{
		[Fact]
		public void FindRecipient_GpgIdInDirectory_GetsRecipientsFromGpgId()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\sub\.gpg-id", "test_recipient_1\ntest_recipient_2")
				.Build();
			var passwordStore = new MockDirectoryInfo(fileSystem, @"C:\password-store");
			var recipientFinder = new GpgRecipientFinder(passwordStore, new EnvironmentVariables());
			var passwordFile = new PasswordFile(fileSystem.FileInfo.New(@"C:\password-store\sub\password"), passwordStore);

			var recipients = recipientFinder.FindRecipients(passwordFile);

			recipients.ShouldBe(new []
			{
				"test_recipient_1",
				"test_recipient_2"
			});
		}

		[Fact]
		public void FindRecipient_GpgIdInParentDirectory_GetsRecipientsFromGpgId()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\.gpg-id", "test_recipient_1\ntest_recipient_2")
				.Build();
			var passwordStore = new MockDirectoryInfo(fileSystem, @"C:\password-store");
			var recipientFinder = new GpgRecipientFinder(passwordStore, new EnvironmentVariables());
			var passwordFile = new PasswordFile(fileSystem.FileInfo.New(@"C:\password-store\sub\password"), passwordStore);

			var recipients = recipientFinder.FindRecipients(passwordFile);

			recipients.ShouldBe(new []
			{
				"test_recipient_1",
				"test_recipient_2"
			});
		}

		[Fact]
		public void FindRecipient_NoGpgId_ReturnsEmptyArray()
		{
			var fileSystem = new MockFileSystemBuilder()
				.Build();
			var passwordStore = new MockDirectoryInfo(fileSystem, @"C:\password-store");
			var recipientFinder = new GpgRecipientFinder(passwordStore, new EnvironmentVariables());
			var passwordFile = new PasswordFile(fileSystem.FileInfo.New(@"C:\password-store\sub\password"), passwordStore);

			var recipients = recipientFinder.FindRecipients(passwordFile);

			recipients.ShouldBeEmpty();
		}

		[Fact]
		public void FindRecipient_PasswordStoreKeyInEnvironment_OverridesGpgId()
		{
			var fileSystem = new MockFileSystemBuilder()
				.WithFile(@"C:\password-store\sub\.gpg-id", "test_recipient_1\ntest_recipient_2")
				.Build();
			var passwordStore = new MockDirectoryInfo(fileSystem, @"C:\password-store");
			var recipientFinder = new GpgRecipientFinder(passwordStore, new EnvironmentVariables {
				PasswordStoreKey = "test_recipient_3 test_recipient_4",
			});
			var passwordFile = new PasswordFile(fileSystem.FileInfo.New(@"C:\password-store\sub\password"), passwordStore);

			var recipients = recipientFinder.FindRecipients(passwordFile);

			recipients.ShouldBe(new[]
			{
				"test_recipient_3",
				"test_recipient_4"
			});
		}
	}
}
