using System.Collections.Generic;
using System.IO.Abstractions;
using PassWinmenu.Utilities;

namespace PassWinmenu.PasswordManagement
{
	internal interface IPasswordManager
	{
		IDirectoryInfo PasswordStore { get; }

		IEnumerable<PasswordFile> GetPasswordFiles();

		Option<PasswordFile> QueryPasswordFile(string path);

		KeyedPasswordFile DecryptPassword(PasswordFile file, bool passwordOnFirstLine);

		PasswordFile EncryptPassword(DecryptedPasswordFile file);
		
		PasswordFile AddPassword(string path, string password, string metadata);
	}
}
