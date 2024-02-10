namespace by.illusion21.Platforms;

// ReSharper disable once InconsistentNaming
public interface IDaemon {
    bool IsRunning { get; set; }
    Task Run();
    Task Backup(int millisecondsDelay, CancellationToken stopToken, bool isOneshot);
    void RunServer();
    Task CheckMemory(int millisecondsDelay, CancellationToken stopToken);
    void Restart();
    void TerminateProcess();
    void KillProcess(string processName);
    (double PercentUsedMemory, double TotalMemory) GetMemoryInfo();
}