using Onyx.MagicBitboards;

namespace MagicBitboardGenerator;

public class MagicBitboardGenerator
{
    private ulong GetMagicNumber(int square, bool isDiag)
    {
        var mask = isDiag ? MaskGenerator.GenerateDiagonalMoves(square) : MaskGenerator.GenerateStraightMoves(square);

        var maskBits = (int)ulong.PopCount(mask);
        var numberOfCombinations = 1 << maskBits;

        // reserve the right size
        var occupancies = new List<ulong>(numberOfCombinations);
        var attacks = new List<ulong>(numberOfCombinations);

        for (var i = 0; i < numberOfCombinations; i++)
        {
            occupancies.Add(MaskGenerator.GetThisOccupancy(i, mask));
            attacks.Add(isDiag
                ? MaskGenerator.GetDiagonalAttacks(square, occupancies[i])
                : MaskGenerator.GetStraightAttacks(square, occupancies[i]));
        }

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidateMagic = RandomUlong();
            var candidateWorks = true;

            var usedAttacks = new ulong[numberOfCombinations];

            for (var i = 0; i < numberOfCombinations; i++)
            {
                // this is then the candidate index
                var thisIndex = (int)((occupancies[i] * candidateMagic) >> (64 - maskBits));

                if (usedAttacks[thisIndex] == 0ul)
                {
                    usedAttacks[thisIndex] = attacks[i];
                }
                else if (usedAttacks[thisIndex] != attacks[i])
                {
                    candidateWorks = false;
                    break;
                }
            }

            if (candidateWorks)
                return candidateMagic;
        }


        Console.WriteLine($"Couldn't find magic for {square}");
        return 0ul;
    }

    private const int MaxAttempts = 100_000_000;

    private ulong RandomUlong()
    {
        var a = NextUlong();
        var b = NextUlong();
        var c = NextUlong();
        return a & b & c; // ANDing makes sparse numbers
    }

    private ulong NextUlong()
    {
        var buffer = new byte[8];
        _random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    private readonly Random _random = new Random(123123);


    public void Generate()
    {
        Console.WriteLine("public static readonly ulong [] DiagMagics = {\n");
        for (var square = 0; square < 64; square++)
        {
            Console.WriteLine($"{GetMagicNumber(square, true)},");
        }
        
        Console.WriteLine("};\n public static readonly ulong [] StraightMagics = {\n");
        for (var square = 0; square < 64; square++)
        {
            Console.WriteLine($"{GetMagicNumber(square, false)},");
        }
    }
}