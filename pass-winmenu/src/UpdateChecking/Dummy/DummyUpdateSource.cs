using System.Collections.Generic;

#nullable enable
namespace PassWinmenu.UpdateChecking.Dummy
{
	internal sealed class DummyUpdateSource : IUpdateSource
	{
		public List<ProgramVersion> Versions { get; set; } = new List<ProgramVersion>();

		public bool RequiresConnectivity => false;

		public IEnumerable<ProgramVersion> GetAllReleases()
		{
			return Versions;
		}
	}
}
