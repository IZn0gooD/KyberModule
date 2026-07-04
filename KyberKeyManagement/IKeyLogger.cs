namespace KyberKeyManagement;

public interface IKeyLogger
{
    void Info(string source, string message);
    void Warning(string source, string message);
    void Error(string source, string message, Exception? ex = null);
}

public sealed class NullKeyLogger : IKeyLogger
{
    public void Info(string source, string message) { }
    public void Warning(string source, string message) { }
    public void Error(string source, string message, Exception? ex = null) { }
}
