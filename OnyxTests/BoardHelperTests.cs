using Onyx.Core;
using Onyx.Statics;

namespace OnyxTests;

public class BoardHelperTests
{
    [Test]
    public void NeighbouringFiles()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BoardHelpers.NeighbouringFiles(6), Is.EqualTo(0xe0e0e0e0e0e0e0e0));
            Assert.That(BoardHelpers.NeighbouringFiles(54), Is.EqualTo(0xe0e0e0e0e0e0e0e0));
            Assert.That(BoardHelpers.NeighbouringFiles(7), Is.EqualTo(0xc0c0c0c0c0c0c0c0));
            Assert.That(BoardHelpers.NeighbouringFiles(47), Is.EqualTo(0xc0c0c0c0c0c0c0c0));
        });
    }

    [Test]
    public void OpenFiles()
    {
        var board = new Position();
        var pawns = board.Bitboards.OccupancyByPiece(Piece.WP) | board.Bitboards.OccupancyByPiece(Piece.BP);

        for (int file = 0; file < 8; file++)
        {
            Assert.That(!BoardHelpers.FileIsOpen(file, pawns));
        }
        
        // c file is open (index 2)
        board.SetFen("rnbqkbnr/pp1ppppp/8/8/8/8/PP1PPPPP/RNBQKBNR w KQkq - 0 1");
        pawns = board.Bitboards.OccupancyByPiece(Piece.WP) | board.Bitboards.OccupancyByPiece(Piece.BP);
        for (int file = 0; file < 8; file++)
        {
            Assert.That(BoardHelpers.FileIsOpen(file, pawns), Is.EqualTo(file == 2));
        }
    }
}