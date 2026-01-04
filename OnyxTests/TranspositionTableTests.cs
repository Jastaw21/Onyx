using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class TranspositionTableTests
{
    [Test]
    public void EntriesStoredAndRetrieved()
    {
        var transpositionTable = new TranspositionTable();
        var startingHashZob = new Zobrist(Fen.DefaultFen);

        var hash = startingHashZob.HashValue;
        
        transpositionTable.Store(hash, 1, 1, 1, BoundFlag.Exact, default, BoardStatus.Normal);
        var retrieved = transpositionTable.Retrieve(hash);
        Assert.Multiple(() =>
        {
            Assert.That(retrieved.HasValue);
            if (retrieved != null)
            {
                Assert.That(retrieved.Value.Age, Is.EqualTo(1));
                Assert.That(retrieved.Value.Depth, Is.EqualTo(1));
                Assert.That(retrieved.Value.Eval, Is.EqualTo(1));
                Assert.That(retrieved.Value.Hash, Is.EqualTo(hash));
            }
        });

        var otherZob = new Zobrist(Fen.KiwiPeteFen);
        var otherHash = otherZob.HashValue;

        var retrievedOther = transpositionTable.Retrieve(otherHash);
        Assert.That(retrievedOther.HasValue,Is.False);


    }
}