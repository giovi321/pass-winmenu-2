using System;
using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using PassWinmenu.Actions;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Hotkeys;
using PassWinmenu.Jobs;
using PassWinmenu.Notifications;
using PassWinmenu.PasswordManagement;
using PassWinmenu.UpdateChecking;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu
{
	internal class DependenciesBuilder
	{
		private readonly ContainerBuilder builder = new();

		public DependenciesBuilder RegisterDesktopNotifications()
		{
			builder.Register(ctx => Notifications.Notifications.Create(ctx.Resolve<ActionDispatcher>(), ctx.Resolve<NotificationConfig>())).AsImplementedInterfaces().SingleInstance();
			builder.RegisterType<GraphicalDialogService>().AsImplementedInterfaces();
			
			return this;
		}
		
		public DependenciesBuilder RegisterCommandLineNotifications()
		{
			builder.RegisterType<StubNotificationService>().AsImplementedInterfaces();
			builder.RegisterType<CommandLineDialogService>().AsImplementedInterfaces();
			
			return this;
		}

		public DependenciesBuilder RegisterConfiguration(ConfigManager configManager)
		{
			builder.Register(_ => configManager).AsImplementedInterfaces().AsSelf();
			builder.Register(_ => configManager.ConfigurationFile).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Application).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Application.UpdateChecking).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Git).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Gpg).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Gpg.GpgAgent).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Gpg.GpgAgent.Config).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Gpg.Biometrics).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Interface).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Interface.PasswordEditor).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Notifications).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.Notifications.Types).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.PasswordStore).AsSelf();
			builder.Register(_ => configManager.ConfigurationFile.Config.PasswordStore.UsernameDetection).AsSelf();

			return this;
		}

		public DependenciesBuilder RegisterEnvironment()
		{
			// Register environment wrappers
			builder.RegisterTypes(
					typeof(FileSystem),
					typeof(SystemEnvironment),
					typeof(Processes),
					typeof(ExecutablePathResolver))
				.AsImplementedInterfaces();
			builder.Register(_ => EnvironmentVariables.LoadFromEnvironment()).AsSelf();

			return this;
		}

		public DependenciesBuilder RegisterActions()
		{
			// Register actions and hotkeys
			builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(ActionDispatcher)))
				.InNamespaceOf<ActionDispatcher>()
				.Except<ActionDispatcher>()
				.AsImplementedInterfaces()
				.AsSelf();
			builder.RegisterType<HotkeyService>()
				.AsSelf();
			builder.Register(_ => WindowsHotkeyRegistrar.Retrieve()).As<IHotkeyRegistrar>();

			builder.RegisterType<ActionDispatcher>().AsSelf();

			return this;
		}

		public DependenciesBuilder RegisterGpg()
		{
			// Register GPG types
			builder.RegisterTypes(
					typeof(GpgInstallationFinder),
					typeof(GpgHomeDirResolver),
					typeof(GpgAgentConfigReader),
					typeof(GpgAgentConfigUpdater),
					typeof(GpgTransport),
					typeof(GpgResultVerifier),
					typeof(GpgKeygripResolver),
					typeof(GpgAgentPresetService),
					typeof(GpgAgentControl),
					typeof(GPG))
				.AsImplementedInterfaces()
				.AsSelf();

			// Register GPG installation
			// Single instance, as there is no need to look for the same GPG installation multiple times.
			builder.Register(context => context.Resolve<GpgInstallationFinder>().FindGpgInstallation(context.Resolve<GpgConfig>().GpgPath))
				.SingleInstance();

			builder.Register(ctx => ctx.Resolve<GpgHomeDirResolver>().GetHomeDir())
				.SingleInstance();

			return this;
		}

		public DependenciesBuilder RegisterBiometrics()
		{
			// Windows Hello unlock services. The key store is the only type that touches WinRT.
			builder.RegisterTypes(
					typeof(KeyCredentialBiometricKeyStore),
					typeof(PassphraseProtector),
					typeof(FileBiometricBlobStore),
					typeof(BiometricVault))
				.AsImplementedInterfaces()
				.AsSelf();

			// Single instance so the short-lived passphrase cache (cache cadence) survives
			// across decryptions.
			builder.RegisterType<BiometricPassphraseProvider>()
				.AsImplementedInterfaces()
				.AsSelf()
				.SingleInstance();

			return this;
		}

		public DependenciesBuilder RegisterGit()
		{
			// Create the Git wrapper, if enabled.
			// This needs to be a single instance to stop startup warnings being displayed multiple times.
			builder.RegisterType<GitSyncStrategies>().AsSelf();
			builder.Register(CreateSyncService)
				.AsSelf()
				.SingleInstance();

			builder.Register(
					context => UpdateCheckerFactory.CreateUpdateChecker(
						context.Resolve<UpdateCheckingConfig>(),
						context.Resolve<IUpdateTracker>()))
				.SingleInstance();
			builder.RegisterType<RemoteUpdateCheckerFactory>().AsSelf();
			builder.Register(context => context.Resolve<RemoteUpdateCheckerFactory>().Build()).AsSelf().SingleInstance();

			return this;
		}

		public DependenciesBuilder RegisterApplication()
		{
			// Register user interaction types
			builder.RegisterType<DialogCreator>()
				.AsSelf();
			builder.RegisterType<PathDisplayService>()
				.AsSelf();

			// Register the internal password manager
			builder.Register(context => context.Resolve<IFileSystem>().DirectoryInfo.New(context.Resolve<PasswordStoreConfig>().Location))
				.Named("PasswordStore", typeof(IDirectoryInfo));

			builder.RegisterType<GpgRecipientFinder>().WithParameter(
					(parameter, context) => parameter.ParameterType == typeof(IDirectoryInfo),
					(parameter, context) => context.ResolveNamed<IDirectoryInfo>("PasswordStore"))
				.AsImplementedInterfaces();

			builder.RegisterType<PasswordManager>().WithParameter(
					(parameter, context) => parameter.ParameterType == typeof(IDirectoryInfo),
					(parameter, context) => context.ResolveNamed<IDirectoryInfo>("PasswordStore"))
				.AsImplementedInterfaces()
				.AsSelf();

			builder.RegisterType<PasswordFileParser>().AsSelf();
			builder.RegisterType<TemporaryClipboard>().AsSelf();
			builder.RegisterType<UpdateTracker>().AsImplementedInterfaces();

			return this;
		}

		public DependenciesBuilder RegisterJobs()
		{
			builder.RegisterTypes(
					typeof(AssignHotkeys),
					typeof(EnableConfigReloading),
					typeof(PreloadGpgAgent),
					typeof(StartRemoteUpdateChecker),
					typeof(StartUpdateChecker),
					typeof(UpdateGpgAgentConfig),
					typeof(BiometricUnlockJob))
				.AsImplementedInterfaces()
				.AsSelf();

			return this;
		}

		public IContainer Build()
		{
			return builder.Build();
		}

		private static Option<ISyncService> CreateSyncService(IComponentContext context)
		{
			var config = context.Resolve<GitConfig>();
			var signService = context.Resolve<ISignService>();
			var passwordStore = context.ResolveNamed<IDirectoryInfo>("PasswordStore");
			var dialogService = context.Resolve<IDialogService>();
			var strategies = context.Resolve<GitSyncStrategies>();

			var factory = new SyncServiceFactory(config, passwordStore, signService, strategies);

			try
			{
				var syncService = factory.BuildSyncService();
				if (factory.Status == SyncServiceStatus.GitLibraryNotFound)
				{
					dialogService.ShowErrorWindow(
						"The git2 DLL could not be found. Git support will be disabled.");
				}

				return Option.FromNullable(syncService);
			}
			catch (Exception e)
			{
				dialogService.ShowErrorWindow(
					$"Failed to open the password store Git repository ({e.GetType().Name}: {e.Message}). Git support will be disabled.");
			}

			return Option<ISyncService>.None;
		}
	}
}
