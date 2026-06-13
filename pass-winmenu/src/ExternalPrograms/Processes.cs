using System.Diagnostics;
using System.Linq;

namespace PassWinmenu.ExternalPrograms
{
	internal class Processes : IProcesses
	{
		public IProcess Start(ProcessStartInfo psi)
		{
			return new ProcessWrapper(Process.Start(psi));
		}
	}
}
