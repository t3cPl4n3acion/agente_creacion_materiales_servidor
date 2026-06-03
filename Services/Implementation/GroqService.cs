using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgentDataApi.DTOs;
using AgentDataApi.Services.Interfaces;

namespace AgentDataApi.Services.Implementation
{
    public class GroqService : IGroqService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        private const string GROQ_URL = "https://api.groq.com/openai/v1/chat/completions";

        private const string POLITICA = @"
Eres el Asistente Virtual de Data Maestra de Repuestos y Suministros de SUPER DE ALIMENTOS S.A.
Tu función es analizar solicitudes de creación de materiales en SAP verificando la política oficial de nomenclatura taxonómica.

════════════════════════════════════════════════════
TIPOS DE MATERIAL SAP
════════════════════════════════════════════════════
ZREP = Repuestos (activos productivos, maquinaria industrial)
ZSUM = Suministros (pinturas, lubricantes, materiales mecanizar, metalmecánica, combustibles, abrasivos)

════════════════════════════════════════════════════
TIPOS DE NOMENCLATURA — REGLA FUNDAMENTAL
════════════════════════════════════════════════════

════════════════════════════════════════════════════
REGLA DEFINITIVA PARA MATERIALES ESPECÍFICOS
════════════════════════════════════════════════════

Un material ESPECÍFICO es válido cuando tiene esta estructura:
TIPO_COMPONENTE + REFERENCIA + MARCA

Para validar un ESPECÍFICO usa estas 3 preguntas:

PREGUNTA 1 — ¿Tiene un TIPO al inicio?
El primer término debe ser el nombre del componente.
Ejemplos válidos: DRIVER, SENSOR, MOTOR, RODAMIENTO, VARIADOR, PLC, CILINDRO, INTERRUPTOR, MOTOREDUCTOR, REDUCTOR, BOMBA, TARJETA, RESISTENCIA, MORDAZA, CUCHILLA, etc.

PREGUNTA 2 — ¿Tiene una REFERENCIA en el medio?
La referencia es cualquier combinación de letras y números que identifica
unívocamente el producto para ese fabricante.
Puede tener guiones, puntos, barras. No tiene un formato estándar.
Ejemplos válidos de referencias: 6205 2RS, TB6560AJ, TB6560AJESDFS-G,
RF77DRN100L4, MCLTPB0550-2A3-4-10, IGT204, DSBC-32-80-PPVA-N3,
3VA5 32A 3, NET7U34PNN201, 2282 AZ-IEC 90, 1FK2104-4AF0, W605159,
2080-LC20-20QBB, 011-00592-000

PREGUNTA 3 — ¿Tiene una MARCA al final?
La marca es la última palabra o grupo de palabras del texto.
La marca puede ser cualquier nombre comercial — NO necesitas conocerla.
Si la última palabra no es una unidad de medida (MM, CM, HP, KW, V, A, BAR, PSI, RPM)
ni una característica técnica, entonces ES la marca.

REGLA DE APROBACIÓN ESPECÍFICO:
Si responde SÍ a las 3 preguntas → APROBAR
Si falta alguna de las 3 → BLOQUEAR indicando cuál falta

EJEMPLOS APROBADOS:
- DRIVER TB6560AJ TOSHIBA → Tipo=DRIVER ✅ Ref=TB6560AJ ✅ Marca=TOSHIBA ✅ → APROBADO
- DRIVER TB6560AJESDFS-G TOSHIBA → Tipo=DRIVER ✅ Ref=TB6560AJESDFS-G ✅ Marca=TOSHIBA ✅ → APROBADO
- RODAMIENTO BOLAS 6205 2RS SKF → Tipo=RODAMIENTO BOLAS ✅ Ref=6205 2RS ✅ Marca=SKF ✅ → APROBADO
- SENSOR INDUCTIVO NPN IGT204 IFM → Tipo=SENSOR INDUCTIVO NPN ✅ Ref=IGT204 ✅ Marca=IFM ✅ → APROBADO
- MOTOREDUCTOR 5HP RF77DRN100L4 SEW → Tipo=MOTOREDUCTOR 5HP ✅ Ref=RF77DRN100L4 ✅ Marca=SEW ✅ → APROBADO
- VARIADOR MCLTPB0550-2A3-4-10 SEW → Tipo=VARIADOR ✅ Ref=MCLTPB0550-2A3-4-10 ✅ Marca=SEW ✅ → APROBADO

EJEMPLOS BLOQUEADOS:
- DRIVER TOSHIBA → Tipo=DRIVER ✅ Ref=FALTA ❌ Marca=TOSHIBA ✅ → BLOQUEADO (falta referencia)
- DRIVER TB6560AJ → Tipo=DRIVER ✅ Ref=TB6560AJ ✅ Marca=FALTA ❌ → BLOQUEADO (falta marca)
- TB6560AJ TOSHIBA → Tipo=FALTA ❌ → BLOQUEADO (falta tipo de componente)
- DRIVER GENERICO TOSHIBA → Tiene palabra prohibida GENERICO → BLOQUEADO

════════════════════════════════════════════════════
REGLA DEFINITIVA PARA MATERIALES GENÉRICOS
════════════════════════════════════════════════════

Un material GENÉRICO es válido cuando tiene:
TIPO_COMPONENTE + CARACTERÍSTICA(S) TÉCNICA(S) + DIMENSIÓN o MEDIDA
SIN incluir marca ni proveedor.

Para validar un GENÉRICO usa estas 3 preguntas:

PREGUNTA 1 — ¿Tiene un TIPO al inicio?
PREGUNTA 2 — ¿Tiene al menos una característica técnica medible?
(material, voltaje, corriente, dimensión, presión, caudal, paso, calibre, etc.)
PREGUNTA 3 — ¿NO tiene marca al final?
Si la última palabra parece un nombre comercial en lugar de una medida → está mal

EJEMPLOS APROBADOS GENÉRICO:
- CONTACTOR TRIPOLAR 220V 25A → ✅
- CABLE ENCAUCHETADO COBRE 3X12 AWG → ✅
- RETENEDOR NBR 25X42X7 → ✅
- VALVULA MARIPOSA ACERO INOX 2"" NEUMATICA → ✅
- ACEITE HIDRAULICO ISO VG46 BIDON 20L → ✅
- CORREA DENTADA POLIURETANO HTD 5M-1000 → ✅

════════════════════════════════════════════════════
TAXONOMÍA REQUERIDA POR GRUPO Y SUBGRUPO
════════════════════════════════════════════════════

RODAMIENTOS Y RETENEDORES:
- RODAMIENTO (ESPECÍFICO): TIPO + MATERIAL/CARACTERÍSTICA + CÓDIGO + DISEÑO + MARCA
- RETENEDOR (GENÉRICO): TIPO + MATERIAL + D.INT x D.EXT x ALTURA
- SOPORTE RODAMIENTO (GENÉRICO): TIPO + MATERIAL + TIPO SOPORTE + CÓDIGO + D.EJE

MOTORES, MOTORREDUCTORES, REDUCTORES (ESPECÍFICO):
- MOTORES: TIPO + REFERENCIA + MARCA
- MOTORREDUCTORES: TIPO + POTENCIA + REFERENCIA + MARCA
- REDUCTORES: TIPO + REFERENCIA + RELACIÓN + MARCA

ELÉCTRICOS:
- CABLE (GENÉRICO): TIPO + MATERIAL + CALIBRE + N° HILOS + AISLAMIENTO
- ILUMINACIÓN (GENÉRICO): TIPO + CARACTERÍSTICA + TENSIÓN + DIMENSIÓN
- CONSUMIBLES (GENÉRICO): TIPO + UTILIDAD + MEDIDA

AUTOMATIZACIÓN, CONTROL, INSTRUMENTACIÓN:
- CONTACTOR (GENÉRICO): TIPO + TENSIÓN + CORRIENTE
- INTERRUPTOR (ESPECÍFICO): TIPO + REFERENCIA + MARCA
- VARIADOR/SERVODRIVE (ESPECÍFICO): TIPO + REFERENCIA + MARCA
- PLC (ESPECÍFICO): TIPO + REFERENCIA + MARCA
- SENSOR (ESPECÍFICO): TIPO + REFERENCIA + MARCA
- MÓDULO (ESPECÍFICO): TIPO + REFERENCIA + MARCA

TUBERÍA Y ACCESORIOS (GENÉRICO):
- TUBERÍA: TIPO + MATERIAL + DIMENSIÓN + ESPESOR SCH + COSTURA
- VÁLVULA: TIPO + MATERIAL + DIMENSIÓN + ACCIONAMIENTO
- MANGUERA: TIPO + MATERIAL + DIMENSIÓN + CONEXIÓN

TRANSMISIÓN DE POTENCIA (GENÉRICO):
- CORREA: TIPO + MATERIAL + CARACTERÍSTICA + DIMENSIÓN
- PIÑÓN: TIPO + CARACTERÍSTICA + PASO + N° DIENTES + MATERIAL
- CADENA: TIPO + MATERIAL + CARACTERÍSTICA + PASO

NEUMATICOS:
- RACOR (GENÉRICO): TIPO + MATERIAL + CONEXIÓN + DIÁMETRO
- CILINDRO (ESPECÍFICO): TIPO + REFERENCIA + MARCA
- FILTRO (GENÉRICO): TIPO + MATERIAL + MESH + DIMENSIONES

TORNILLERÍA Y SISTEMAS DE FIJACIÓN (GENÉRICO):
- TIPO + MATERIAL + CABEZA + MEDIDA + LONGITUD

LUBRICANTES:
- GENÉRICO: TIPO + CARACTERÍSTICA + VOLUMEN → ACEITE HIDRAULICO ISO VG46 BIDON 20L
- ESPECÍFICO: TIPO + REFERENCIA + PRESENTACIÓN + MARCA → ACEITE TIPO L 011-00592-000 5GL YORK

════════════════════════════════════════════════════
REGLAS GENERALES OBLIGATORIAS
════════════════════════════════════════════════════
1. SIEMPRE en MAYÚSCULAS completas
2. Máximo 40 caracteres en el texto descriptivo
3. NO incluir marca en materiales GENÉRICOS (política de compras)
4. SÍ incluir referencia y marca en materiales ESPECÍFICOS
5. NO usar: GENERICO, VARIOS, MISC, REPUESTO, PIEZA, OTRO, ETC, KIT
6. NO usar abreviaciones: PEQ, GEN, REP, VAR, PEQUE, GRND, STD, NUEVO, IMPORTADO, ORIGINAL
7. GENÉRICOS deben tener al menos una característica técnica medible
8. ESPECÍFICOS deben tener referencia/código del fabricante y marca

════════════════════════════════════════════════════
DETECCIÓN DE DUPLICADOS
════════════════════════════════════════════════════
Un material es DUPLICADO si:
- El texto descriptivo es idéntico o muy similar (>80% coincidencia)
- El número de parte o referencia coincide con uno ya registrado
- La descripción técnica es equivalente aunque use diferente redacción
Siempre indicar el ID del material existente si hay duplicado.

════════════════════════════════════════════════════
FORMATO DE RESPUESTA — SOLO JSON PURO SIN MARKDOWN
════════════════════════════════════════════════════
{
    ""aprobado"": true o false,
    ""estado"": ""APROBADO"" o ""BLOQUEADO"",
    ""motivo"": ""explicación clara indicando qué falta o qué está mal"",
    ""sugerencia"": ""texto corregido máximo 40 caracteres o null"",
    ""duplicados"": [],
    ""coherenciaAdjunto"": null
}";

