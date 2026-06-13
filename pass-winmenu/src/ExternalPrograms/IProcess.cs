using System;
using System.IO;

namespace PassWinmenu.ExternalPrograms
{
	public interface IProcess
	{
		int Id { get; }
		StreamWriter StandardInput { get; }
		StreamReader StandardOutput { get; }
		StreamReader StandardError { get; }
		int ExitCode { get; }

		void Kill();
		bool WaitForExit(TimeSpan timeout);
	}
}
