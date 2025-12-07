using MagicBitboardGenerator;

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
    public void ExpectedOccupancyCount()
    {
        var mask = MaskGenerator.GenerateDiagonalMoves(43);
        Assert.That(mask,Is.EqualTo(0x14001422400000));
        var numberBits = 7;
        var nuberOccupancyCombinations = 1 << numberBits;
    }


}