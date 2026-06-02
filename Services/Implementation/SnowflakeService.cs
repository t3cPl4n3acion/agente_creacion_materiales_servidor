using AgentDataApi.DTOs;
using AgentDataApi.Services.Interfaces;
using Snowflake.Data.Client;
using System.Data;

namespace AgentDataApi.Services.Implementation
{
    public class SnowflakeService : ISnowflakeService
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
        public async Task<string> GuardarSolicitudAsync(MaterialDto datos)
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
                    ""SOM_JustificacionExcepcion"",
                    ""SOM_EstadoSAP"",
                    ""SOM_IdMaterialSAP""
                ) VALUES (
                    :id, :tipo, :texto, :grupo, :idGrupo,
                    :grupoExt, :idGrupoExt, :nParte, :fabricante,
                    :unidad, :valor, :solicitante, :textoCompra,
                    :estadoIA, :recomIA, :tieneAdj, :nomAdj,
                    :tipoAdj, :tieneExc, :usuAut, :justExc,
                    :estadoSAP, :idMaterialSAP
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
            AddParam("estadoSAP", EsAprobado(datos.EstadoIA) ? "PENDIENTE" : null);
            AddParam("idMaterialSAP", null);

            await cmd.ExecuteNonQueryAsync();
            return idSolicitud;
        }

        // ── 4. OBTENER TODAS LAS SOLICITUDES ─────────────
        public async Task<MaterialesPageDto> ObtenerSolicitudesAsync(MaterialesQueryDto query)
        {
            var resultados = new List<Dictionary<string, object>>();
            var sf = _config.GetSection("Snowflake");
            var db = sf["DbSolicitudes"];
            var schema = sf["SchemaSolicitudes"];
            var tabla = sf["TableSolicitudes"];
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var offset = (page - 1) * pageSize;
            var total = 0;
            var stats = new MaterialesStatsDto();

            try
            {
                using var conn = new SnowflakeDbConnection();
                conn.ConnectionString = BuildConnectionString("DbSolicitudes");
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                var where = BuildSolicitudesWhereClause(cmd, query);
                cmd.CommandText = $@"
                    SELECT
                        ""SOM_IdSolicitud"",
                        ""SOM_TextoDescriptivo"",
                        ""SOM_TipoSolicitud"",
                        ""SOM_GrupoArticulo"",
                        ""SOM_NumeroParte"",
                        ""SOM_Unidad"",
                        ""SOM_Solicitante"",
                        ""SOM_EstadoIA"",
                        ""SOM_EstadoSAP"",
                        ""SOM_IdMaterialSAP"",
                        ""SOM_FechaSolicitud"",
                        ""SOM_RecomendacionIA"",
                        COUNT(*) OVER() AS ""__Total"",
                        SUM(CASE WHEN UPPER(COALESCE(""SOM_EstadoIA"", '')) IN ('', 'REVISION') THEN 1 ELSE 0 END) OVER() AS ""__Pendientes"",
                        SUM(CASE WHEN UPPER(COALESCE(""SOM_EstadoIA"", '')) = 'APROBADO' THEN 1 ELSE 0 END) OVER() AS ""__Aprobados"",
                        SUM(CASE WHEN UPPER(COALESCE(""SOM_EstadoIA"", '')) IN ('RECHAZADO', 'BLOQUEADO') THEN 1 ELSE 0 END) OVER() AS ""__Rechazados""
                    FROM {db}.{schema}.""{tabla}""
                    {where}
                    ORDER BY ""SOM_FechaSolicitud"" DESC
                    LIMIT {pageSize} OFFSET {offset}";

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (total == 0)
                    {
                        total = ToInt(GetValue(reader, "__Total"));
                        stats.Pendientes = ToInt(GetValue(reader, "__Pendientes"));
                        stats.Aprobados = ToInt(GetValue(reader, "__Aprobados"));
                        stats.Rechazados = ToInt(GetValue(reader, "__Rechazados"));
                    }

                    var fila = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        if (columnName.StartsWith("__", StringComparison.Ordinal))
                            continue;

                        fila[columnName] = reader.IsDBNull(i) ? "" : reader.GetValue(i);
                    }

                    resultados.Add(fila);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ObtenerSolicitudes: {ex.Message}");
            }

            return new MaterialesPageDto
            {
                Data = resultados,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)),
                Stats = stats
            };
        }

        public async Task<List<string>> ObtenerSolicitantesAsync()
        {
            var solicitantes = new List<string>();
            var sf = _config.GetSection("Snowflake");
            var db = sf["DbSolicitudes"];
            var schema = sf["SchemaSolicitudes"];
            var tabla = sf["TableSolicitudes"];

            using var conn = new SnowflakeDbConnection();
            conn.ConnectionString = BuildConnectionString("DbSolicitudes");
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT DISTINCT ""SOM_Solicitante""
                FROM {db}.{schema}.""{tabla}""
                WHERE ""SOM_Solicitante"" IS NOT NULL AND ""SOM_Solicitante"" <> ''
                ORDER BY ""SOM_Solicitante""";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var solicitante = reader["SOM_Solicitante"]?.ToString();
                if (!string.IsNullOrWhiteSpace(solicitante))
                    solicitantes.Add(solicitante);
            }

            return solicitantes;
        }

        // ── 5. ACTUALIZAR SOLICITUD ───────────────────────
        public async Task ActualizarSolicitudAsync(string id, ActualizarMaterialDto datos)
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
                    ""SOM_RecomendacionIA""  = :recom,
                    ""SOM_EstadoSAP""        = CASE
                        WHEN :estadoSAP IS NOT NULL THEN :estadoSAP
                        ELSE ""SOM_EstadoSAP""
                    END
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
            AddParam("estadoSAP", EsAprobado(datos.EstadoIA) ? "PENDIENTE" : null);
            AddParam("id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        private static bool EsAprobado(string? estado) =>
            string.Equals(estado?.Trim(), "APROBADO", StringComparison.OrdinalIgnoreCase);

        private static string BuildSolicitudesWhereClause(IDbCommand cmd, MaterialesQueryDto query)
        {
            var conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var value = $"%{query.Search.Trim()}%";
                AddParam(cmd, "searchTexto", value);
                AddParam(cmd, "searchSolicitante", value);
                AddParam(cmd, "searchNumeroParte", value);
                AddParam(cmd, "searchId", value);
                conditions.Add(@"(
                    UPPER(""SOM_TextoDescriptivo"") LIKE UPPER(:searchTexto)
                    OR UPPER(""SOM_Solicitante"") LIKE UPPER(:searchSolicitante)
                    OR UPPER(""SOM_NumeroParte"") LIKE UPPER(:searchNumeroParte)
                    OR UPPER(""SOM_IdSolicitud"") LIKE UPPER(:searchId)
                )");
            }

            if (!string.IsNullOrWhiteSpace(query.Estado))
            {
                var estado = NormalizeEstadoIA(query.Estado);
                if (estado == "PENDIENTE")
                {
                    conditions.Add("UPPER(COALESCE(\"SOM_EstadoIA\", '')) IN ('', 'REVISION')");
                }
                else
                {
                    AddParam(cmd, "estado", estado);
                    conditions.Add("UPPER(\"SOM_EstadoIA\") = :estado");
                }
            }

            if (!string.IsNullOrWhiteSpace(query.EstadoSAP))
            {
                AddParam(cmd, "estadoSAP", query.EstadoSAP.Trim().ToUpperInvariant());
                conditions.Add("UPPER(\"SOM_EstadoSAP\") = :estadoSAP");
            }

            if (!string.IsNullOrWhiteSpace(query.Solicitante))
            {
                AddParam(cmd, "solicitante", query.Solicitante.Trim());
                conditions.Add("\"SOM_Solicitante\" = :solicitante");
            }

            return conditions.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", conditions)}";
        }

        private static void AddParam(IDbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        private static string NormalizeEstadoIA(string estado)
        {
            return estado.Trim().ToUpperInvariant() switch
            {
                "APROBADO" or "APROBADA" => "APROBADO",
                "BLOQUEADO" or "BLOQUEADA" => "BLOQUEADO",
                "RECHAZADO" or "RECHAZADA" => "RECHAZADO",
                "PENDIENTE" or "REVISION" or "REVISIÓN" => "PENDIENTE",
                "EXCEPCION" or "EXCEPCIÓN" => "EXCEPCION",
                var value => value
            };
        }

        private static int ToInt(object value)
        {
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static object GetValue(IDataRecord reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            }

            return DBNull.Value;
        }
    }
}
