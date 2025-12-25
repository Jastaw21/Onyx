namespace Onyx.Statics;

public enum LogType
{
    UCIReceived,
    UCISent,
    EngineLog
}

public static class Logger
{
    public static void Log(LogType type, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";

        var logFile = type switch
        {
            LogType.UCIReceived => "uci_recieved.log",
            LogType.UCISent => "uci_sent.log",
            LogType.EngineLog => "engine.log",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        File.AppendAllText(logFile, logEntry + Environment.NewLine);
    }
}