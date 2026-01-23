using Onyx.Statics;
using Onyx.UCI;

namespace OnyxTests;

public class UCIInterfaceTests
{
    [Test]
    public void SetLogging()
    {
        var player = new UciInterface();
        Assert.That(Logger.LoggingEnabled, Is.False);
        player.HandleCommand("setoption name logging value true");
        Assert.That(Logger.LoggingEnabled, Is.True);
    }
    
    [Test]
    public void SetOptionLMR()
    {
        var player = new UciInterface();
        player.HandleCommand("setoption name lmrthreshold value 3");
        Assert.That(player.Player.PrimaryWorker.LmrThreshold, Is.EqualTo(3));
    }
}