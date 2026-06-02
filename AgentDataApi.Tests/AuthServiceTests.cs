using AgentDataApi.Services;
using Microsoft.Extensions.Configuration;

namespace AgentDataApi.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidarEmergenciaAsync_TrimsAndComparesSupervisorUserCaseInsensitively()
    {
        var service = CreateService();

        var result = await service.ValidarEmergenciaAsync("  supervisor  ", "clave-secreta");

        Assert.True(result);
    }

    [Fact]
    public async Task ValidarEmergenciaAsync_RequiresExactSupervisorPasswordAfterTrim()
    {
        var service = CreateService();

        var result = await service.ValidarEmergenciaAsync("SUPERVISOR", "CLAVE-SECRETA");

        Assert.False(result);
    }

    private static AuthService CreateService()
    {
        var values = new Dictionary<string, string?>
        {
            ["Auth:SupervisorUser"] = "SUPERVISOR",
            ["Auth:SupervisorPass"] = "clave-secreta"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new AuthService(config);
    }
}
