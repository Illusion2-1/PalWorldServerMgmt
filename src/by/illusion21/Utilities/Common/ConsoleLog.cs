namespace by.illusion21.Utilities.Common;

public enum LogType {
    Info,
    Warn,
    Error
}

public static class Log {
    public static void WriteLine(string msg) {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]: {msg}");
    }

    public static void Write(string msg) {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]: {msg}");
    }

    public static void WriteLine(string msg, LogType type) {
        SetConsoleColor(type);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]: {msg}");
        Console.ResetColor();
    }

    public static void Write(string msg, LogType type) {
        SetConsoleColor(type);
        Console.Write($"[{DateTime.Now:HH:mm:ss}]: {msg}");
        Console.ResetColor();
    }

    private static void SetConsoleColor(LogType type) {
        switch (type) {
            case LogType.Info:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case LogType.Warn:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogType.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            default:
                Console.ForegroundColor = ConsoleColor.White; // 默认颜色
                break;
        }
    }
}