using KyberCLI.Commands;

namespace KyberCLI;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var command = CommandLineParser.Parse(args);
            return await command.ExecuteAsync();
        }
        catch (CommandLineException ex)
        {
            Console.Error.WriteLine($"Erreur: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erreur inattendue: {ex}");
            return -1;
        }
    }
}
