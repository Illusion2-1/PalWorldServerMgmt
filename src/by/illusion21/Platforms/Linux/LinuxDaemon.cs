using System.Diagnostics;
using System.IO.Compression;
using by.illusion21.Services.Common.Types;
using by.illusion21.Services.EventBus;
using by.illusion21.Utilities.Common;
using Mono.Unix.Native;

namespace by.illusion21.Platforms.Linux;

// ReSharper disable InconsistentNaming
public class LinuxDaemon : IDaemon {
    private static bool _isRunning = true;
    private readonly string _backupFolder = PalWorldServerMg.Config!.ValueOf<string>("PalWorld", "TgtFolder");
    private readonly double _memoryThreshold = PalWorldServerMg.Config.ValueOf<double>("PalWorld", "MemThreshold");
    private readonly string _serverExePath = PalWorldServerMg.Config.ValueOf<string>("PalWorld", "PalWorldExecPath");
    private readonly int _serverPort = PalWorldServerMg.Config.ValueOf<int>("PalWorld", "CustomPort");
    private readonly string _sourceFolder = PalWorldServerMg.Config.ValueOf<string>("PalWorld", "SrcFolder");
    private readonly string _steamcmdPath = PalWorldServerMg.Config.ValueOf<string>("PalWorld", "SteamCmdExecPath");

    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private EventBus<CommandEventType> _eventBus;
    private ICommandEventHandler _eventHandler;
    private readonly CancellationTokenSource shutdownCts = new();
    private CancellationTokenSource restartCts = new();

    public LinuxDaemon(EventBus<CommandEventType> eventBus, ICommandEventHandler eventHandler) {
        _eventBus = eventBus;
        _eventHandler = eventHandler;
        Initialize(eventHandler);
    }

    public bool IsRunning {
        get => _isRunning;
        set => _isRunning = value;
    }

    public async Task Run() {
        Directory.CreateDirectory(_backupFolder); // Ensure the backup folder exists
        _eventBus.Subscribe(CommandEventType.EventRestartCallback, _ => { ((IDaemon)this).Restart(); });
        await StartServerLoop();
    }

    async Task IDaemon.Backup() {
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
    }

    public void RunServer() {
        Log.WriteLine("Making an attempt to start up server.", LogType.Info);
        Task.Run(() => {
            ProcessHandler.RunProcess(_serverExePath, $"port={_serverPort} -useperfthreads -NoAsyncLoadingThread -UseMultiTHreadForDS");
            Log.WriteLine("Exiting process thread", LogType.Warn);
            if (!_isRunning) shutdownCts.Cancel();
            restartCts.Cancel();
        });
    }

    async Task IDaemon.CheckMemory() {
        var memoryInfo = ((IDaemon)this).GetMemoryInfo();
        if (memoryInfo.PercentUsedMemory * 0.01 >= _memoryThreshold) {
            var messageStatus = await PalWorldServerMg.Channel.SendMessageAsync($"Memory usage {memoryInfo.PercentUsedMemory * memoryInfo.TotalMemory * 0.01}MiB "
                                                                                + "is hitting the threshold "
                                                                                + _memoryThreshold * memoryInfo.TotalMemory
                                                                                + "MiB\nThe server will restart in 30 seconds.");
            if (messageStatus == MessageStatus.Successful)
                Log.WriteLine("A restart warning has been sent to channel successfully", LogType.Info);
            else if (messageStatus == MessageStatus.Failed)
                Log.WriteLine("An attempt at sending restart warning to channel seemed failed", LogType.Error);
            else
                Log.WriteLine("Threshold reached. Daemon will try to restart in 30s.", LogType.Warn);
            Task.Delay(30000).Wait();
            ((IDaemon)this).Restart();
        }
    }

    void IDaemon.Restart() {
        restartCts = new CancellationTokenSource(); // Reset cancel status
        ((IDaemon)this).TerminateProcess();
        Task.Delay(30000, restartCts.Token).Wait();
        RunServer();
    }

    void IDaemon.KillProcess(string processName) {
        var processes = Process.GetProcessesByName(processName);
        foreach (var process in processes) Syscall.kill(process.Id, Signum.SIGTERM);
        Log.WriteLine("SIGTERM sent to PalServer process!", LogType.Warn);
    }

    (double PercentUsedMemory, double TotalMemory) IDaemon.GetMemoryInfo() {
        double percentUsedMemory = 0;
        double totalMemory = 0;

        try {
            var output = RunBashCommand("free -m | grep Mem | awk '{print $3/$2 * 100.0}'");

            if (double.TryParse(output, out var usedMemoryRatio)) {
                percentUsedMemory = usedMemoryRatio;

                output = RunBashCommand("free -m | grep Mem | awk '{print $2}'");

                if (double.TryParse(output, out var totalMemoryInMB)) totalMemory = totalMemoryInMB;
            }
        } catch (Exception ex) {
            Log.WriteLine($"Error getting memory info: {ex.Message}", LogType.Error);
        }

        return (percentUsedMemory, totalMemory);
    }


    public void TerminateProcess() {
        ((IDaemon)this).KillProcess("PalServer-Linux-Test");
    }

    private void Initialize(ICommandEventHandler eventHandler) {
        _eventHandler = eventHandler;
        if (eventHandler is CommandEventHandler handler) handler.SetDaemon(this);
        _eventHandler.SubscribeToEvents(_eventBus);
    }

    private async Task StartServerLoop() {
        await Task.Run(() => {
            if (!PalWorldServerMg.Config!.ValueOf<bool>("PalWorld", "DoUpdate")) return;
            Log.WriteLine("Trying to fetch update from steam content server", LogType.Info);
            ProcessHandler.RunProcess(_steamcmdPath,
                $"+force_install_dir \"{Path.GetDirectoryName(_serverExePath)}\" +login anonymous +app_update 2394010 validate +quit");
        });
        RunServer();

        while (_isRunning)
            try {
                await ((IDaemon)this).Backup();
                await ((IDaemon)this).CheckMemory();
                await Task.Delay(600000, shutdownCts.Token);
            } catch (Exception e) {
                Log.WriteLine($"Cancelled loop\n Is scheduled cancellation: {!_isRunning}\n" +
                              $"is shutdownCts cancelled: {shutdownCts.IsCancellationRequested}\n" +
                              $"is restartCts cancelled: {restartCts.IsCancellationRequested}\n" +
                              $"Detailed exception: {e.Message}", LogType.Info);
                break;
            }
    }

    private static string? RunBashCommand(string command) {
        string? output = null;

        try {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "/bin/bash",
                    Arguments = "-c \"" + command + "\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
        } catch (Exception ex) {
            Log.WriteLine($"Error running command: {ex.Message}", LogType.Error);
        }

        return output;
    }
}