namespace AgentDataApi.DTOs
{
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
}
