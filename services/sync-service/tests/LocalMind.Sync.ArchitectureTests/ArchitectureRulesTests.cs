namespace LocalMind.Sync.ArchitectureTests;

using NetArchTest.Rules;
using Xunit;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void DomainDoesNotDependOnFrameworksOrInfrastructure()
    {
        TestResult result = Types
            .InAssembly(typeof(Domain.Devices.Device).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.AspNetCore", "MongoDB", "StackExchange.Redis", "RabbitMQ.Client", "MassTransit", "LocalMind.Sync.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ApplicationDoesNotDependOnInfrastructureOrApi()
    {
        TestResult result = Types
            .InAssembly(typeof(Application.Devices.DeviceService).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("LocalMind.Sync.Infrastructure", "LocalMind.Sync.Api", "MongoDB", "StackExchange.Redis", "RabbitMQ.Client", "MassTransit")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ApiDoesNotDependOnConcreteInfrastructureClients()
    {
        TestResult result = Types
            .InAssembly(typeof(Api.Web.EndpointResults).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("MongoDB", "StackExchange.Redis", "RabbitMQ.Client", "MassTransit")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
