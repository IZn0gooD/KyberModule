using System.Text;
using KyberLibrary;
using Xunit;

namespace KyberIntegrationTests;

public class DilithiumOptimizationsTests
{
    [Fact]
    public void SignBatch_parallel_produces_valid_signatures()
    {
        var wrapper = new DilithiumWrapper();
        var (publicKey, privateKey) = wrapper.GenerateKeyPair();
        var messages = new[]
        {
            Encoding.UTF8.GetBytes("batch-message-1"),
            Encoding.UTF8.GetBytes("batch-message-2"),
            Encoding.UTF8.GetBytes("batch-message-3")
        };

        var signatures = wrapper.SignBatch(messages, privateKey, preHash: true, parallel: true);
        Assert.Equal(messages.Length, signatures.Length);

        for (int i = 0; i < messages.Length; i++)
        {
            Assert.True(wrapper.Verify(messages[i], signatures[i], publicKey, preHash: true));
        }
    }
}
