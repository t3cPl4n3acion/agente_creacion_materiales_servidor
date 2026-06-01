namespace AgentDataApi.DTOs
{
	// ── AUTH ──────────────────────────────────────────
	public class LoginDto
	{
		public string Usuario { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class LoginResponseDto
	{
		public string Token { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string Rol { get; set; } = string.Empty;
	}

	public class EmergenciaDto
	{
		public string Usuario { get; set; } = string.Empty;
		public string Clave { get; set; } = string.Empty;
		public string Justificacion { get; set; } = string.Empty;
		public string IdSolicitud { get; set; } = string.Empty;
		public string TextoDescriptivo { get; set; } = string.Empty;
		public string Solicitante { get; set; } = string.Empty;
	}

	// ── MATERIAL ──────────────────────────────────────
	public class MaterialDto
	{
		public string TipoSolicitud { get; set; } = string.Empty;
		public string TextoDescriptivo { get; set; } = string.Empty;
		public string GrupoArticulo { get; set; } = string.Empty;
		public string IdGrupoArticulo { get; set; } = string.Empty;
		public string GrupoExterno { get; set; } = string.Empty;
		public string IdGrupoExterno { get; set; } = string.Empty;
		public string NumeroParte { get; set; } = string.Empty;
		public string Fabricante { get; set; } = string.Empty;
		public string Unidad { get; set; } = string.Empty;
		public decimal ValorCompra { get; set; }
		public string Solicitante { get; set; } = string.Empty;
		public string TextoCompra { get; set; } = string.Empty;
		public string EstadoIA { get; set; } = "APROBADO";
		public string RecomendacionIA { get; set; } = string.Empty;
		public bool TieneAdjunto { get; set; } = false;
		public string? NombreAdjunto { get; set; }
		public string? TipoAdjunto { get; set; }
		public bool TieneExcepcion { get; set; } = false;
		public string? UsuarioAutorizador { get; set; }
		public string? JustificacionExcepcion { get; set; }
	}

	public class ActualizarMaterialDto
	{
		public string TextoDescriptivo { get; set; } = string.Empty;
		public string EstadoIA { get; set; } = string.Empty;
		public string RecomendacionIA { get; set; } = string.Empty;
		public string Solicitante { get; set; } = string.Empty;
	}

	// ── CHAT ──────────────────────────────────────────
	public class VerificarDto
	{
		public string TextoDescriptivo { get; set; } = string.Empty;
		public string TextoCompra { get; set; } = string.Empty;
		public string NumeroParte { get; set; } = string.Empty;
		public string Fabricante { get; set; } = string.Empty;
		public string GrupoArticulo { get; set; } = string.Empty;
		public string IdGrupoExterno { get; set; } = string.Empty;
		public List<AdjuntoDto> Adjuntos { get; set; } = new();
	}

	public class AdjuntoDto
	{
		public string Name { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public string DataUrl { get; set; } = string.Empty;
	}

	public class ChatMensajeDto
	{
		public string Pregunta { get; set; } = string.Empty;
		public List<HistorialDto> Historial { get; set; } = new();
	}

	public class HistorialDto
	{
		public string Role { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
	}

	public class ChatResponseDto
	{
		public bool Ok { get; set; } = true;
		public string Respuesta { get; set; } = string.Empty;
		public bool Bloqueado { get; set; } = false;
		public string? Mensaje { get; set; }
	}

	public class VerificarResponseDto
	{
		public bool Ok { get; set; } = true;
		public VerificarResultadoDto? Resultado { get; set; }
		public string? Mensaje { get; set; }
	}

	public class VerificarResultadoDto
	{
		public bool Aprobado { get; set; }
		public string Estado { get; set; } = string.Empty;
		public string Motivo { get; set; } = string.Empty;
		public string? Sugerencia { get; set; }
		public List<object> Duplicados { get; set; } = new();
		public string? CoherenciaAdjunto { get; set; }
	}
}