namespace KyberCLI.Commands;

public interface IKyberCommand
{
    Task<int> ExecuteAsync();
}
