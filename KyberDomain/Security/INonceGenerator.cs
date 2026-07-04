namespace KyberDomain.Security;

public interface INonceGenerator
{
    byte[] Generate(int size);
}
