using System.CommandLine;

namespace PassWinmenu.CommandLine;

public class Program
{
	private static void Main(string[] args)
	{
		CommandBuilder.Build().Parse(args).Invoke();
	}
}
