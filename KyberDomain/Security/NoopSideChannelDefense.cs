namespace KyberDomain.Security;

public sealed class NoopSideChannelDefense : ISideChannelDefense
{
    public void BeforeOperation(SideChannelOperation operation, params byte[][] sensitiveBuffers)
    {
    }

    public void AfterOperation(SideChannelOperation operation, params byte[][] sensitiveBuffers)
    {
    }
}
