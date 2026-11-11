namespace QTKD.Tests.IntegrationTests
{
    // Dùng chung một CustomWebApplicationFactory và chạy tuần tự giữa các class Integration Test
    // nhằm tránh việc nhiều class chạy song song cùng reset database QtkdTestDb cùng lúc.
    [CollectionDefinition("IntegrationTests")]
    public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
    {
    }
}
