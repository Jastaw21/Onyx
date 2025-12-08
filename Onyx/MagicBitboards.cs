using MagicBitboardGenerator;

namespace Onyx;

public struct Magic
{
    public ulong Mask = 0ul;
    public ulong MagicNumber = 0ul;
    public int Shift = 0;
    public ulong[] Attacks = [];

    public Magic(ulong mask, ulong number, int shift, int numberAttacks)
    {
        Mask = mask;
        MagicNumber = number;
        Shift = shift;
        Attacks = new ulong[numberAttacks];
    }
}

public class MagicBitboards
{
    public MagicBitboards()
    {
        InitDiagMagics();
        InitStraightMagics();
    }

    private void InitDiagMagics()
    {
        for (var square = 0; square < 64; square++)
        {
            var mask = MaskGenerator.GenerateDiagonalMoves(square);
            var number = PrecomputedMagics.DiagMagics[square];
            var shift = (int)(64 - ulong.PopCount(mask));
            var numberOfAttacks = 1 << (int)ulong.PopCount(mask);

            var newMagic = new Magic(mask, number, shift, numberOfAttacks);


            for (var attack = 0; attack < numberOfAttacks; attack++)
            {
                var occupancy = MaskGenerator.GetThisOccupancy(attack, newMagic.Mask);
                var calculatedAttacks = MaskGenerator.GetDiagonalAttacks(square, occupancy);

                occupancy *= newMagic.MagicNumber;
                occupancy >>= newMagic.Shift;
                newMagic.Attacks[occupancy] = calculatedAttacks;
            }

            _diagMagics[square] = newMagic;
        }
    }

    private void InitStraightMagics()
    {
        for (var square = 0; square < 64; square++)
        {
            var mask = MaskGenerator.GenerateStraightMoves(square);
            var number = PrecomputedMagics.StraightMagics[square];
            var shift = (int)(64 - ulong.PopCount(mask));
            var numberOfAttacks = 1 << (int)ulong.PopCount(mask);

            var newMagic = new Magic(mask, number, shift, numberOfAttacks);


            for (var attack = 0; attack < numberOfAttacks; attack++)
            {
                var occupancy = MaskGenerator.GetThisOccupancy(attack, newMagic.Mask);
                var calculatedAttacks = MaskGenerator.GetStraightAttacks(square, occupancy);

                occupancy *= newMagic.MagicNumber;
                occupancy >>= newMagic.Shift;
                newMagic.Attacks[occupancy] = calculatedAttacks;
            }

            _straightMagics[square] = newMagic;
        }
    }

    private Magic[] _diagMagics = new Magic[64];
    private Magic[] _straightMagics = new Magic[64];

    public ulong GetMovesByPiece(Piece piece, Square square, ulong boardState)
    {
        switch (piece.Type)
        {
            case PieceType.Queen:
                return GetDiagAttacks(square, boardState) | GetStraightAttacks(square, boardState);
        }

        return 0;
    }

    private ulong GetDiagAttacks(Square square, ulong occupancy)
    {
        occupancy &= _diagMagics[square.SquareIndex].Mask;
        occupancy *= _diagMagics[square.SquareIndex].MagicNumber;
        occupancy >>= _diagMagics[square.SquareIndex].Shift;

        return _diagMagics[square.SquareIndex].Attacks[occupancy];
    }

    private ulong GetStraightAttacks(Square square, ulong occupancy)
    {
        occupancy &= _straightMagics[square.SquareIndex].Mask;
        occupancy *= _straightMagics[square.SquareIndex].MagicNumber;
        occupancy >>= _straightMagics[square.SquareIndex].Shift;

        return _straightMagics[square.SquareIndex].Attacks[occupancy];
    }
}