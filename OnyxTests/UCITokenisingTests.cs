using Onyx.Core;
using Onyx.UCI;

namespace OnyxTests;

public class UciTokenisingTests
{
    [Test]
    public void BasicTokenisation()
    {
        var tokens = new Tokeniser("uci 1 go perft depth");

        Assert.That(tokens.Tokens, Has.Count.EqualTo(5));
    }
}

public class UciParsingTests
{
    [Test]
    public void ParseGoCommands()
    {
        const string testGoCommand = "go depth 2";
        var parser = new UciParser();
        var command = parser.Parse(testGoCommand);
        Assert.That(command, Is.Not.Null);
        Assert.That(command is GoCommand);
        var goCommand = command as GoCommand;
        Assert.Multiple(() =>
        {
            Assert.That(goCommand.depth, Is.EqualTo(2));
            Assert.That(goCommand.isPerft, Is.False);
        });

        const string testPerftCommand = "go perft 2";
        var perftCommand = parser.Parse(testPerftCommand);
        Assert.That(perftCommand, Is.Not.Null);
        Assert.That(perftCommand is GoCommand);
        var pComm = perftCommand as GoCommand;
        Assert.Multiple(() =>
        {
            Assert.That(pComm.depth, Is.EqualTo(2));
            Assert.That(pComm.isPerft, Is.True);
        });
    }

    [Test]
    public void NonsenseCommandCausesException()
    {
        const string nonsenseString = "ak21";
        var parser = new UciParser();
        Assert.Throws<ArgumentException>(() => { parser.Parse(nonsenseString); });
    }

    [Test]
    public void ParsePositionNoMoves()
    {
        const string basicStartpos = "position startpos";
        var parser = new UciParser();
        var command = parser.Parse(basicStartpos);
        Assert.That(command, Is.Not.EqualTo(null));
        Assert.That(command is PositionCommand);
        var posCommand = command as PositionCommand;
        Assert.Multiple(() =>
        {
            Assert.That(posCommand.IsStartpos, Is.True);
            Assert.That(posCommand.FenString, Is.EqualTo(Fen.DefaultFen));
        });

        const string specifiedFenCommand =
            "position fen r1bqkbnr/pp1ppppp/2n5/2p5/5Q2/3PP3/PPP2PPP/RNB1KBNR w KQkq - 0 1";

        var comm = parser.Parse(specifiedFenCommand);

        Assert.That(comm, Is.Not.EqualTo(null));
        Assert.That(comm is PositionCommand);
        var posCom = comm as PositionCommand;
        Assert.Multiple(() =>
        {
            Assert.That(posCom != null && posCom.IsStartpos, Is.False);
            Assert.That(posCom.FenString,
                Is.EqualTo("r1bqkbnr/pp1ppppp/2n5/2p5/5Q2/3PP3/PPP2PPP/RNB1KBNR w KQkq - 0 1"));
        });
    }
}