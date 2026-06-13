namespace PassWinmenu.Configuration
{
	public class UsernameDetectionOptions
	{
		public int LineNumber { get; set; } = 2;
		public string Regex { get; set; } = @"^[Uu]sername: ((?<username>.*)\r|(?<username>.*))$";
		public UsernameDetectionRegexOptions RegexOptions { get; set; } = new();
	}
}
