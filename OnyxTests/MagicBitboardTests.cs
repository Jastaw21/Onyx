using Onyx.Core;
using Onyx.MagicBitboards;

namespace OnyxTests;

public class MagicBitboardTests
{
    [Test]
    public void MaskGenTests()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MaskGenerator.GenerateStraightMoves(0), Is.EqualTo(0x101010101017e));
            Assert.That(MaskGenerator.GenerateStraightMoves(8), Is.EqualTo(0x1010101017e00));
            Assert.That(MaskGenerator.GenerateStraightMoves(16), Is.EqualTo(0x10101017e0100));
            Assert.That(MaskGenerator.GenerateStraightMoves(55), Is.EqualTo(0x7e808080808000));
        });


        Assert.Multiple(() =>
        {
            Assert.That(MaskGenerator.GenerateDiagonalMoves(0), Is.EqualTo(0x40201008040200));
            Assert.That(MaskGenerator.GenerateDiagonalMoves(8), Is.EqualTo(0x20100804020000));
            Assert.That(MaskGenerator.GenerateDiagonalMoves(16), Is.EqualTo(0x10080402000200));
            Assert.That(MaskGenerator.GenerateDiagonalMoves(55), Is.EqualTo(0x402010080400));
        });
    }

    [Test]
    public void GetThisOccupancy_Index0_ReturnsEmpty()
    {
        const ulong mask = 0b10101010ul;
        Assert.That(MaskGenerator.GetThisOccupancy(0, mask), Is.EqualTo(0ul));
    }

    [Test]
    public void DiagonalAttacksStopForOccupancy()
    {
        var mask = MaskGenerator.GenerateDiagonalMoves(43);
        Assert.That(mask, Is.EqualTo(0x14001422400000));

        var occupancy = 0x4000000000000ul; // block on c7
        var attacks = MaskGenerator.GetDiagonalAttacks(43, occupancy);
        Assert.That(attacks, Is.EqualTo(0x2014001422418000));

        occupancy = 0x1000000000; // blocked on e5
        attacks = MaskGenerator.GetDiagonalAttacks(43, occupancy);
        Assert.That(attacks, Is.EqualTo(0x2214001402010000));

        occupancy = 0ul; // empty board
        attacks = MaskGenerator.GetDiagonalAttacks(21, occupancy);
        Assert.That(attacks, Is.EqualTo(0x0102048850005088));

        occupancy = 0x70507000; // ringed in on f3
        Assert.That(MaskGenerator.GetDiagonalAttacks(21, occupancy), Is.EqualTo(0x50005000));
    }

    [Test]
    public void StraightAttacksStopForOccupancy()
    {
        Assert.Multiple(() =>
        {
            // empty board tests
            Assert.That(MaskGenerator.GetStraightAttacks(0, 0), Is.EqualTo(0x1010101010101fe));
            Assert.That(MaskGenerator.GetStraightAttacks(32, 0), Is.EqualTo(0x10101fe01010101));

            // occupied tests
            Assert.That(MaskGenerator.GetStraightAttacks(43, 0x2820000000808), Is.EqualTo(0x808f60808080800));
            Assert.That(MaskGenerator.GetStraightAttacks(53, 0x42000000200000), Is.EqualTo(0x205e202020200000));
        });
    }

    [Test]
    public void GetQueenMoves()
    {
        Assert.That(MagicBitboards.GetMovesByPiece(Piece.WQ, 0, 0ul),
            Is.EqualTo(0x81412111090503fe));
    }

    [Test]
    public void GetRookMoves()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MagicBitboards.GetMovesByPiece(Piece.BR, 0, 0ul),
                Is.EqualTo(0x1010101010101fe));

            Assert.That(MagicBitboards.GetMovesByPiece(Piece.BR, 0, 0x10090),
                Is.EqualTo(0x1011e));

            Assert.That(MagicBitboards.GetMovesByPiece(Piece.BR, 20, 0x10090),
                Is.EqualTo(0x1010101010ef1010));
        });
    }

    [Test]
    public void GetKnightMoves()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MagicBitboards.GetMovesByPiece(Piece.BN,0, 0ul),
                    Is.EqualTo(0x20400));

            Assert.That(MagicBitboards.GetMovesByPiece(Piece.BN, 35, 0ul),
                Is.EqualTo(0x14220022140000));

            Assert.That(MagicBitboards.GetMovesByPiece(Piece.BN, 46, 0ul),
                Is.EqualTo(0xa0100010a0000000));
        });
    }

    [Test]
    public void GetKingMoves()
    {
        Assert.That(MagicBitboards.GetMovesByPiece(Piece.WK,0,0ul),
            Is.EqualTo(0x302));
    }

    [Test]
    public void GetPawnMoves()
    {
        Assert.Multiple(() =>
        {
            // starting rank moves
            Assert.That(MagicBitboards.GetMovesByPiece(Piece.WP, 8, 0),
                    Is.EqualTo(0x1030000));

            Assert.That(MagicBitboards.GetMovesByPiece(Piece.BP, 48, 0),
                Is.EqualTo(0x30100000000));
            
            Assert.That(MagicBitboards.GetMovesByPiece(Piece.WP, 19, 0),
                Is.EqualTo(0x1c000000));
        });
    }


}