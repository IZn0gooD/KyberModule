using KyberDomain.Security;

namespace KyberLibrary.DomainAdapters;

internal sealed class KeyMaterialProtector : IKeyMaterialProtector
{
    public void Zeroize(byte[] material)
    {
        if (material is null)
        {
            return;
        }

        SecureKeyManager.Zeroize(material);
    }
}
