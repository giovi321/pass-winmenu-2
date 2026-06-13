using System;
using PassWinmenu.Actions;
using PassWinmenu.Configuration;
using PassWinmenu.Hotkeys;
using PassWinmenu.WinApi;

namespace PassWinmenu.Jobs;

internal class AssignHotkeys : IStartupJob
{
	private readonly HotkeyService hotkeyService;
	private readonly ActionDispatcher actionDispatcher;
	private readonly IDialogService dialogService;
	private readonly Config config;

	public AssignHotkeys(HotkeyService hotkeyService, ActionDispatcher actionDispatcher, IDialogService dialogService, Config config)
	{
		this.hotkeyService = hotkeyService;
		this.actionDispatcher = actionDispatcher;
		this.dialogService = dialogService;
		this.config = config;
	}

	/// <summary>
	/// Loads keybindings from the configuration file and registers them with Windows.
	/// </summary>
	public void Run()
	{
		try
		{
			hotkeyService.AssignHotkeys(config.Hotkeys, actionDispatcher);
		}
		catch (Exception e) when (e is HotkeyException)
		{
			Log.Send("Failed to register hotkeys", LogLevel.Error);
			Log.ReportException(e);

			dialogService.ShowErrorWindow(e.Message, "Could not register hotkeys");
			App.Exit();
		}
	}
}
