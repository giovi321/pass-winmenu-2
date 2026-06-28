using System.Threading.Tasks;
using PassWinmenu.Biometrics;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Biometrics
{
	// Headless (no WPF Application.Current): exercises the no-window fast path.
	public class WindowsHelloPromptTests
	{
		[Fact]
		public void RunAsync_WithoutWpfApp_InvokesHelloCallOnceAndReturnsResult()
		{
			var calls = 0;

			var result = WindowsHelloPrompt.RunAsync(
				"unlock",
				ct =>
				{
					calls++;
					return Task.FromResult(7);
				}).GetAwaiter().GetResult();

			result.ShouldBe(7);
			calls.ShouldBe(1);
		}
	}
}
