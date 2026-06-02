using AgentDataApi.DTOs;

namespace AgentDataApi.Services.Interfaces;

public interface IGroqService
{
    Task<VerificarResultadoDto> VerificarMaterialAsync(
        VerificarDto datos,
        List<Dictionary<string, object>> duplicadosSnowflake);

    Task<string> ChatLibreAsync(string pregunta, List<HistorialDto> historial);
}
