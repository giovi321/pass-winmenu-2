using System.Runtime.InteropServices;
using System.Text;
using Autofac;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities;

namespace PassWinmenu.CommandLine;

using PasswordManagement;

public class ShowCommand
{
	private readonly string configPath;
	private readonly string passwordPath;
	private readonly IPasswordManager passwordManager;

	public ShowCommand(string configPath, string passwordPath)
	{
		if (!passwordPath.EndsWith(PassWinmenu.Program.EncryptedFileExtension))
		{
			passwordPath += PassWinmenu.Program.EncryptedFileExtension;
		}

		this.passwordPath = passwordPath;
		this.configPath = configPath;
		this.passwordManager = CreateContainer().Resolve<IPasswordManager>();
	}

	private IContainer CreateContainer()
	{
		var loadResult = ConfigManager.Load(configPath, allowCreate: false);
		var configManager = loadResult switch
		{
			LoadResult.Success s => s.ConfigManager,
			LoadResult.NeedsUpgrade => throw new Exception("Outdated configuration file"),
			LoadResult.NotFound => throw new Exception("Configuration file not found"),
			_ => throw new ArgumentOutOfRangeException(),
		};

		return Setup.InitialiseCommandLine(configManager);
	}

	public void All()
	{
		var password = GetPasswordFile();
		var decrypted = passwordManager.DecryptPassword(password, false).Content;
		Console.WriteLine(decrypted);
	}

	public void Password()
	{
		var password = GetPasswordFile();
		var decrypted = passwordManager.DecryptPassword(password, true).Password;
		Console.WriteLine(decrypted);
	}

	public void Key(string key)
	{
		var password = GetPasswordFile();
		var keys = passwordManager.DecryptPassword(password, true).Keys;

		var matchingPair = keys
			.Select<KeyValuePair<string, string>, KeyValuePair<string, string>?>(p => p)
			.FirstOrDefault(k => string.Equals(k!.Value.Key, key, StringComparison.CurrentCultureIgnoreCase));
		if (matchingPair.HasValue)
		{
			Console.WriteLine(matchingPair.Value.Value);
		}
		else
		{
			Console.Error.WriteLine("Key does not exist!");
			Environment.Exit(1);
		}
	}

	private PasswordFile GetPasswordFile()
	{
		var file = passwordManager.QueryPasswordFile(passwordPath).ValueOrDefault();
		if (file == null)
		{
			Console.Error.WriteLine("Password does not exist!");
			Environment.Exit(1);
		}

		return file;
	}
}

public class EnrollCommand
{
	private readonly IContainer container;

	public EnrollCommand(string configPath)
	{
		var loadResult = ConfigManager.Load(configPath, allowCreate: false);
		var configManager = loadResult switch
		{
			LoadResult.Success s => s.ConfigManager,
			LoadResult.NeedsUpgrade => throw new Exception("Outdated configuration file"),
			LoadResult.NotFound => throw new Exception("Configuration file not found"),
			_ => throw new ArgumentOutOfRangeException(),
		};

		container = Setup.InitialiseCommandLine(configManager);
	}

	public void Run()
	{
		var vault = container.Resolve<IBiometricVault>();

		if (!Task.Run(() => vault.IsAvailableAsync()).GetAwaiter().GetResult())
		{
			Console.Error.WriteLine("Windows Hello is not available on this device. Set up a fingerprint or PIN in Windows settings first.");
			Environment.Exit(1);
			return;
		}

		var passphrase = ReadPassphrase("Enter your GPG passphrase: ");
		try
		{
			Task.Run(() => vault.EnrollAsync(passphrase)).GetAwaiter().GetResult();
			Console.WriteLine("Windows Hello unlock has been set up. You can now unlock your passwords with your fingerprint.");
		}
		catch (BiometricException e)
		{
			Console.Error.WriteLine($"Could not set up Windows Hello unlock: {e.Message}");
			Environment.Exit(1);
		}
		finally
		{
			Array.Clear(passphrase, 0, passphrase.Length);
		}
	}

	private static char[] ReadPassphrase(string prompt)
	{
		Console.Write(prompt);

		// Redirected/piped input: read raw bytes so the passphrase never becomes an immutable string.
		if (Console.IsInputRedirected)
		{
			using var stdin = Console.OpenStandardInput();
			using var buffer = new MemoryStream();
			stdin.CopyTo(buffer);
			var bytes = buffer.ToArray();
			try
			{
				return TrimTrailingNewline(Encoding.UTF8.GetChars(bytes));
			}
			finally
			{
				Array.Clear(bytes, 0, bytes.Length);
			}
		}

		var typed = new List<char>();
		try
		{
			while (true)
			{
				var key = Console.ReadKey(intercept: true);
				if (key.Key == ConsoleKey.Enter)
				{
					Console.WriteLine();
					break;
				}

				if (key.Key == ConsoleKey.Backspace)
				{
					if (typed.Count > 0)
					{
						typed.RemoveAt(typed.Count - 1);
					}

					continue;
				}

				if (!char.IsControl(key.KeyChar))
				{
					typed.Add(key.KeyChar);
				}
			}

			return typed.ToArray();
		}
		finally
		{
			// Zero the list's backing array so the passphrase doesn't linger after copying.
			CollectionsMarshal.AsSpan(typed).Clear();
		}
	}

	private static char[] TrimTrailingNewline(char[] chars)
	{
		var length = chars.Length;
		while (length > 0 && (chars[length - 1] == '\n' || chars[length - 1] == '\r'))
		{
			length--;
		}

		if (length == chars.Length)
		{
			return chars;
		}

		var trimmed = new char[length];
		Array.Copy(chars, trimmed, length);
		Array.Clear(chars, 0, chars.Length);
		return trimmed;
	}
}

public static class Commands
{
	public static void ShowAll(string configPath, string passwordPath)
	{
		new ShowCommand(configPath, passwordPath).All();
	}

	public static void ShowPassword(string configPath, string passwordPath)
	{
		new ShowCommand(configPath, passwordPath).Password();
	}

	public static void ShowKey(string configPath, string passwordPath, string key)
	{
		new ShowCommand(configPath, passwordPath).Key(key);
	}

	public static void Enroll(string configPath)
	{
		new EnrollCommand(configPath).Run();
	}
}
