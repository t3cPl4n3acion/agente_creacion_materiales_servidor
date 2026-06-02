using AgentDataApi.DTOs;
using AgentDataApi.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Snowflake.Data.Client;

namespace AgentDataApi.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public AuthService(IConfiguration config)
        {
            _config = config;
            _connectionString = BuildConnectionString();
        }

        private string BuildConnectionString()
        {
            var sf = _config.GetSection("Snowflake");
            return $"account={sf["Account"]};user={sf["User"]};password={sf["Password"]};" +
                   $"db={sf["DbMaestro"]};schema=PUBLIC;warehouse={sf["Warehouse"]};role={sf["Role"]}";
        }

        // ── LOGIN — consulta Snowflake igual que Node.js ──
        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            try
            {
                using var conn = new SnowflakeDbConnection();
                conn.ConnectionString = _connectionString;
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT ""USUARIO"", ""CONTRASENA""
                    FROM DB_MANTENIMIENTO.PUBLIC.""DATAMAESUSUAR""
                    WHERE UPPER(""USUARIO"") = UPPER(:usuario)
                    AND ""CONTRASENA"" = :password
                    LIMIT 1";

                var pUsuario = cmd.CreateParameter();
                pUsuario.ParameterName = "usuario";
                pUsuario.Value = dto.Usuario;
                cmd.Parameters.Add(pUsuario);

                var pPassword = cmd.CreateParameter();
                pPassword.ParameterName = "password";
                pPassword.Value = dto.Password;
                cmd.Parameters.Add(pPassword);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                var nombreUsuario = reader["USUARIO"]?.ToString() ?? dto.Usuario;
                var token = GenerarToken(nombreUsuario);

                return new LoginResponseDto
                {
                    Token = token,
                    Nombre = nombreUsuario,
                    Rol = "usuario"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en login Snowflake: {ex.Message}");
            }
        }

        // ── AUTORIZACIÓN DE EMERGENCIA ────────────────────
        public async Task<bool> ValidarEmergenciaAsync(string usuario, string clave)
        {
            var supervisorUser = _config["Auth:SupervisorUser"] ?? "";
            var supervisorPass = _config["Auth:SupervisorPass"] ?? "";

            return await Task.FromResult(
                string.Equals(usuario.Trim().ToUpper(), supervisorUser.Trim().ToUpper(),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(clave.Trim(), supervisorPass.Trim(),
                    StringComparison.Ordinal)
            );
        }

        // ── GENERAR JWT ───────────────────────────────────
        private string GenerarToken(string nombreUsuario)
        {
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Jwt:Key must be configured in appsettings.Development.json or environment variables.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name,  nombreUsuario),
                new Claim("usuario",        nombreUsuario),
                new Claim(ClaimTypes.Role,  "usuario")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "AgentDataApi",
                audience: _config["Jwt:Audience"] ?? "AgentDataAngular",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
