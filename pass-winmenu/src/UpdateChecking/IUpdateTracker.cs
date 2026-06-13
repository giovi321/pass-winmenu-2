namespace PassWinmenu.UpdateChecking;

public interface IUpdateTracker
{
	void HandleUpdateAvailable(UpdateAvailableEventArgs args);
}
