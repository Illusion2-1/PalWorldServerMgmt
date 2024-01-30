namespace by.illusion21.Platforms;

// ReSharper disable once InconsistentNaming
public interface IDaemon {
    bool IsRunning { get; set; }
    Task Run();
    Task Backup();
    void RunServer();
    Task CheckMemory();
    void Restart();
    void TerminateProcess();
    void KillProcess(string processName);
    (double PercentUsedMemory, double TotalMemory) GetMemoryInfo();
}