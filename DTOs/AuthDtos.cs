namespace AgentDataApi.DTOs
{
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
}
