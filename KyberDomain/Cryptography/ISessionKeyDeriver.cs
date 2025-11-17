namespace KyberDomain.Cryptography;

public interface ISessionKeyDeriver
{
    byte[] Derive(byte[] sharedSecret);
}
