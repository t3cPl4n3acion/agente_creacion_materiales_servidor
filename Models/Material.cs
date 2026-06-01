namespace AgentDataApi.Models
{
	public class Material
	{
		public int Id { get; set; }
		public string TipoSolicitud { get; set; } = string.Empty;
		public string Descripcion { get; set; } = string.Empty;
		public string Grupo { get; set; } = string.Empty;
		public string GrupoExt { get; set; } = string.Empty;
		public string NParte { get; set; } = string.Empty;
		public string Fabricante { get; set; } = string.Empty;
		public string UnidadMedida { get; set; } = string.Empty;
		public decimal ValorCompra { get; set; }
		public string Solicitante { get; set; } = string.Empty;
		public string TextoCompra { get; set; } = string.Empty;
		public string Estado { get; set; } = "Pendiente";
		public string RecomIA { get; set; } = string.Empty;
		public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
	}
}