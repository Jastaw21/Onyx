// See https://aka.ms/new-console-template for more information

using Onyx.Core;

namespace Onyx;

public static class Program
{
    public static void Main()
    {
        var engine = new Engine();

        var command = string.Empty;
        while (command != "quit")
        {
            command = Console.ReadLine();
            engine.HandleCommand(command);
        }
    }
}