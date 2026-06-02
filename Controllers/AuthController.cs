using AgentDataApi.DTOs;
using AgentDataApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgentDataApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ── POST /api/auth/login ──────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Usuario) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { ok = false, mensaje = "Usuario y contraseña requeridos." });

            try
            {
                var resultado = await _authService.LoginAsync(dto);

                if (resultado == null)
                    return Unauthorized(new { ok = false, mensaje = "Credenciales incorrectas." });

                return Ok(new
                {
                    ok = true,
                    token = resultado.Token,
                    nombre = resultado.Nombre,
                    rol = resultado.Rol
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }

        // ── POST /api/auth/emergencia ─────────────────────
        [HttpPost("emergencia")]
        public async Task<IActionResult> Emergencia([FromBody] EmergenciaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Usuario) ||
                string.IsNullOrWhiteSpace(dto.Clave) ||
                string.IsNullOrWhiteSpace(dto.Justificacion))
                return BadRequest(new { ok = false, mensaje = "Todos los campos son requeridos." });

            try
            {
                var valido = await _authService.ValidarEmergenciaAsync(dto.Usuario, dto.Clave);

                if (!valido)
                    return Unauthorized(new { ok = false, mensaje = "Credenciales de supervisor incorrectas." });

                return Ok(new
                {
                    ok = true,
                    mensaje = $"Autorización concedida por {dto.Usuario}.",
                    usuario = dto.Usuario
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }
    }
}
