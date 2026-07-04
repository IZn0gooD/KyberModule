namespace KyberDomain.Security;

public interface ISideChannelDefense
{
    void BeforeOperation(SideChannelOperation operation, params byte[][] sensitiveBuffers);
    void AfterOperation(SideChannelOperation operation, params byte[][] sensitiveBuffers);
}
