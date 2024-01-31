using System.Runtime.InteropServices;
using by.illusion21.Communication;
using by.illusion21.Platforms;
using by.illusion21.Platforms.Linux;
using by.illusion21.Platforms.Windows;
using by.illusion21.Services;
using by.illusion21.Services.Common.Types;
using by.illusion21.Services.EventBus;
using by.illusion21.Utilities.Common;
using by.illusion21.Utilities.IO;
using Microsoft.Extensions.DependencyInjection;

namespace by.illusion21;

internal abstract class PalWorldServerMg {
    private static EventBus<CommandEventType> _eventBus = null!;
    public static readonly KookMessage Channel = new();
    public static ConfigHandler? Config;

    public static async Task Main() {
        _eventBus = new EventBus<CommandEventType>();
        Config = new ConfigHandler();
        var serviceCollection = new ServiceCollection();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ConfigureService(serviceCollection, "Windows");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            ConfigureService(serviceCollection, "Linux");
        else
            throw new PlatformNotSupportedException("Unsupported operating system.");

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var daemon = serviceProvider.GetService<IDaemon>();
        var commandHandler = new CommandHandler(_eventBus);
        var commandHandlerTask = Task.Run(() => commandHandler.StartAsync());
        var daemonTask = Task.Run(() => daemon!.Run());
        await Task.WhenAll(commandHandlerTask, daemonTask);
        Log.WriteLine("Exiting main thread", LogType.Warn);
    }


    private static void ConfigureService(IServiceCollection services, string platform) {
        services.AddSingleton(_eventBus);
        services.AddTransient<ICommandEventHandler, CommandEventHandler>();
        switch (platform) {
            case "Windows":
                services.AddTransient<IDaemon, WindowsDaemon>();
                break;
            case "Linux":
                services.AddTransient<IDaemon, LinuxDaemon>();
                break;
        }
    }
}