namespace KyberDomain.Cryptography;

public interface ISecureSessionFactory
{
    SecureSession Create(SecureSessionOptions options);
}
