using KyberCLI.Commands;

namespace KyberCLI.Commands;

public static class CommandLineParser
{
    public static IKyberCommand Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            return new HelpCommand();
        }

        return args[0] switch
        {
            "connect" => ConnectCommand.Parse(args[1..]),
            "keys" => KeysCommand.Parse(args[1..]),
            "passhash" => PasswordHashCommand.Parse(args[1..]),
            _ => throw new CommandLineException($"Commande inconnue: {args[0]}")
        };
    }
}

public sealed class CommandLineException : Exception
{
    public CommandLineException(string message) : base(message) { }
}
