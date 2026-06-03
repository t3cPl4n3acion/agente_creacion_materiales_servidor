using AgentDataApi.Controllers;
using AgentDataApi.DTOs;
using AgentDataApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgentDataApi.Tests;

public class ControllerSuccessTests
{
    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        var auth = new FakeAuthService
        {
            LoginResult = new LoginResponseDto
            {
                Token = "jwt",
                Nombre = "Juan Perez",
                Rol = "ADMIN"
            }
        };
        var controller = new AuthController(auth);

        var result = await controller.Login(new LoginDto { Usuario = "JUAN", Password = "secret" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetProperty<bool>(ok.Value!, "ok"));
        Assert.Equal("jwt", GetProperty<string>(ok.Value!, "token"));
        Assert.Equal("Juan Perez", GetProperty<string>(ok.Value!, "nombre"));
        Assert.Equal("ADMIN", GetProperty<string>(ok.Value!, "rol"));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var controller = new AuthController(new FakeAuthService());

        var result = await controller.Login(new LoginDto { Usuario = "JUAN", Password = "wrong" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.False(GetProperty<bool>(unauthorized.Value!, "ok"));
        Assert.Equal("Credenciales incorrectas.", GetProperty<string>(unauthorized.Value!, "mensaje"));
    }

    [Fact]
    public async Task Guardar_ReturnsOk_WithGeneratedSolicitudId()
    {
        var snowflake = new FakeSnowflakeService { SavedId = "SOL-123" };
        var controller = new MaterialesController(snowflake);

        var result = await controller.Crear(new MaterialDto { TextoDescriptivo = "Bomba centrifuga" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetProperty<bool>(ok.Value!, "ok"));
        Assert.Equal("SOL-123", GetProperty<string>(ok.Value!, "idSolicitud"));
        Assert.Equal("Material SOL-123 guardado exitosamente en Snowflake.", GetProperty<string>(ok.Value!, "mensaje"));
        Assert.Equal("Bomba centrifuga", snowflake.SavedMaterial?.TextoDescriptivo);
    }

    [Fact]
    public async Task Listar_ReturnsOk_WithSolicitudes()
    {
        var solicitudes = new List<Dictionary<string, object>>
        {
            new() { ["ID_SOLICITUD"] = "SOL-1", ["ESTADO_IA"] = "APROBADO" }
        };
        var snowflake = new FakeSnowflakeService { Solicitudes = solicitudes };
        var controller = new MaterialesController(snowflake);

        var result = await controller.Listar(new MaterialesQueryDto { Search = "motor", Page = 2, PageSize = 5 });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MaterialesPageDto>(ok.Value);
        Assert.True(response.Ok);
        Assert.Same(solicitudes, response.Data);
        Assert.Equal("motor", snowflake.LastQuery?.Search);
        Assert.Equal(2, snowflake.LastQuery?.Page);
        Assert.Equal(5, snowflake.LastQuery?.PageSize);
    }

    [Fact]
    public async Task Solicitantes_ReturnsOk_WithRequesterOptions()
    {
        var controller = new MaterialesController(new FakeSnowflakeService());

        var result = await controller.Solicitantes();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetProperty<bool>(ok.Value!, "ok"));
        Assert.Equal(new List<string> { "JUAN" }, GetProperty<List<string>>(ok.Value!, "data"));
    }

    [Fact]
    public async Task Actualizar_ReturnsOk_AndPassesRequestToSnowflakeService()
    {
        var snowflake = new FakeSnowflakeService();
        var controller = new MaterialesController(snowflake);

        var result = await controller.Actualizar("SOL-123", new ActualizarMaterialDto { EstadoIA = "RECHAZADO" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetProperty<bool>(ok.Value!, "ok"));
        Assert.Equal("Solicitud SOL-123 actualizada correctamente.", GetProperty<string>(ok.Value!, "mensaje"));
        Assert.Equal("SOL-123", snowflake.UpdatedId);
        Assert.Equal("RECHAZADO", snowflake.UpdatedMaterial?.EstadoIA);
    }

    [Fact]
    public async Task Verificar_ReturnsOk_WithGroqResultAndSnowflakeDuplicates()
    {
        var duplicados = new List<Dictionary<string, object>>
        {
            new() { ["ID_MATERIAL"] = "MAT-1" }
        };
        var groqResult = new VerificarResultadoDto
        {
            Aprobado = false,
            Estado = "RECHAZADO",
            Motivo = "Duplicado"
        };
        var controller = new ChatController(
            new FakeGroqService { VerificarResult = groqResult },
            new FakeSnowflakeService { Duplicados = duplicados });

        var result = await controller.Verificar(new VerificarDto { TextoDescriptivo = "Bomba" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<VerificarResponseDto>(ok.Value);
        Assert.True(response.Ok);
        Assert.Same(groqResult, response.Resultado);
        Assert.NotNull(response.Resultado);
        Assert.Single(response.Resultado.Duplicados);
    }

    [Fact]
    public async Task Mensaje_ReturnsOk_WithGroqAnswer()
    {
        var controller = new ChatController(
            new FakeGroqService { ChatAnswer = "Respuesta IA" },
            new FakeSnowflakeService());

        var result = await controller.Mensaje(new ChatMensajeDto { Pregunta = "Que unidad uso?" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChatResponseDto>(ok.Value);
        Assert.True(response.Ok);
        Assert.Equal("Respuesta IA", response.Respuesta);
    }

    [Fact]
    public async Task SugerirTexto_ReturnsOk_WithSuggestedText()
    {
        var controller = new ChatController(
            new FakeGroqService { SuggestedText = "BANDA PLANA POLIU 5100X950X2" },
            new FakeSnowflakeService());

        var result = await controller.SugerirTexto(new SugerirTextoDto
        {
            IdGrupoExterno = "086",
            GrupoExterno = "BANDAS TRANSPORTADOR",
            TextoCompra = "Banda plana poliuretano 5100x950x2"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SugerirTextoResponseDto>(ok.Value);
        Assert.True(response.Ok);
        Assert.Equal("BANDA PLANA POLIU 5100X950X2", response.Texto);
    }

    private static T GetProperty<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(value));
    }

    private sealed class FakeAuthService : IAuthService
    {
        public LoginResponseDto? LoginResult { get; set; }

        public Task<LoginResponseDto?> LoginAsync(LoginDto dto) => Task.FromResult(LoginResult);

        public Task<bool> ValidarEmergenciaAsync(string usuario, string clave) => Task.FromResult(true);
    }

    private sealed class FakeSnowflakeService : ISnowflakeService
    {
        public string SavedId { get; set; } = "SOL-1";
        public MaterialDto? SavedMaterial { get; private set; }
        public string? UpdatedId { get; private set; }
        public ActualizarMaterialDto? UpdatedMaterial { get; private set; }
        public MaterialesQueryDto? LastQuery { get; private set; }
        public List<Dictionary<string, object>> Duplicados { get; set; } = new();
        public List<Dictionary<string, object>> Solicitudes { get; set; } = new();

        public Task<List<Dictionary<string, object>>> BuscarDuplicadosAsync(string textoDescriptivo) => Task.FromResult(Duplicados);

        public Task<string> ObtenerUltimoIdAsync() => Task.FromResult(SavedId);

        public Task<string> GuardarSolicitudAsync(MaterialDto datos)
        {
            SavedMaterial = datos;
            return Task.FromResult(SavedId);
        }

        public Task<MaterialesPageDto> ObtenerSolicitudesAsync(MaterialesQueryDto query)
        {
            LastQuery = query;
            return Task.FromResult(new MaterialesPageDto
            {
                Data = Solicitudes,
                Total = Solicitudes.Count,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = 1,
                Stats = new MaterialesStatsDto { Aprobados = Solicitudes.Count }
            });
        }

        public Task<List<string>> ObtenerSolicitantesAsync() => Task.FromResult(new List<string> { "JUAN" });

        public Task ActualizarSolicitudAsync(string id, ActualizarMaterialDto datos)
        {
            UpdatedId = id;
            UpdatedMaterial = datos;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGroqService : IGroqService
    {
        public VerificarResultadoDto VerificarResult { get; set; } = new();
        public string ChatAnswer { get; set; } = string.Empty;
        public string SuggestedText { get; set; } = string.Empty;

        public Task<VerificarResultadoDto> VerificarMaterialAsync(
            VerificarDto datos,
            List<Dictionary<string, object>> duplicadosSnowflake) => Task.FromResult(VerificarResult);

        public Task<string> ChatLibreAsync(string pregunta, List<HistorialDto> historial) => Task.FromResult(ChatAnswer);

        public Task<string> SugerirTextoDescriptivoAsync(SugerirTextoDto datos) => Task.FromResult(SuggestedText);
    }
}
