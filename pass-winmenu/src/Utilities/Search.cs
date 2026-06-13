using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PassWinmenu.Utilities;

public static class Search
{
	public static IEnumerable<string> Match(IEnumerable<string> candidates, string query)
	{
		// We split on spaces to allow the user to quickly search for a certain term, as it allows them
		// to search, for example, for site.com/username by entering "si us"
		var terms = query.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

		return candidates.Where(
			key =>
			{
				var lcOption = key.ToLower(CultureInfo.CurrentCulture);
				return terms.All(
					term =>
					{
						// Perform case-sensitive matching if the user entered an uppercase character.
						if (term.Any(char.IsUpper))
						{
							if (key.Contains(term))
							{
								return true;
							}
						}
						else
						{
							if (lcOption.Contains(term))
							{
								return true;
							}
						}

						return false;
					});
			});
	}
}
