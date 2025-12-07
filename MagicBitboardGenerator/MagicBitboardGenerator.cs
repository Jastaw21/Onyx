namespace MagicBitboardGenerator;

public class MagicBitboardGenerator
{
    public ulong getMagicNumber(int square, bool isDiag)
    {
        var mask = isDiag ? MaskGenerator.GenerateDiagonalMoves(square) : MaskGenerator.GenerateStraightMoves(square);

        var maskBits = (int)ulong.PopCount(mask);
        var numberOfCombinations = 1 << maskBits;

        // reserve the right size
        var occupancies = new List<ulong>(numberOfCombinations);
        var attacks = new List<ulong>(numberOfCombinations);

        for (var i = 0; i < numberOfCombinations; i++)
        {
            occupancies[i] = MaskGenerator.GetThisOccupancy(i, mask);
            attacks[i] = isDiag
                ? MaskGenerator.GetDiagonalAttacks(square, occupancies[i])
                : MaskGenerator.GenerateStraightAttacks(square, occupancies[i]);
        }


        return 0ul;
    }
}