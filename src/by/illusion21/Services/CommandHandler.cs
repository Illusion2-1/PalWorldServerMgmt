using by.illusion21.Services.Common.Types;
using by.illusion21.Services.EventBus;
using by.illusion21.Utilities.Common;

namespace by.illusion21.Services;

public class CommandHandler {
    private readonly Dictionary<string, Func<string[], Task>> _commands;
    private readonly EventBus<CommandEventType> _eventBus;
    private bool _isRunning = true;

    public CommandHandler(EventBus<CommandEventType> eventBus) {
        _eventBus = eventBus;
        _commands = new Dictionary<string, Func<string[], Task>>();
        RegisterCommands();
    }

    public async Task StartAsync() {
        var task = Task.Run(async () => {
            while (_isRunning) {
                var input = await Console.In.ReadLineAsync() ?? string.Empty;
                var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var command = parts.Length > 0 ? parts[0] : null;
                var arguments = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

                if (command != null && _commands.TryGetValue(command, out var action))
                    await action.Invoke(arguments);
                else
                    Log.WriteLine($"未知命令: {command}\n使用help获取可用命令", LogType.Error);
            }
        });
        await task;
        Log.WriteLine("Exiting command handler thread.", LogType.Warn);
    }

    private void RegisterCommands() {
        Log.WriteLine("Registering publisher", LogType.Warn);
        _commands["exit"] = async _ => {
            _isRunning = false;
            _eventBus.Publish(CommandEventType.EventExit);
            await Task.CompletedTask;
        };

        _commands["backup"] = async _ => {
            _eventBus.Publish(CommandEventType.EventBackup);
            await Task.CompletedTask;
        };

        _commands["restart"] = async _ => {
            _eventBus.Publish(CommandEventType.EventRestart);
            await Task.CompletedTask;
        };

        _commands["stats"] = async _ => {
            Log.WriteLine("Trying to publish", LogType.Warn);
            _eventBus.Publish(CommandEventType.EventStat);
            await Task.CompletedTask;
        };

        _commands["help"] = async _ => {
            Console.WriteLine("可用命令:");
            foreach (var cmd in _commands.Keys) Console.WriteLine($"- {cmd}");
            await Task.CompletedTask;
        };

        _commands["echo"] = async args => {
            if (await PalWorldServerMg.Channel.SendMessageAsync(string.Join(" ", args)))
                Log.WriteLine("Message sent", LogType.Info);
            else
                Log.WriteLine("Failed sending message", LogType.Error);
        };
    }
}