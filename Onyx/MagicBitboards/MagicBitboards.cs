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
        BuildPawnAttacks();

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
    private static readonly ulong[,] _pawnAttacks = new ulong[2, 64];

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ulong GetMovesByPiece(sbyte piece, int square, ulong boardState)
    {
        var type = Piece.PieceType(piece);

        switch (type)
        {
            case Piece.Queen:
                return GetDiagAttacks(square, boardState) | GetStraightAttacks(square, boardState);
            case Piece.Rook:
                return GetStraightAttacks(square, boardState);
            case Piece.Bishop:
                return GetDiagAttacks(square, boardState);
            case Piece.Pawn:
                return GetPawnMoves(Piece.IsWhite(piece), square, boardState);
            case Piece.Knight:
                return _knightAttacks[square];
            case Piece.King:
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
            var newRank = RankAndFile.RankIndex(square) + knightMove[0];
            var newFile = RankAndFile.FileIndex(square) + knightMove[1];
            if (newRank < 0 || newRank > 7 || newFile < 0 || newFile > 7)
                continue;
            var newSquare = RankAndFile.SquareIndex(newRank, newFile);
            movesFromHere |= 1ul << newSquare;
        }

        _knightAttacks[square] = movesFromHere;
    }

    private static void BuildPawnAttacks()
    {
        for (ulong square = 0; square < 64; square++)
        {;            
            _pawnAttacks[0,(int)square] = GetPawnAttacks(true, (int)square);
            _pawnAttacks[1,(int)square] = GetPawnAttacks(false, (int)square);
        }
    }

    private static void BuildKingMoves(int square)
    {
        var movesFromHere = 0ul;
        foreach (var kingMove in BoardConstants.KingMoves)
        {
            var newRank = RankAndFile.RankIndex(square) + kingMove[0];
            var newFile = RankAndFile.FileIndex(square) + kingMove[1];
            if (newRank < 0 || newRank > 7 || newFile < 0 || newFile > 7)
                continue;
            var newSquare = RankAndFile.SquareIndex(newRank, newFile);
            movesFromHere |= 1ul << newSquare;
        }

        _kingAttacks[square] = movesFromHere;
    }

    public static ulong GetPawnAttacks(bool isWhite, int square)
    {
        var result = 0ul;
        var FileIndex = RankAndFile.FileIndex(square);
        if (FileIndex < 7)
        {
            var rightIndex = isWhite ? 9 : -7;
            result |= 1ul << square + rightIndex;
        }

        if (FileIndex > 0)
        {
            var leftIndex = isWhite ? 7 : -9;
            result |= 1ul << square + leftIndex;
        }

        return result;
    }

    private static ulong GetPawnMoves(bool isWhite, int square, ulong boardState)
    {
        var index = isWhite ? 0 : 1;
        return GetPawnPushes(isWhite, square, boardState) | _pawnAttacks[index, square];
    }


    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static ulong GetPawnPushes(bool isWhite, int square, ulong boardState)
    {
        var rankIndex = RankAndFile.RankIndex(square);
        if (rankIndex is 7 or 0)
            return 0ul;


        var squareOffset = isWhite ? 8 : -8;

        var isWhiteDoublePush = isWhite && rankIndex == 1;
        var isBlackDoublePush = !isWhite && rankIndex == 6;


        // add the single push
        var result = 1ul << square + squareOffset;

        // cant go anywhere occupied
        if ((result & boardState) > 0)
            return 0ul;

        // if the pawns aren't on their starting ranks, return early
        if (!isBlackDoublePush && !isWhiteDoublePush) return result;


        switch (isWhite)
        {
            case true:
                // if there's anything on the immediate next rank, can't do a double push
                if (((1ul << square + 8) & boardState) > 0)
                    break;
                // or on the square we're going to
                if (((1ul << square + 16) & boardState) > 0)
                    break;

                result |= 1ul << square + 16;
                break;
            case false:
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