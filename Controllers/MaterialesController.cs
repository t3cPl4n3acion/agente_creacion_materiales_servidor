using AgentDataApi.DTOs;
using AgentDataApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentDataApi.Controllers
{
    [ApiController]
    [Route("api/materials")]
    [Authorize]
    public class MaterialesController : ControllerBase
    {
        private readonly ISnowflakeService _snowflake;

        public MaterialesController(ISnowflakeService snowflake)
        {
            _snowflake = snowflake;
        }

        // ── POST /api/materials/guardar ───────────────────
        [HttpPost("guardar")]
        public async Task<IActionResult> Guardar([FromBody] MaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TextoDescriptivo))
                return BadRequest(new { ok = false, mensaje = "El Texto Descriptivo es requerido." });

            try
            {
                var idSolicitud = await _snowflake.GuardarSolicitudAsync(dto);

                return Ok(new
                {
                    ok = true,
                    idSolicitud = idSolicitud,
                    mensaje = $"Material {idSolicitud} guardado exitosamente en Snowflake."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }

        // ── GET /api/materials/listar ─────────────────────
        [HttpGet("listar")]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var solicitudes = await _snowflake.ObtenerSolicitudesAsync();
                return Ok(new { ok = true, data = solicitudes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }

        // ── PUT /api/materials/actualizar/:id ─────────────
        [HttpPut("actualizar/{id}")]
        public async Task<IActionResult> Actualizar(string id, [FromBody] ActualizarMaterialDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { ok = false, mensaje = "ID de solicitud requerido." });

            try
            {
                await _snowflake.ActualizarSolicitudAsync(id, dto);
                return Ok(new { ok = true, mensaje = $"Solicitud {id} actualizada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }
    }
}
