// See https://aka.ms/new-console-template for more information

using Onyx.UCI;

namespace Onyx;

public static class Program
{
    public static void Main()
    {
        var engine = new UciInterface();
       
        var command = string.Empty;
        while (command != "quit")
        {
            command = Console.ReadLine();
            if (command is null)
                break;
            if (command != "quit")
                engine.HandleCommand(command);
            else
                break;
        }
    }
}