using Onyx.Core;

namespace Onyx.MagicBitboards;

internal struct Magic
{
    public readonly ulong Mask = 0ul;
    public readonly ulong MagicNumber = 0ul;
    public readonly int Shift = 0;
    public readonly ulong[] Attacks = [];

    public Magic(ulong mask, ulong number, int shift, int numberAttacks)
    {
        Mask = mask;
        MagicNumber = number;
        Shift = shift;
        Attacks = new ulong[numberAttacks];
    }
}

public static class MagicBitboards
{
    static MagicBitboards()
    {
        InitDiagMagics();
        InitStraightMagics();

        for (var square = 0; square < 64; square++)
        {
            BuildKnightMoves(square);
            BuildKingMoves(square);
        }
    }

    private static void InitDiagMagics()
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

    private static void InitStraightMagics()
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

    private static readonly Magic[] _diagMagics = new Magic[64];
    private static readonly Magic[] _straightMagics = new Magic[64];
    private static readonly ulong[] _knightAttacks = new ulong[64];
    private static readonly ulong[] _kingAttacks = new ulong[64];

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ulong GetMovesByPiece(Piece piece, int square, ulong boardState)
    {
        var type = piece.Type;       
        
        switch (type)
        {
            case PieceType.Queen:
                return GetDiagAttacks(square, boardState) | GetStraightAttacks(square, boardState);
            case PieceType.Rook:
                return GetStraightAttacks(square, boardState);
            case PieceType.Bishop:
                return GetDiagAttacks(square, boardState);
            case PieceType.Pawn:
                return GetPawnMoves(piece.Colour, square, boardState);
            case PieceType.Knight:
                return _knightAttacks[square];
            case PieceType.King:               
                return _kingAttacks[square];
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static ulong GetDiagAttacks(int square, ulong occupancy)
    {
        var magic = _diagMagics[square];
        occupancy &= magic.Mask;
        occupancy *= magic.MagicNumber;
        occupancy >>= magic.Shift;

        return magic.Attacks[(int)occupancy];
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static ulong GetStraightAttacks(int square, ulong occupancy)
    {
        var magic = _straightMagics[square];
        occupancy &= magic.Mask;
        occupancy *= magic.MagicNumber;
        occupancy >>= magic.Shift;

        return magic.Attacks[(int)occupancy];
    }

    private static void BuildKnightMoves(int square)
    {
        var movesFromHere = 0ul;
        foreach (var knightMove in BoardConstants.KnightMoves)
        {
            var newRank = RankAndFileHelpers.RankIndex(square) + knightMove[0];
            var newFile = RankAndFileHelpers.FileIndex(square) + knightMove[1];
            if (newRank < 0 || newRank > 7 || newFile < 0 || newFile > 7)
                continue;
            var newSquare = RankAndFileHelpers.SquareIndex(newRank, newFile);
            movesFromHere |= 1ul << newSquare;
        }

        _knightAttacks[square] = movesFromHere;
    }

    private static void BuildKingMoves(int square)
    {
        var movesFromHere = 0ul;
        foreach (var kingMove in BoardConstants.KingMoves)
        {
            var newRank = RankAndFileHelpers.RankIndex(square)  + kingMove[0];
            var newFile = RankAndFileHelpers.FileIndex(square) + kingMove[1];
            if (newRank < 0 || newRank > 7 || newFile < 0 || newFile > 7)
                continue;
            var newSquare = RankAndFileHelpers.SquareIndex(newRank, newFile);
            movesFromHere |= 1ul << newSquare;
        }

        _kingAttacks[square] = movesFromHere;
    }

    private static ulong GetPawnMoves(Colour colour, int square, ulong boardState)
    {
        var result = 0ul;

        result |= GetPawnPushes(colour, square, boardState);
       

        // can go right
        var FileIndex = RankAndFileHelpers.FileIndex(square);
        if (FileIndex < 7)
        {
            var rightIndex = colour == Colour.White ? 9 : -7;
            result |= 1ul << square + rightIndex;
        }

        if (FileIndex > 0)
        {
            var leftIndex = colour == Colour.White ? 7 : -9;
            result |= 1ul << square + leftIndex;
        }

        return result;
    }


    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ulong GetPawnPushes(Colour colour, int square, ulong boardState)
    {
        var rankIndex = RankAndFileHelpers.RankIndex(square);
        if (rankIndex is 7 or 0)
            return 0ul;

       
        var squareOffset = colour == Colour.White ? 8 : -8;

        var isWhiteDoublePush = colour == Colour.White && rankIndex == 1;
        var isBlackDoublePush = colour == Colour.Black && rankIndex == 6;


        // add the single push
        var result = 1ul << square + squareOffset;
        
        // cant go anywhere occupied
        if ((result & boardState) > 0)
            return 0ul;

        // if the pawns aren't on their starting ranks, return early
        if (!isBlackDoublePush && !isWhiteDoublePush) return result;


        switch (colour)
        {
            case Colour.White:
                // if there's anything on the immediate next rank, can't do a double push
                if (((1ul << square + 8) & boardState) > 0)
                    break;
                // or on the square we're going to
                if (((1ul << square + 16) & boardState) > 0)
                    break;

                result |= 1ul << square + 16;
                break;
            case Colour.Black:
                // if there's anything on the immediate next rank, can't do a double push
                if (((1ul << square - 8) & boardState) > 0)
                    break;
                
                // or on the square we're going to
                if (((1ul << square - 16) & boardState) > 0)
                    break;

                result |= 1ul << square - 16;
                break;
        }

        return result;
    }
}