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

    public class MaterialesQueryDto
    {
        public string? Search { get; set; }
        public string? Estado { get; set; }
        public string? EstadoSAP { get; set; }
        public string? Solicitante { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
    }

    public class MaterialesPageDto
    {
        public bool Ok { get; set; } = true;
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalPages { get; set; } = 1;
        public MaterialesStatsDto Stats { get; set; } = new();
    }

    public class MaterialesStatsDto
    {
        public int Pendientes { get; set; }
        public int Aprobados { get; set; }
        public int Rechazados { get; set; }
    }
}
