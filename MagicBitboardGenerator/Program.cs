// See https://aka.ms/new-console-template for more information

namespace MagicBitboardGenerator;

static class Program
{
    static void Main()
    {
        var generator = new global::MagicBitboardGenerator.MagicBitboardGenerator();
        
        generator.Generate();
    }
}