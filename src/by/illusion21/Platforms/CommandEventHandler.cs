using by.illusion21.Services.Common.Types;
using by.illusion21.Services.EventBus;
using by.illusion21.Utilities.Common;

namespace by.illusion21.Platforms;

public class CommandEventHandler : ICommandEventHandler {
    private IDaemon _daemon = null!;

    public void SubscribeToEvents(EventBus<CommandEventType> eventBus) {
        Log.WriteLine("Subscribing to events", LogType.Warn);
        eventBus.Subscribe(CommandEventType.EventExit, _ => {
            _daemon.IsRunning = false;
            _daemon.TerminateProcess();
            Log.WriteLine("Exiting");
        });

        eventBus.Subscribe(CommandEventType.EventBackup, _ => {
            Log.WriteLine("Creating backup manually", LogType.Info);
#pragma warning disable CS4014
            _daemon.Backup(0, new CancellationToken(), true); // Should not block thread.
#pragma warning restore CS4014
        });

        eventBus.Subscribe(CommandEventType.EventRestart, _ => {
            Log.WriteLine("Executing manual restart");
            eventBus.Publish(CommandEventType.EventRestartCallback);
        });

        eventBus.Subscribe(CommandEventType.EventStat, _ => {
            var memInfo = _daemon.GetMemoryInfo();
            Log.Write($"总内存{memInfo.TotalMemory}MiB" +
                      $"\n已使用{memInfo.PercentUsedMemory}%" +
                      $"（{(memInfo.PercentUsedMemory * memInfo.TotalMemory * 0.01):0.00}MiB）" +
                      $"\n可用{(memInfo.TotalMemory - memInfo.PercentUsedMemory * memInfo.TotalMemory * 0.01):0.00}MiB" +
                      $"\n内存阈值{(memInfo.TotalMemory * PalWorldServerMg.Config!.ValueOf<double>("PalWorld", "MemThreshold")):0.00}MiB");
        });
    }

    public void SetDaemon(IDaemon daemon) {
        _daemon = daemon;
    }
}