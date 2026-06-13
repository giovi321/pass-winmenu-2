using System.Diagnostics;

namespace PassWinmenu.ExternalPrograms
{
	internal interface IProcesses
	{
		IProcess Start(ProcessStartInfo psi);
	}
}
