using AgentDataApi.DTOs;
using AgentDataApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentDataApi.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IGroqService _groq;
        private readonly ISnowflakeService _snowflake;

        public ChatController(IGroqService groq, ISnowflakeService snowflake)
        {
            _groq = groq;
            _snowflake = snowflake;
        }

        // ── POST /api/chat/verificar ──────────────────────
        [HttpPost("verificar")]
        public async Task<IActionResult> Verificar([FromBody] VerificarDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TextoDescriptivo))
                return BadRequest(new { ok = false, mensaje = "El Texto Descriptivo es requerido." });

            try
            {
                // 1. Buscar duplicados en Snowflake
                var duplicados = await _snowflake.BuscarDuplicadosAsync(dto.TextoDescriptivo);

                // 2. Verificar con Groq IA
                var resultado = await _groq.VerificarMaterialAsync(dto, duplicados);

                // 3. Agregar duplicados de Snowflake al resultado
                if (duplicados.Count > 0)
                    resultado.Duplicados = duplicados.Cast<object>().ToList();

                return Ok(new VerificarResponseDto
                {
                    Ok = true,
                    Resultado = resultado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ok = false,
                    mensaje = ex.Message
                });
            }
        }

        // ── POST /api/chat/mensaje ────────────────────────
        [HttpPost("mensaje")]
        public async Task<IActionResult> Mensaje([FromBody] ChatMensajeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Pregunta))
                return BadRequest(new { ok = false, mensaje = "La pregunta es requerida." });

            try
            {
                var respuesta = await _groq.ChatLibreAsync(dto.Pregunta, dto.Historial);

                return Ok(new ChatResponseDto
                {
                    Ok = true,
                    Respuesta = respuesta
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    ok = false,
                    mensaje = ex.Message
                });
            }
        }
    }
}
