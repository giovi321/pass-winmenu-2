using System.Threading.Tasks;
using PassWinmenu.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Utilities
{
	// These run headless (no WPF Application.Current), so they exercise the
	// no-WPF-app fast path of RunOnUi.
	public class UiThreadTests
	{
		[Fact]
		public void RunOnUi_Generic_WithoutWpfApp_RunsWorkAndReturnsResult()
		{
			var result = UiThread.RunOnUi(() => Task.FromResult(42));

			result.ShouldBe(42);
		}

		[Fact]
		public void RunOnUi_Void_WithoutWpfApp_RunsWork()
		{
			var ran = false;

			UiThread.RunOnUi(() =>
			{
				ran = true;
				return Task.CompletedTask;
			});

			ran.ShouldBeTrue();
		}
	}
}
