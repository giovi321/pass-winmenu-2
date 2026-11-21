using System.CommandLine;

namespace PassWinmenu.CommandLine;

public class CommandBuilder
{
	private readonly Option<string> configFile;

	private CommandBuilder()
	{
		RootCommand = new RootCommand("Commandline companion to pass-winmenu");
		var defaultConfigPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "pass-winmenu.yaml");
		configFile = new Option<string>("--config-file")
		{
			Description = "Path to the configuration file, relative to the main executable",
			DefaultValueFactory = _ => defaultConfigPath,
			Recursive = true,
		};
		RootCommand.Options.Add(configFile);
	}

	private RootCommand RootCommand { get; }

	private CommandBuilder AddList()
	{
		var list = new Command("list", "List passwords");
		var path = new Argument<string>("path") {Description = "Search path", Arity = ArgumentArity.ZeroOrOne};
		list.Arguments.Add(path);

		return this;
	}
	
	private CommandBuilder AddShow()
	{
		var show = new Command("show", "Show a password");
		var path = new Argument<string>("path") {Description = "Path to the password to be shown"};

		var all = new Command("all", "Show an entire password file, including metadata");
		all.Arguments.Add(path);
		all.SetAction(r => Commands.ShowAll(r.GetValue(configFile)!, r.GetValue(path)!));
		
		var password = new Command("password", "Show a password");
		password.Arguments.Add(path);
		password.SetAction(r => Commands.ShowPassword(r.GetValue(configFile)!, r.GetValue(path)!));
		
		var key = new Command("key", "Show the value of a key");
		var keyArg = new Argument<string>("key") {Description = "Name of the key to be shown"};
		key.Arguments.Add(keyArg);
		key.Arguments.Add(path);
		key.SetAction(r => Commands.ShowKey(r.GetValue(configFile)!, r.GetValue(path)!, r.GetValue(keyArg)!));
		
		show.Subcommands.Add(all);
		show.Subcommands.Add(password);
		show.Subcommands.Add(key);

		RootCommand.Add(show);

		return this;
	}

	private CommandBuilder AddEnroll()
	{
		var enroll = new Command("enroll", "Set up Windows Hello (fingerprint) unlock for your GPG passphrase");
		enroll.SetAction(r => Commands.Enroll(r.GetValue(configFile)!));
		RootCommand.Add(enroll);

		return this;
	}

	public static RootCommand Build()
	{
		return new CommandBuilder()
			.AddList()
			.AddShow()
			.AddEnroll()
			.RootCommand;
	}
}
