using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PassWinmenu.Utilities
{
	/// <summary>
	/// Helpers for synchronously waiting on asynchronous work without freezing the WPF UI thread.
	/// </summary>
	internal static class UiThread
	{
		/// <summary>
		/// Runs <paramref name="work"/> on a background thread and waits for it. When called on the
		/// WPF UI thread, the dispatcher is pumped while waiting, so the UI stays responsive and
		/// background threads can still marshal back to the UI thread (e.g. to show the Windows Hello
		/// prompt window). Hard-blocking instead would deadlock that marshalling.
		/// </summary>
		public static T RunBlocking<T>(Func<Task<T>> work)
		{
			var dispatcher = Application.Current?.Dispatcher;
			var task = Task.Run(work);

			if (dispatcher != null && dispatcher.CheckAccess())
			{
				var frame = new DispatcherFrame();
				task.ContinueWith(
					_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)),
					TaskScheduler.Default);
				Dispatcher.PushFrame(frame);
			}

			return task.GetAwaiter().GetResult();
		}

		public static void RunBlocking(Func<Task> work)
		{
			RunBlocking(async () =>
			{
				await work();
				return true;
			});
		}
	}
}
