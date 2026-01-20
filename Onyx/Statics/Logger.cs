namespace Onyx.Statics;

public interface ILoggable
{
    public string Get();
}

public enum LogType
{
    UCIReceived,
    UCISent,
    EngineLog,
    Search,
    Evaluator
}

public static class Logger
{
    public static bool LoggingEnabled = false;
    public static string TimeString;

    public static void Log(LogType type, string message)
    {
        var logFile = type switch
        {
            LogType.UCIReceived => "received",
            LogType.UCISent => "sent",
            LogType.EngineLog => "engine",
            LogType.Search => "search",
            LogType.Evaluator => "evaluator",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{logFile}] {message}";

        var finalLog = "uci_log" + TimeString + ".log";
        File.AppendAllText(finalLog, logEntry + Environment.NewLine);
    }

    public static void Log(LogType type, ILoggable loggable)
    {
        var logFile = type switch
        {
            LogType.UCIReceived => "received",
            LogType.UCISent => "sent",
            LogType.EngineLog => "engine",
            LogType.Search => "search",
            LogType.Evaluator => "evaluator",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{logFile}] {loggable.Get()}";
        Console.Error.WriteLine(logEntry);
        var finalLog = "uci_log" + TimeString + ".log";
        File.AppendAllText(finalLog, logEntry + Environment.NewLine);
    }
}