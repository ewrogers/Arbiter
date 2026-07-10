using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Arbiter.App.Logging;
using Arbiter.App.Mappings;
using Arbiter.App.Services.Client;
using Arbiter.App.Services.Dialogs;
using Arbiter.App.Services.Entities;
using Arbiter.App.Services.Input;
using Arbiter.App.Services.Players;
using Arbiter.App.Services.Settings;
using Arbiter.App.Services.Sprites;
using Arbiter.App.Services.Tracing;
using Avalonia.Markup.Xaml;
using Arbiter.App.ViewModels;
using Arbiter.App.ViewModels.Client;
using Arbiter.App.ViewModels.Dialogs;
using Arbiter.App.ViewModels.Entities;
using Arbiter.App.ViewModels.Filters;
using Arbiter.App.ViewModels.Inspector;
using Arbiter.App.ViewModels.Logging;
using Arbiter.App.ViewModels.MessageBox;
using Arbiter.App.ViewModels.Proxy;
using Arbiter.App.ViewModels.Send;
using Arbiter.App.ViewModels.Tracing;
using Arbiter.App.Views;
using Arbiter.Net.Proxy;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arbiter.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Create the main window and register it as top level
            var window = new MainWindow();
            collection.AddSingleton<Window>(window);
            collection.AddSingleton<TopLevel>(window);
            collection.AddSingleton(window.StorageProvider);

            // Configure logging with our custom provider
            var loggerProvider = new ArbiterLoggerProvider();
            collection.AddSingleton(loggerProvider);
            
            collection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(loggerProvider);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Register all services and view models with the DI container
            RegisterServices(collection);
            RegisterViewModels(collection);

            // Build the service provider and get the main view model
            var services = collection.BuildServiceProvider();
            var vm = services.GetRequiredService<MainWindowViewModel>();
            window.DataContext = vm;

            // Set it as the main window to display
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    
    private static void RegisterServices(IServiceCollection services)
    {
        // Singletons
        services.AddSingleton<IEntityStore, EntityStore>();
        services.AddSingleton<IKeyboardService, KeyboardService>();
        services.AddSingleton<InspectorMappingRegistry>();
        services.AddSingleton<InspectorViewModelFactory>();
        services.AddSingleton<ProxyServer>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<IGameSpriteService, GameSpriteService>();
        services.AddSingleton(TimeProvider.System);
        
        // Transients
        services.AddTransient<IDialogService, DialogService>();
        services.AddTransient<IGameClientService, GameClientService>();
        services.AddTransient<ISettingsService, SettingsService>();
        services.AddTransient<ITraceService, TraceService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        // Singletons
        services.AddSingleton<ConsoleViewModel>();
        services.AddSingleton<ClientManagerViewModel>();
        services.AddSingleton<CrcCalculatorViewModel>();
        services.AddSingleton<DialogManagerViewModel>();
        services.AddSingleton<EntityManagerViewModel>();
        services.AddSingleton<InspectorViewModel>();
        services.AddSingleton<ProxyViewModel>();
        services.AddSingleton<SendPacketViewModel>();
        
        // Transients
        services.AddTransient<MessageBoxViewModel>();
        services.AddTransient<MessageFilterListViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TraceViewModel>();
    }
}