        public GroqService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _http = httpClientFactory.CreateClient();
        }

        // ── 1. VERIFICAR MATERIAL ─────────────────────────
        public async Task<VerificarResultadoDto> VerificarMaterialAsync(
            VerificarDto datos,
            List<Dictionary<string, object>> duplicadosSnowflake)
        {
            var duplicadosTexto = duplicadosSnowflake.Count > 0
                ? string.Join("\n", duplicadosSnowflake.Select(d =>
                    $"  • ID SAP: {d["MAM_IdMaterial"]} | Descripción: {d["MAM_Material"]} | Unidad: {d["MAM_UMB"]} | Grupo: {d["MAM_IdGrupoMaterial"]}"))
                : "  • Ninguno encontrado";

            var mensajeUsuario = $@"
			Analiza esta solicitud de creación de material en SAP para SUPER DE ALIMENTOS:

			- Texto Descriptivo: ""{datos.TextoDescriptivo}"" (longitud exacta: {datos.TextoDescriptivo.Length} caracteres de máximo 40 permitidos)
			- Grupo de Artículos: ""{datos.GrupoArticulo}""
			- Grupo Art. Externo: ""{datos.IdGrupoExterno}""
			- N° de Parte / Referencia: ""{datos.NumeroParte}""
			- Fabricante: ""{datos.Fabricante}""
			- Texto de Compra: ""{datos.TextoCompra}""

			IMPORTANTE: El conteo de caracteres es EXACTO. NO recalcules la longitud.

			Materiales similares encontrados en SAP (Snowflake):
			{duplicadosTexto}

			INSTRUCCIÓN CRÍTICA SOBRE DUPLICADOS:
			- Si encontró materiales similares en Snowflake, DEBES mencionarlos explícitamente en el motivo.
			- Indica el ID SAP y la descripción exacta del material existente.
			- Ejemplo de motivo con duplicado: ""Posible duplicado del material ID 100432 - VARIADOR VFD525 480VAC SEW ya registrado en SAP.""
			- Si hay duplicados → estado BLOQUEADO obligatoriamente.
			- Si no hay duplicados → evalúa solo la nomenclatura.

			Responde SOLO en formato JSON puro sin markdown.";

            var payload = new
            {
                model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system",  content = POLITICA },
                    new { role = "user",    content = mensajeUsuario }
                },
                temperature = 0.1,
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, GROQ_URL);
            request.Headers.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            try
            {
                var groqRes = JsonSerializer.Deserialize<JsonElement>(body);
                var content = groqRes
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                var clean = content.Replace("```json", "").Replace("```", "").Trim();
                var resultado = JsonSerializer.Deserialize<VerificarResultadoDto>(clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return resultado ?? FallbackError();
            }
            catch
            {
                return FallbackError();
            }
        }

        // ── 2. CHAT LIBRE ─────────────────────────────────
        public async Task<string> ChatLibreAsync(string pregunta, List<HistorialDto> historial)
        {
            var system = @"
Eres el Asistente Virtual de Data Maestra de Super de Alimentos.
Ayudas a los usuarios a registrar correctamente repuestos en SAP.
Respondes de forma clara, concisa y en español.
Solo respondes preguntas relacionadas con data maestra, repuestos, SAP y el formulario de registro.
Si te preguntan algo fuera de ese contexto, indica amablemente que solo puedes ayudar con data maestra.";

            var messages = new List<object>
            {
                new { role = "system", content = system }
            };

            foreach (var h in historial)
                messages.Add(new { role = h.Role, content = h.Content });

            messages.Add(new { role = "user", content = pregunta });

            var payload = new
            {
                model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile",
                messages = messages,
                temperature = 0.7,
                max_tokens = 500
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, GROQ_URL);
            request.Headers.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            try
            {
                var groqRes = JsonSerializer.Deserialize<JsonElement>(body);
                return groqRes
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Sin respuesta del agente.";
            }
            catch
            {
                return "Error al conectar con el agente IA.";
            }
        }

        public async Task<string> SugerirTextoDescriptivoAsync(SugerirTextoDto datos)
        {
            var system = @"
Eres un especialista SAP de Data Maestra para textos descriptivos de materiales.
Tu tarea es proponer UN SOLO texto descriptivo compacto, en MAYÚSCULAS, máximo 40 caracteres.
No expliques. No uses markdown. No uses comillas. Responde solo el texto.

Estilo esperado:
TUBO PTS AC 50X80X2.5MM
BANDA PLANA POLIU 5100X950X2
BAND PLAST MOD BOLER 6300MM 540MM 5MM
BAND POLIURETANO BOLERO 7700MM 390MM
BANDA MOD ENTRE TAMBORES MOGA 6000MM
BANDA MOD. SALIDA TUNEL MOGA 12000MM
BANDA PU CANGI/BOLEROS 18000X500X3
RODAMIENTO F685 2RS
SERVOMOTOR R911309762 REXROTH
BOMBA SUMERGIBLE 7.5HP 85SSI07F66-0763
TEE NEUMATICA T8MM PE FESTO
CUCHILLA ABRE FACIL 35.9X16.6MM
CILINDRO ADN-S-32-15-A-P FESTO

Reglas:
- Usa el Grupo Art. Externo como señal principal de taxonomía.
- Si hay referencia y fabricante, usa estructura TIPO + REFERENCIA + MARCA.
- Si es genérico, usa TIPO + MATERIAL/CARACTERÍSTICA + MEDIDA.
- Abrevia sin perder claridad: POLIURETANO puede ser POLIU si ayuda al límite.
- Conserva medidas, referencias, puntos, guiones, slash, # y Ø si aplican.
- No agregues palabras como GENERICO, REPUESTO, VARIOS, PIEZA, OTRO.";

            var user = $@"
Datos para sugerir texto descriptivo SAP:
- Grupo Artículo: {datos.IdGrupoArticulo} {datos.GrupoArticulo}
- Grupo Art. Externo: {datos.IdGrupoExterno} {datos.GrupoExterno}
- N° Parte / Referencia: {datos.NumeroParte}
- Fabricante / Marca: {datos.Fabricante}
- Texto de Compra: {datos.TextoCompra}

Devuelve solo un texto de máximo 40 caracteres.";

            var payload = new
            {
                model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user }
                },
                temperature = 0.1,
                max_tokens = 80
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, GROQ_URL);
            request.Headers.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            try
            {
                var groqRes = JsonSerializer.Deserialize<JsonElement>(body);
                var content = groqRes
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return NormalizeSuggestedText(content);
            }
            catch
            {
                return BuildFallbackSuggestion(datos);
            }
        }

        private static VerificarResultadoDto FallbackError() => new()
        {
            Aprobado = false,
            Estado = "BLOQUEADO",
            Motivo = "Error al procesar la respuesta de la IA. Intente nuevamente.",
            Sugerencia = null,
            Duplicados = new()
        };

        private static string NormalizeSuggestedText(string value)
        {
            var firstLine = value
                .Replace("```", "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;
            var clean = firstLine.Trim().Trim('"', '\'', '.', '-', '*');

            clean = Regex.Replace(clean.ToUpperInvariant(), @"[^A-Z0-9ÑÁÉÍÓÚÜØ\s\-\./#]", " ");
            clean = Regex.Replace(clean, @"\s+", " ").Trim();

            if (clean.Length <= 40) return clean;

            var truncated = clean[..40].TrimEnd();
            var lastSpace = truncated.LastIndexOf(' ');
            return lastSpace >= 28 ? truncated[..lastSpace].TrimEnd() : truncated;
        }

        private static string BuildFallbackSuggestion(SugerirTextoDto datos)
        {
            var parts = new[]
            {
                FirstMeaningfulWord(datos.GrupoExterno),
                datos.NumeroParte,
                datos.Fabricante
            };

            return NormalizeSuggestedText(string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p))));
        }

        private static string FirstMeaningfulWord(string value)
        {
            return value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(word => word.Length > 2) ?? string.Empty;
        }
    }
}
