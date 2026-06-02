using AgentDataApi.DTOs;

namespace AgentDataApi.Services.Interfaces;

public interface ISnowflakeService
{
    Task<List<Dictionary<string, object>>> BuscarDuplicadosAsync(string textoDescriptivo);
    Task<string> ObtenerUltimoIdAsync();
    Task<string> GuardarSolicitudAsync(MaterialDto datos);
    Task<List<Dictionary<string, object>>> ObtenerSolicitudesAsync();
    Task ActualizarSolicitudAsync(string id, ActualizarMaterialDto datos);
}
