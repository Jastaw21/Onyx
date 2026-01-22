// See https://aka.ms/new-console-template for more information

using Onyx.Statics;
using Onyx.UCI;

namespace Onyx;

public static class Program
{
    public static void Main()
    {
        try
        {
            var engine = new UciInterface();
            Logger.TimeString = DateTime.Now.ToString("yyyyMMddHHmmssfff");
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
        catch (Exception e)
        {
            Logger.Log(LogType.EngineLog,e.ToString());
            throw;
        }
        
        
    }
}