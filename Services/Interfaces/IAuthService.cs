using AgentDataApi.DTOs;

namespace AgentDataApi.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto dto);
    Task<bool> ValidarEmergenciaAsync(string usuario, string clave);
}
