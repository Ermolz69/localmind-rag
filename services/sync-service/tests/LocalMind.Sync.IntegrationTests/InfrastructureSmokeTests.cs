namespace LocalMind.Sync.IntegrationTests;

using Xunit;

public sealed class InfrastructureSmokeTests
{
    [Fact]
    public void TestcontainersModulesAreReferencedForMongoRedisAndRabbitMq()
    {
        Assert.Equal("MongoDbBuilder", typeof(Testcontainers.MongoDb.MongoDbBuilder).Name);
        Assert.Equal("RedisBuilder", typeof(Testcontainers.Redis.RedisBuilder).Name);
        Assert.Equal("RabbitMqBuilder", typeof(Testcontainers.RabbitMq.RabbitMqBuilder).Name);
    }
}
