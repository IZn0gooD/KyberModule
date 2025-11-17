using System;
using KyberDomain.Security;
using KyberLibrary;
using Org.BouncyCastle.Security;

namespace KyberLibrary.DomainAdapters;

public sealed class AdvancedSideChannelDefense : ISideChannelDefense
{
    private readonly SecureRandom _random = new();

    public void BeforeOperation(SideChannelOperation operation, params byte[][] sensitiveBuffers)
    {
        SideChannelProtection.FlushCache();
        TouchBuffers(sensitiveBuffers);
    }

    public void AfterOperation(SideChannelOperation operation, params byte[][] sensitiveBuffers)
    {
        TouchBuffers(sensitiveBuffers);

        // Ajouter un léger délai pseudo-aléatoire pour augmenter le bruit temporel
        var microDelay = _random.Next(5, 35);
        SideChannelProtection.AddRandomDelay(0, microDelay);

        SideChannelProtection.FlushCache();
    }

    private static void TouchBuffers(byte[][] buffers)
    {
        if (buffers is null)
        {
            return;
        }

        foreach (var buffer in buffers)
        {
            if (buffer is { Length: > 0 })
            {
                SideChannelProtection.SequentialMemoryAccess(buffer);
            }
        }
    }
}
