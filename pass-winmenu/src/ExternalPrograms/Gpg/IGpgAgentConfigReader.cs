namespace PassWinmenu.ExternalPrograms.Gpg
{
	public interface IGpgAgentConfigReader
	{
		string[] ReadConfigLines();
		void WriteConfigLines(string[] lines);
	}
}
