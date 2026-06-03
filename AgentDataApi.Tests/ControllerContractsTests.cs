using AgentDataApi.Controllers;
using AgentDataApi.DTOs;
using AgentDataApi.Services.Implementation;
using AgentDataApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AgentDataApi.Tests;

public class ControllerContractsTests
{
    [Fact]
    public async Task Login_ReturnsBadRequest_WhenUsuarioIsMissing()
    {
        var controller = new AuthController(CreateAuthService());

        var result = await controller.Login(new LoginDto { Usuario = "", Password = "secret" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("Usuario y contraseña requeridos.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenPasswordIsMissing()
    {
        var controller = new AuthController(CreateAuthService());

        var result = await controller.Login(new LoginDto { Usuario = "JUAN", Password = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("Usuario y contraseña requeridos.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task Emergencia_ReturnsBadRequest_WhenRequiredFieldsAreMissing()
    {
        var controller = new AuthController(CreateAuthService());

        var result = await controller.Emergencia(new EmergenciaDto
        {
            Usuario = "SUPERVISOR",
            Clave = "clave",
            Justificacion = ""
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("Todos los campos son requeridos.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task Emergencia_ReturnsUnauthorized_WhenSupervisorCredentialsAreInvalid()
    {
        var controller = new AuthController(CreateAuthService());

        var result = await controller.Emergencia(new EmergenciaDto
        {
            Usuario = "SUPERVISOR",
            Clave = "wrong",
            Justificacion = "Se aprueba por excepción."
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.False(GetProperty<bool>(unauthorized.Value!, "ok"));
        Assert.Equal("Credenciales de supervisor incorrectas.", GetProperty<string>(unauthorized.Value!, "mensaje"));
    }

    [Fact]
    public async Task Emergencia_ReturnsOk_WhenSupervisorCredentialsAreValid()
    {
        var controller = new AuthController(CreateAuthService());

        var result = await controller.Emergencia(new EmergenciaDto
        {
            Usuario = "supervisor",
            Clave = "clave-secreta",
            Justificacion = "Se aprueba por excepción."
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetProperty<bool>(ok.Value!, "ok"));
        Assert.Equal("Autorización concedida por supervisor.", GetProperty<string>(ok.Value!, "mensaje"));
        Assert.Equal("supervisor", GetProperty<string>(ok.Value!, "usuario"));
    }

    [Fact]
    public async Task Guardar_ReturnsBadRequest_WhenTextoDescriptivoIsMissing()
    {
        var controller = new MaterialesController(CreateSnowflakeService());

        var result = await controller.Crear(new MaterialDto { TextoDescriptivo = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("El Texto Descriptivo es requerido.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task Actualizar_ReturnsBadRequest_WhenIdIsMissing()
    {
        var controller = new MaterialesController(CreateSnowflakeService());

        var result = await controller.Actualizar("", new ActualizarMaterialDto());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("ID de solicitud requerido.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task Verificar_ReturnsBadRequest_WhenTextoDescriptivoIsMissing()
    {
        var controller = new ChatController(CreateGroqService(), CreateSnowflakeService());

        var result = await controller.Verificar(new VerificarDto { TextoDescriptivo = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("El Texto Descriptivo es requerido.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task Mensaje_ReturnsBadRequest_WhenPreguntaIsMissing()
    {
        var controller = new ChatController(CreateGroqService(), CreateSnowflakeService());

        var result = await controller.Mensaje(new ChatMensajeDto { Pregunta = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("La pregunta es requerida.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task SugerirTexto_ReturnsBadRequest_WhenGrupoExternoIsMissing()
    {
        var controller = new ChatController(CreateGroqService(), CreateSnowflakeService());

        var result = await controller.SugerirTexto(new SugerirTextoDto { TextoCompra = "Banda modular" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("El Grupo Art. Ext. es requerido.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    [Fact]
    public async Task SugerirTexto_ReturnsBadRequest_WhenDetailsAreMissing()
    {
        var controller = new ChatController(CreateGroqService(), CreateSnowflakeService());

        var result = await controller.SugerirTexto(new SugerirTextoDto { IdGrupoExterno = "086" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(GetProperty<bool>(badRequest.Value!, "ok"));
        Assert.Equal("Ingrese referencia, fabricante o texto de compra para sugerir el texto.", GetProperty<string>(badRequest.Value!, "mensaje"));
    }

    private static IAuthService CreateAuthService() => new AuthService(CreateConfiguration());

    private static ISnowflakeService CreateSnowflakeService() => new SnowflakeService(CreateConfiguration());

    private static IGroqService CreateGroqService() => new GroqService(CreateConfiguration(), new TestHttpClientFactory());

    private static IConfiguration CreateConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["Auth:SupervisorUser"] = "SUPERVISOR",
            ["Auth:SupervisorPass"] = "clave-secreta",
            ["Jwt:Key"] = "test-jwt-key-with-enough-length-for-hmac",
            ["Jwt:Issuer"] = "AgentDataApi",
            ["Jwt:Audience"] = "AgentDataAngular"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static T GetProperty<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(value));
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
