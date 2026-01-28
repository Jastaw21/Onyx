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
    public int? MovesToGo;
}

public record GoCommand : Command
{
    public bool IsPerft;
    public int? Depth;
    public TimeControl? TimeControl;
    public bool IsPerftDivide;
}

public record SetOptionCommand : Command
{
    public string Name = null!;
    public string Value = null!;
}

public record UciNewGameCommand : Command;
public record IsReadyCommand : Command;
public record SetLoggingOn : Command;

public record PositionCommand(bool IsStartpos, string? Fen = null, List<string>? Moves = null)
    : Command
{
    public string? FenString => IsStartpos ? Core.Fen.DefaultFen : Fen;

    public string? Fen { get; set; } = Fen;
    public List<string>? Moves = Moves;
}