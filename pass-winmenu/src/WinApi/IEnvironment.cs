using System;

#nullable enable
namespace PassWinmenu.WinApi
{
	public interface IEnvironment
	{
		string? GetEnvironmentVariable(string variableName);
		string GetFolderPath(Environment.SpecialFolder folder);
	}
}
