using System.Collections.Generic;
using System.IO;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class StreamExtensions
	{
		public static List<string> ReadAllLines(this StreamReader reader)
		{
			var lines = new List<string>(8);
			var line = reader.ReadLine();
			while (line != null)
			{
				lines.Add(line);
				line = reader.ReadLine();
			}

			return lines;
		}
	}
}
