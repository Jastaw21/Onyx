using Onyx.Core;
using Onyx.UCI;

namespace OnyxTests;

public class UCITokenising
{
    [Test]
    public void basicTokenisation()
    {
        var tokens = new Tokeniser("uci 1 go perft depth");

        Assert.That(tokens.tokens, Has.Count.EqualTo(5));
    }
}

public class UCIParsing
{
    [Test]
    public void ParseGoCommands()
    {
        var testGoCommand = "go depth 2";
        var parser = new UCIParser(testGoCommand);
        var command = parser.Parse();
        Assert.That(command, Is.Not.Null);
        Assert.That(command is GoCommand);
        GoCommand goCommand = command as GoCommand;
        Assert.Multiple(() =>
        {
            Assert.That(goCommand.depth, Is.EqualTo(2));
            Assert.That(goCommand.isPerft, Is.False);
        });

        var testPerftCommand = "go perft 2";
        var perftParser = new UCIParser(testPerftCommand);
        var perftCommand = perftParser.Parse();
        Assert.That(perftCommand, Is.Not.Null);
        Assert.That(perftCommand is GoCommand);
        GoCommand? pComm = perftCommand as GoCommand;
        Assert.That(pComm.depth, Is.EqualTo(2));
        Assert.That(pComm.isPerft, Is.True);
    }

    [Test]
    public void NonsenseCommandCausesException()
    {
        var nonsenseString = "ak21";
        Assert.Throws<ArgumentException>(() =>
        {
            var parser = new UCIParser(nonsenseString);
            parser.Parse();
        });
    }

    [Test]
    public void ParsePositionNoMoves()
    {
        var basicStartpos = "position startpos";
        var command = new UCIParser(basicStartpos).Parse();
        Assert.That(command, Is.Not.EqualTo(null));
        Assert.That(command is PositionCommand);
        var posCommand = command as PositionCommand;
        
        Assert.That(posCommand.IsStartpos, Is.True);
        Assert.That(posCommand.FenString, Is.EqualTo(Fen.DefaultFen));
    }
}