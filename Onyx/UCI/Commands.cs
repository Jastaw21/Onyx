using Onyx.Core;

namespace Onyx.UCI;

public abstract record Command;
public record UciCommand : Command;
public record DebugCommand : Command;
public record StopCommand : Command;
public record EvaluateCommand : Command;
public struct TimeControl
{
    public int? Wtime;
    public int? Btime;
    public int? Binc;
    public int? Winc;
    public int? movesToGo;
}

public record GoCommand : Command
{
    public bool IsPerft;
    public int? Depth;
    public TimeControl? TimeControl;
    public bool IsPerftDivide;
}

public record UciNewGameCommand : Command;
public record IsReadyCommand : Command;
public record SetLoggingOn : Command;
public record PositionCommand : Command
{
    public bool IsStartpos { get; }

    public string? FenString => IsStartpos ? Fen.DefaultFen : Fenstring;

    public string? Fenstring { get; set; }
    public List<string>? Moves;

    public PositionCommand(bool isStartpos, string? fen = null, List<string>? moves = null)
    {
        IsStartpos = isStartpos;
        Fenstring = fen;
        Moves = moves;
    }
}