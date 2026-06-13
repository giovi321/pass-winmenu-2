using System.Collections.Generic;
using PassWinmenu.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Utilities;

public class SearchTests
{
	[Fact]
	public void Match_SingleCharacter_MatchesCandidatesContainingCharacter()
	{
		var candidates = "The quick brown fox jumped over the lazy dog".Split(" ");

		var matches = Search.Match(candidates, "o");

		matches.ShouldBe(new List<string> {"brown", "fox", "over", "dog"});
	}
	
	[Fact]
	public void Match_SpaceSeparated_MatchesAnywhereInCandidate()
	{
		var candidates = "one/two three/four five/six seven/eight".Split(" ");

		var matches = Search.Match(candidates, "en ei");

		matches.ShouldBe(new List<string> {"seven/eight"});
	}
	
	[Fact]
	public void Match_Lowercase_MatchesAny()
	{
		var candidates = "foo Foo fOo foO FOo fOO FOO".Split(" ");

		var matches = Search.Match(candidates, "foo");

		matches.ShouldBe(new List<string> {"foo", "Foo", "fOo", "foO", "FOo", "fOO", "FOO"});
	}
	
	[Fact]
	public void Match_QueryContainsUppercase_MatchesExactly()
	{
		var candidates = "foo Foo fOo foO FOo fOO FOO FoO".Split(" ");

		var matches = Search.Match(candidates, "oO");

		matches.ShouldBe(new List<string> {"foO", "FoO"});
	}
}
