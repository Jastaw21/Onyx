using MagicBitboardGenerator;

namespace Onyx;

public struct Magic
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

public class MagicBitboards
{
    public MagicBitboards()
    {
        InitDiagMagics();
        InitStraightMagics();

        for (int square = 0; square < 64; square++)
        {
            var sq = new Square(square);
            BuildKnightMoves(sq);
            BuildKingMoves(sq);
        }
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

    private readonly Magic[] _diagMagics = new Magic[64];
    private readonly Magic[] _straightMagics = new Magic[64];
    private readonly ulong[] _knightAttacks = new ulong[64];
    private readonly ulong[] _kingAttacks = new ulong[64];

    public ulong GetMovesByPiece(Piece piece, Square square, ulong boardState)
    {
        switch (piece.Type)
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
                return _knightAttacks[square.SquareIndex];
            case PieceType.King:
                return _kingAttacks[square.SquareIndex];
            default:
                throw new ArgumentOutOfRangeException();
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

    private void BuildKnightMoves(Square square)
    {
        var movesFromHere = 0ul;
        foreach (var knightMove in BoardConstants.KnightMoves)
        {
            var newRank = square.RankIndex + knightMove[0];
            var newFile = square.FileIndex + knightMove[1];
            if (newRank < 0 || newRank > 7 || newFile < 0 || newFile > 7)
                continue;
            var newSquare = new Square(newRank, newFile);
            movesFromHere |= (1ul << newSquare.SquareIndex);
        }

        _knightAttacks[square.SquareIndex] = movesFromHere;
    }

    private void BuildKingMoves(Square square)
    {
        var movesFromHere = 0ul;
        foreach (var kingMove in BoardConstants.KingMoves)
        {
            var newRank = square.RankIndex + kingMove[0];
            var newFile = square.FileIndex + kingMove[1];
            if (newRank < 0 || newRank > 7 || newFile < 0 || newFile > 7)
                continue;
            var newSquare = new Square(newRank, newFile);
            movesFromHere |= (1ul << newSquare.SquareIndex);
        }

        _kingAttacks[square.SquareIndex] = movesFromHere;
    }

    private ulong GetPawnMoves(Colour colour, Square square, ulong boardState)
    {
        // should never actually have a pawn on these ranks
        if (square.RankIndex is 7 or 0)
            return 0ul;


        var result = 0ul;
        var squareIndex = square.SquareIndex;
        var squareOffset = (colour == Colour.White) ? 8 : -8;

        var isWhiteDoublePush = colour == Colour.White && square.RankIndex == 1;
        var isBlackDoublePush = colour == Colour.Black && square.RankIndex == 6;


        result |= 1ul << squareIndex + squareOffset;

        // can go right
        if (square.FileIndex < 7)
        {
            var rightIndex = (colour == Colour.White) ? 9 : -7;
            result |= 1ul << squareIndex + rightIndex;
        }

        if (square.FileIndex > 0)
        {
            var leftIndex = (colour == Colour.White) ? 7 : -9;
            result |= 1ul << squareIndex + leftIndex;
        }


        // if the pawns aren't on their starting ranks, return early
        if (!isBlackDoublePush && !isWhiteDoublePush) return result;
        
        
        switch (colour)
        {
            case Colour.White:
                // if there's anything on the immediate next rank, can't do a double push
                if (((1ul << squareIndex + 8) & boardState) > 0)
                    break;

                result |= 1ul << squareIndex + 16;
                break;
            case Colour.Black:
                // if there's anything on the immediate next rank, can't do a double push
                if (((1ul << squareIndex - 8) & boardState) > 0)
                    break;

                result |= 1ul << squareIndex - 16;
                break;
        }

        return result;
    }
}