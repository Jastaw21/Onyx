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