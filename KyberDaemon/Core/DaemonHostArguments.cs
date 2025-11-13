using System;

namespace KyberDaemon.Core;

public sealed class DaemonHostArguments
{
    public DaemonHostArguments(string[] args)
    {
        Args = args ?? Array.Empty<string>();
    }

    public string[] Args { get; }
}
