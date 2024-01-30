using System.Diagnostics;

namespace by.illusion21.Utilities.Common;

public class ProcessHandler {
    public static void RunProcess(string fileName, string arguments) {
        var startInfo = new ProcessStartInfo {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => {
            if (!string.IsNullOrEmpty(e.Data)) Log.WriteLine(e.Data, LogType.Info);
        };
        process.ErrorDataReceived += (_, e) => {
            if (!string.IsNullOrEmpty(e.Data)) Log.WriteLine(e.Data, LogType.Error);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        Log.WriteLine($"Process exited with code {process.ExitCode}", LogType.Warn);
    }
}