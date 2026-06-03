namespace AgentDataApi.DTOs
{
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

    public class SugerirTextoDto
    {
        public string GrupoArticulo { get; set; } = string.Empty;
        public string IdGrupoArticulo { get; set; } = string.Empty;
        public string GrupoExterno { get; set; } = string.Empty;
        public string IdGrupoExterno { get; set; } = string.Empty;
        public string NumeroParte { get; set; } = string.Empty;
        public string Fabricante { get; set; } = string.Empty;
        public string TextoCompra { get; set; } = string.Empty;
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

    public class SugerirTextoResponseDto
    {
        public bool Ok { get; set; } = true;
        public string Texto { get; set; } = string.Empty;
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
