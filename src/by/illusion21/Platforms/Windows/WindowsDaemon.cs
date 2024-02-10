using System.Diagnostics;
using System.IO.Compression;
using System.Management;
using by.illusion21.Services.Common.Types;
using by.illusion21.Services.EventBus;
using by.illusion21.Utilities.Common;

namespace by.illusion21.Platforms.Windows;

// ReSharper disable InconsistentNaming

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class WindowsDaemon : IDaemon {
    private static bool _isRunning = true;
    private readonly string _backupFolder = PalWorldServerMg.Config!.ValueOf<string>("PalWorld", "TgtFolder");
    private readonly double _memoryThreshold = PalWorldServerMg.Config.ValueOf<double>("PalWorld", "MemThreshold");
    private readonly string _serverExePath = PalWorldServerMg.Config.ValueOf<string>("PalWorld", "PalWorldExecPath");
    private readonly int _serverPort = PalWorldServerMg.Config.ValueOf<int>("PalWorld", "CustomPort");
    private readonly string _sourceFolder = PalWorldServerMg.Config.ValueOf<string>("PalWorld", "SrcFolder");
    private readonly string _steamcmdPath = PalWorldServerMg.Config.ValueOf<string>("PalWorld", "SteamCmdExecPath");

    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private EventBus<CommandEventType> _eventBus;
    private ICommandEventHandler? _eventHandler;
    private readonly CancellationTokenSource ShutdownCts = new();
    private CancellationTokenSource RestartCts = new();
    public WindowsDaemon(EventBus<CommandEventType> eventBus, ICommandEventHandler eventHandler) {
        _eventBus = eventBus;
        _eventHandler = eventHandler;
        Initialize(eventHandler);
    }

    public bool IsRunning {
        get => _isRunning;
        set => _isRunning = value;
    }

    public async Task Run() {
        Directory.CreateDirectory(_backupFolder);

        await LaunchDaemon();
    }

    async Task IDaemon.Backup(int millisecondsDelay, CancellationToken stopToken, bool isOneshot) {
        Log.WriteLine("Initializing backup creating thread", LogType.Warn);
        do {
            try {
                await Task.Delay(millisecondsDelay, stopToken);
            } catch (TaskCanceledException) {
                Log.WriteLine("Exiting backup creating thread", LogType.Warn);
                break;
            }

            var timestamp = DateTime.Now.ToString("yyyy.MM.dd-HH:mm:ss");
            var backupFileName = $"{timestamp}-Save.zip";
            var backupFilePath = Path.Combine(_backupFolder, backupFileName);
            using (var zip = new ZipArchive(File.Create(backupFilePath), ZipArchiveMode.Create)) {
                var di = new DirectoryInfo(_sourceFolder);
                foreach (var file in di.EnumerateFiles("*", SearchOption.AllDirectories)) {
                    var relativePath = Path.GetRelativePath(_sourceFolder, file.FullName);
                    zip.CreateEntryFromFile(file.FullName, relativePath);
                }
            }

            var usedMemory = ((IDaemon)this).GetMemoryInfo().PercentUsedMemory;
            var totalMemory = ((IDaemon)this).GetMemoryInfo().TotalMemory;
            var messageStatus = await PalWorldServerMg.Channel.SendMessageAsync(
                $"A backup has been created as {timestamp}-Save.zip\nThe server is currently consuming {usedMemory}% of {totalMemory}MiB\n");
            if (messageStatus == MessageStatus.Successful)
                Log.WriteLine("A backup message has been sent to channel successfully", LogType.Info);
            else if (messageStatus == MessageStatus.Failed)
                Log.WriteLine("An attempt at sending backup message to channel seemed failed", LogType.Error);
            Log.WriteLine($"Backup stored to {backupFilePath}", LogType.Info);
        } while (_isRunning && !isOneshot);
    }

    void IDaemon.RunServer() {
        Log.WriteLine("Making an attempt to start up server.", LogType.Info);
        var processThread = Task.Run(() => {
            ProcessHandler.RunProcess(_serverExePath, $"port={_serverPort} -useperfthreads -NoAsyncLoadingThread -UseMultiTHreadForDS"); 
            Log.WriteLine("Exiting process thread", LogType.Warn);
        });
        processThread.ContinueWith(_ => {
            if (!_isRunning) ShutdownCts.Cancel();
            RestartCts.Cancel();
        });
    }

    async Task IDaemon.CheckMemory(int millisecondsDelay, CancellationToken stopToken) {
        Log.WriteLine("Initializing memory checking thread", LogType.Warn);
        while (_isRunning) {
            try {
                await Task.Delay(millisecondsDelay, stopToken);
            } catch (TaskCanceledException) {
                Log.WriteLine("Exiting memory checking thread", LogType.Warn);
                break;
            }
            var memoryInfo = ((IDaemon)this).GetMemoryInfo();
            if (!(memoryInfo.PercentUsedMemory * 0.01 >= _memoryThreshold)) continue;
            var messageStatus = await PalWorldServerMg.Channel.SendMessageAsync($"Memory usage {memoryInfo.PercentUsedMemory * memoryInfo.TotalMemory * 0.01}MiB "
                                                                                + "is hitting the threshold "
                                                                                + _memoryThreshold * memoryInfo.TotalMemory
                                                                                + "MiB\nThe server will restart in 30 seconds.");
            switch (messageStatus) {
                case MessageStatus.Successful:
                    Log.WriteLine("A restart warning has been sent to channel successfully", LogType.Info);
                    break;
                case MessageStatus.Failed:
                    Log.WriteLine("An attempt at sending restart warning to channel seemed failed", LogType.Error);
                    break;
                case MessageStatus.Undefined:
                    break;
                default:
                    Log.WriteLine("Threshold reached. Daemon will try to restart in 30s.", LogType.Warn);
                    break;
            }
            Task.Delay(30000, stopToken).Wait();
            ((IDaemon)this).Restart();
        }
    }

    void IDaemon.Restart() {
        RestartCts = new();
        ((IDaemon)this).TerminateProcess();
        try {
            Task.Delay(30000, RestartCts.Token).Wait();
        } catch (AggregateException) {
            // No need to handle
        }
        ((IDaemon)this).RunServer();
    }

    void IDaemon.TerminateProcess() {
        ((IDaemon)this).KillProcess("PalServer");
    }

    void IDaemon.KillProcess(string processName) {
        var processes = Process.GetProcessesByName(processName);
        foreach (var process in processes) process.Kill();
        Log.WriteLine("Attempted to kill target process", LogType.Warn);
    }

    (double PercentUsedMemory, double TotalMemory) IDaemon.GetMemoryInfo() {
        double percentUsedMemory = 0;
        double totalMemory = 0;

        try {
            using var searcher = new ManagementObjectSearcher("SELECT PercentMemoryLoaded, TotalVisibleMemorySize FROM Win32_OperatingSystem");

            using var results = searcher.Get();

            foreach (var result in results)
                if (double.TryParse(result["PercentMemoryLoaded"].ToString(), out var usedMemoryRatio)) {
                    percentUsedMemory = usedMemoryRatio;

                    if (ulong.TryParse(result["TotalVisibleMemorySize"].ToString(), out var totalMemoryInBytes))
                        totalMemory = totalMemoryInBytes / (1024.0 * 1024.0);
                }
        } catch (Exception ex) {
            Log.WriteLine($"Error getting memory info: {ex.Message}", LogType.Error);
        }

        return (percentUsedMemory, totalMemory);
    }

    private void Initialize(ICommandEventHandler eventHandler) {
        _eventHandler = eventHandler;
        if (eventHandler is CommandEventHandler handler) handler.SetDaemon(this);
        _eventHandler.SubscribeToEvents(_eventBus);
    }

    private async Task LaunchDaemon() {
        await Task.Run(() => {
            if (!PalWorldServerMg.Config!.ValueOf<bool>("PalWorld", "DoUpdate")) return;
            Log.WriteLine("Trying to fetch update from steam content server", LogType.Info);
            ProcessHandler.RunProcess(_steamcmdPath,
                $"+force_install_dir \"{Path.GetDirectoryName(_serverExePath)}\" +login anonymous +app_update 2394010 validate +quit");
        });
        ((IDaemon)this).RunServer();
        
#pragma warning disable CS4014
        var backupTask = Task.Run(() => ((IDaemon)this).Backup(1800000, ShutdownCts.Token, false));
        var checkMemTask = Task.Run(() => ((IDaemon)this).CheckMemory(60000, ShutdownCts.Token));
        Task.WaitAll(backupTask, checkMemTask);
        Log.WriteLine("Daemon threads are exited", LogType.Warn);
    }
}