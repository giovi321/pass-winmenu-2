using System.Diagnostics;
using PassWinmenu.ExternalPrograms;

namespace PassWinmenuTests.Utilities
{
	internal class FakeProcesses : IProcesses
	{
		private readonly FakeProcess process;

		public FakeProcesses(FakeProcess process)
		{
			this.process = process;
		}

		public IProcess Start(ProcessStartInfo psi)
		{
			return process;
		}
	}
}
