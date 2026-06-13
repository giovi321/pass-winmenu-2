using System.Windows;
using PassWinmenu.Configuration;
using PassWinmenu.Notifications;
using PassWinmenu.Utilities;

namespace PassWinmenu.UpdateChecking;

internal class UpdateTracker : IUpdateTracker
{
	private readonly INotificationService notificationService;
	private readonly INotifyIcon notifyIcon;
	private readonly NotificationTypesConfig notificationTypes;

	public UpdateTracker(INotificationService notificationService, INotifyIcon notifyIcon, NotificationTypesConfig notificationTypes)
	{
		this.notificationService = notificationService;
		this.notifyIcon = notifyIcon;
		this.notificationTypes = notificationTypes;
	}
	
	public void HandleUpdateAvailable(UpdateAvailableEventArgs args)
	{
		Application.Current.Dispatcher.Invoke(() => HandleUpdateAvailableInternal(args));
	}

	private void HandleUpdateAvailableInternal(UpdateAvailableEventArgs args)
	{
		Helpers.AssertOnUiThread();
		
		notifyIcon.AddUpdate(args.Version);

		if (args.Version.Important && (notificationTypes.UpdateAvailable || notificationTypes.ImportantUpdateAvailable))
		{
			notificationService.Raise(
				$"An important vulnerability fix ({args.Version}) is available. Check the release for more information.",
				Severity.Info);
		}
		else if (notificationTypes.UpdateAvailable)
		{
			if (args.Version.IsPrerelease)
			{
				notificationService.Raise($"A new pre-release ({args.Version}) is available.", Severity.Info);
			}
			else
			{
				notificationService.Raise($"A new update ({args.Version}) is available.", Severity.Info);
			}
		}
	}
}
