using Snowflake.Data.Client;
using System.Data;

namespace AgentDataApi.Services
{
	public class SnowflakeService
	{
		private readonly IConfiguration _config;

		public SnowflakeService(IConfiguration config)
		{
			_config = config;
		}

		private string BuildConnectionString(string dbKey = "DbSolicitudes")
		{
			var sf = _config.GetSection("Snowflake");
			return $"account={sf["Account"]};user={sf["User"]};password={sf["Password"]};" +
				   $"db={sf[dbKey]};schema=PUBLIC;warehouse={sf["Warehouse"]};role={sf["Role"]}";
		}

		// ── 1. BUSCAR DUPLICADOS en EEO_MaestraMaterial ──
		public async Task<List<Dictionary<string, object>>> BuscarDuplicadosAsync(string textoDescriptivo)
		{
			var resultados = new List<Dictionary<string, object>>();

			var sf = _config.GetSection("Snowflake");
			var db = sf["DbMaestro"];
			var schema = sf["SchemaMaestro"];
			var tabla = sf["TableMaestro"];

			var palabras = textoDescriptivo.Trim().Split(' ')
				.Where(p => p.Length >= 3).ToList();

			var condiciones = string.Join(" AND ",
				palabras.Select(p => $"UPPER(\"MAM_Material\") LIKE UPPER('%{p}%')"));

			var sql = $@"
                SELECT
                    ""MAM_IdMaterial"",
                    ""MAM_Material"",
                    ""MAM_UMB"",
                    ""MAM_IdGrupoArticulo"",
                    ""MAM_IdGrupoMaterial"",
                    ""MAM_IdStatusMaterial""
                FROM {db}.{schema}.""{tabla}""
                WHERE UPPER(""MAM_Material"") LIKE UPPER(:texto)
                {(condiciones.Length > 0 ? $"OR ({condiciones})" : "")}
                LIMIT 20";

			try
			{
				using var conn = new SnowflakeDbConnection();
				conn.ConnectionString = BuildConnectionString("DbMaestro");
				await conn.OpenAsync();

				using var cmd = conn.CreateCommand();
				cmd.CommandText = sql;

				var p = cmd.CreateParameter();
				p.ParameterName = "texto";
				p.Value = $"%{textoDescriptivo}%";
				cmd.Parameters.Add(p);

				using var reader = await cmd.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					resultados.Add(new Dictionary<string, object>
					{
						["MAM_IdMaterial"] = reader["MAM_IdMaterial"],
						["MAM_Material"] = reader["MAM_Material"],
						["MAM_UMB"] = reader["MAM_UMB"],
						["MAM_IdGrupoArticulo"] = reader["MAM_IdGrupoArticulo"],
						["MAM_IdGrupoMaterial"] = reader["MAM_IdGrupoMaterial"],
						["MAM_IdStatusMaterial"] = reader["MAM_IdStatusMaterial"]
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error BuscarDuplicados: {ex.Message}");
			}

			return resultados;
		}

		// ── 2. OBTENER ÚLTIMO ID ──────────────────────────
		public async Task<string> ObtenerUltimoIdAsync()
		{
			var sf = _config.GetSection("Snowflake");
			var db = sf["DbSolicitudes"];
			var schema = sf["SchemaSolicitudes"];
			var tabla = sf["TableSolicitudes"];

			try
			{
				using var conn = new SnowflakeDbConnection();
				conn.ConnectionString = BuildConnectionString("DbSolicitudes");
				await conn.OpenAsync();

				using var cmd = conn.CreateCommand();
				cmd.CommandText = $@"
                    SELECT MAX(""SOM_IdSolicitud"") AS ultimo
                    FROM {db}.{schema}.""{tabla}""";

				using var reader = await cmd.ExecuteReaderAsync();
				if (await reader.ReadAsync())
				{
					var ultimo = reader["ULTIMO"]?.ToString() ?? "000";
					var numero = int.TryParse(ultimo, out var n) ? n + 1 : 1;
					return numero.ToString().PadLeft(3, '0');
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error ObtenerUltimoId: {ex.Message}");
			}

			return "001";
		}

		// ── 3. GUARDAR SOLICITUD ──────────────────────────
		public async Task<string> GuardarSolicitudAsync(DTOs.MaterialDto datos)
		{
			var sf = _config.GetSection("Snowflake");
			var db = sf["DbSolicitudes"];
			var schema = sf["SchemaSolicitudes"];
			var tabla = sf["TableSolicitudes"];

			var idSolicitud = await ObtenerUltimoIdAsync();

			var sql = $@"
                INSERT INTO {db}.{schema}.""{tabla}"" (
                    ""SOM_IdSolicitud"",
                    ""SOM_TipoSolicitud"",
                    ""SOM_TextoDescriptivo"",
                    ""SOM_GrupoArticulo"",
                    ""SOM_IdGrupoArticulo"",
                    ""SOM_GrupoExterno"",
                    ""SOM_IdGrupoExterno"",
                    ""SOM_NumeroParte"",
                    ""SOM_Fabricante"",
                    ""SOM_Unidad"",
                    ""SOM_ValorCompra"",
                    ""SOM_Solicitante"",
                    ""SOM_TextoCompra"",
                    ""SOM_EstadoIA"",
                    ""SOM_RecomendacionIA"",
                    ""SOM_TieneAdjunto"",
                    ""SOM_NombreAdjunto"",
                    ""SOM_TipoAdjunto"",
                    ""SOM_TieneExcepcion"",
                    ""SOM_UsuarioAutorizador"",
                    ""SOM_JustificacionExcepcion""
                ) VALUES (
                    :id, :tipo, :texto, :grupo, :idGrupo,
                    :grupoExt, :idGrupoExt, :nParte, :fabricante,
                    :unidad, :valor, :solicitante, :textoCompra,
                    :estadoIA, :recomIA, :tieneAdj, :nomAdj,
                    :tipoAdj, :tieneExc, :usuAut, :justExc
                )";

			using var conn = new SnowflakeDbConnection();
			conn.ConnectionString = BuildConnectionString("DbSolicitudes");
			await conn.OpenAsync();

			using var cmd = conn.CreateCommand();
			cmd.CommandText = sql;

			void AddParam(string name, object? value)
			{
				var p = cmd.CreateParameter();
				p.ParameterName = name;
				p.Value = value ?? DBNull.Value;
				cmd.Parameters.Add(p);
			}

			AddParam("id", idSolicitud);
			AddParam("tipo", datos.TipoSolicitud);
			AddParam("texto", datos.TextoDescriptivo);
			AddParam("grupo", datos.GrupoArticulo);
			AddParam("idGrupo", datos.IdGrupoArticulo);
			AddParam("grupoExt", datos.GrupoExterno);
			AddParam("idGrupoExt", datos.IdGrupoExterno);
			AddParam("nParte", datos.NumeroParte);
			AddParam("fabricante", datos.Fabricante);
			AddParam("unidad", datos.Unidad);
			AddParam("valor", datos.ValorCompra);
			AddParam("solicitante", datos.Solicitante);
			AddParam("textoCompra", datos.TextoCompra);
			AddParam("estadoIA", datos.EstadoIA);
			AddParam("recomIA", datos.RecomendacionIA);
			AddParam("tieneAdj", datos.TieneAdjunto);
			AddParam("nomAdj", datos.NombreAdjunto);
			AddParam("tipoAdj", datos.TipoAdjunto);
			AddParam("tieneExc", datos.TieneExcepcion);
			AddParam("usuAut", datos.UsuarioAutorizador);
			AddParam("justExc", datos.JustificacionExcepcion);

			await cmd.ExecuteNonQueryAsync();
			return idSolicitud;
		}

		// ── 4. OBTENER TODAS LAS SOLICITUDES ─────────────
		public async Task<List<Dictionary<string, object>>> ObtenerSolicitudesAsync()
		{
			var resultados = new List<Dictionary<string, object>>();
			var sf = _config.GetSection("Snowflake");
			var db = sf["DbSolicitudes"];
			var schema = sf["SchemaSolicitudes"];
			var tabla = sf["TableSolicitudes"];

			try
			{
				using var conn = new SnowflakeDbConnection();
				conn.ConnectionString = BuildConnectionString("DbSolicitudes");
				await conn.OpenAsync();

				using var cmd = conn.CreateCommand();
				cmd.CommandText = $@"
                    SELECT * FROM {db}.{schema}.""{tabla}""
                    ORDER BY ""SOM_FechaSolicitud"" DESC";

				using var reader = await cmd.ExecuteReaderAsync();
				var schema2 = reader.GetSchemaTable();

				while (await reader.ReadAsync())
				{
					var fila = new Dictionary<string, object>();
					for (int i = 0; i < reader.FieldCount; i++)
						fila[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i);
					resultados.Add(fila);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error ObtenerSolicitudes: {ex.Message}");
			}

			return resultados;
		}

		// ── 5. ACTUALIZAR SOLICITUD ───────────────────────
		public async Task ActualizarSolicitudAsync(string id, DTOs.ActualizarMaterialDto datos)
		{
			var sf = _config.GetSection("Snowflake");
			var db = sf["DbSolicitudes"];
			var schema = sf["SchemaSolicitudes"];
			var tabla = sf["TableSolicitudes"];

			using var conn = new SnowflakeDbConnection();
			conn.ConnectionString = BuildConnectionString("DbSolicitudes");
			await conn.OpenAsync();

			using var cmd = conn.CreateCommand();
			cmd.CommandText = $@"
                UPDATE {db}.{schema}.""{tabla}""
                SET
                    ""SOM_TextoDescriptivo"" = :texto,
                    ""SOM_EstadoIA""         = :estado,
                    ""SOM_RecomendacionIA""  = :recom
                WHERE ""SOM_IdSolicitud"" = :id";

			void AddParam(string name, object? value)
			{
				var p = cmd.CreateParameter();
				p.ParameterName = name;
				p.Value = value ?? DBNull.Value;
				cmd.Parameters.Add(p);
			}

			AddParam("texto", datos.TextoDescriptivo);
			AddParam("estado", datos.EstadoIA);
			AddParam("recom", datos.RecomendacionIA);
			AddParam("id", id);

			await cmd.ExecuteNonQueryAsync();
		}
	}
}