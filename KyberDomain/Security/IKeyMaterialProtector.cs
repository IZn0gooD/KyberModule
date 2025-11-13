namespace KyberDomain.Security;

public interface IKeyMaterialProtector
{
    void Zeroize(byte[] material);
}
