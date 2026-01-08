using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class TranspositionTableTests
{
    [Test]
    public void EntriesStoredAndRetrieved()
    {
        var transpositionTable = new TranspositionTable();
        var startingHashZob = Zobrist.FromFen(Fen.DefaultFen);


        transpositionTable.Store(startingHashZob, 1, 1, 1, BoundFlag.Exact, default);
        var retrieved = transpositionTable.Retrieve(startingHashZob);
        Assert.Multiple(() =>
        {
            Assert.That(retrieved.HasValue);
            if (retrieved != null)
            {
                Assert.That(retrieved.Value.Age, Is.EqualTo(1));
                Assert.That(retrieved.Value.Depth, Is.EqualTo(1));
                Assert.That(retrieved.Value.Eval, Is.EqualTo(1));
                Assert.That(retrieved.Value.Hash, Is.EqualTo(startingHashZob));
            }
        });

        var otherZob = Zobrist.FromFen(Fen.KiwiPeteFen);
        var retrievedOther = transpositionTable.Retrieve(otherZob);
        Assert.That(retrievedOther.HasValue, Is.False);
    }
}