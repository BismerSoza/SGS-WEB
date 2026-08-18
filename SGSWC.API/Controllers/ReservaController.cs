using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGSWC.API.Models;
using System.Data;

namespace SGSWC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservaController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly NotificacionService _notificacionService;

        public ReservaController(IConfiguration configuration, NotificacionService notificacionService)
        {
            _configuration = configuration;
            _notificacionService = notificacionService;
        }

        #region Reservas

        [HttpGet("ConsultarServicios")]
        public IActionResult ConsultarServicios()
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var resultado = context.Query<ServicioResponseModel>("ConsultarServiciosActivos",
                commandType: CommandType.StoredProcedure).ToList();
            if (resultado.Any()) return Ok(resultado);
            return NotFound();
        }

        [Authorize]
        [HttpPost("CrearReserva")]
        public IActionResult CrearReserva(CrearReservaRequestModel reserva)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_usuario", reserva.Id_Usuario);
            p.Add("@id_servicio", reserva.Id_Servicio);
            p.Add("@fecha", reserva.Fecha);
            p.Add("@hora", reserva.Hora);
            p.Add("@direccion_servicio", reserva.Direccion_Servicio);
            p.Add("@observaciones", reserva.Observaciones);
            var resultado = context.ExecuteScalar<int>("CrearReserva", p,
                commandType: CommandType.StoredProcedure);
            if (resultado > 0) return Ok(resultado);
            return BadRequest();
        }

        [Authorize]
        [HttpGet("ObtenerReservaciones")]
        public IActionResult ObtenerReservaciones(int idUsuario)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_usuario", idUsuario);
            var resultado = context.Query<ReservaResponseModel>("ObtenerReservacionesPorUsuario",
                p, commandType: CommandType.StoredProcedure).ToList();
            return Ok(resultado);
        }

        [Authorize]
        [HttpGet("ObtenerDetalle")]
        public IActionResult ObtenerDetalle(int idReservacion, int idUsuario)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_reservacion", idReservacion);
            p.Add("@id_usuario", idUsuario);
            var resultado = context.QueryFirstOrDefault<ReservaResponseModel>("ObtenerReservacionPorId",
                p, commandType: CommandType.StoredProcedure);
            if (resultado == null) return NotFound();
            return Ok(resultado);
        }

        [Authorize]
        [HttpPost("Cancelar")]
        public IActionResult Cancelar(CancelarRequestModel modelo)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_reservacion", modelo.Id_Reservacion);
            p.Add("@id_usuario", modelo.Id_Usuario);
            p.Add("@motivo", modelo.Motivo);
            p.Add("@detalle", modelo.Detalle);
            var resultado = context.ExecuteScalar<int>("CancelarReservacion",
                p, commandType: CommandType.StoredProcedure);
            if (resultado == 0)
                return BadRequest("No se puede cancelar. La reserva no existe, no te pertenece o ya no está pendiente.");
            return Ok(resultado);
        }

        [Authorize]
        [HttpPost("ModificarFecha")]
        public IActionResult ModificarFecha(ModificarFechaRequestModel modelo)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_reservacion", modelo.Id_Reservacion);
            p.Add("@id_usuario", modelo.Id_Usuario);
            p.Add("@nueva_fecha", modelo.Nueva_Fecha);
            p.Add("@nueva_hora", modelo.Nueva_Hora);
            p.Add("@motivo", modelo.Motivo);
            var resultado = context.ExecuteScalar<int>("ModificarFechaReservacion",
                p, commandType: CommandType.StoredProcedure);
            if (resultado == 0) return NotFound("La reserva no existe o no te pertenece.");
            if (resultado == 2) return BadRequest("Solo se pueden modificar reservas en estado Pendiente.");
            return Ok(resultado);
        }

        [HttpPost]
        [Route("CambiarEstadoConNotificacion")]
        public IActionResult CambiarEstadoConNotificacion(
            [FromBody] CambiarEstadoConNotificacionRequestModel model)
        {
            CambiarEstadoConNotificacionSPResult? spResult;
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@id_reservacion", model.IdReservacion);
                parametros.Add("@id_estado_nuevo", model.IdEstadoNuevo);
                parametros.Add("@id_usuario_admin", model.IdUsuarioAdmin);
                parametros.Add("@motivo", model.Motivo);
                spResult = context.QueryFirstOrDefault<CambiarEstadoConNotificacionSPResult>(
                    "CambiarEstadoConNotificacion",
                    parametros,
                    commandType: CommandType.StoredProcedure);
            }

            if (spResult == null)
                return StatusCode(500, new { mensaje = "Error interno al procesar el cambio de estado." });
            if (spResult.Resultado == -1)
                return NotFound(new { mensaje = "Reservación no encontrada." });
            if (spResult.Resultado == 0)
                return BadRequest(new { mensaje = "La transición de estado no está permitida." });

            if (!spResult.Notificaciones_Activas)
            {
                return Ok(new CambiarEstadoConNotificacionResponseModel
                {
                    Resultado = 1,
                    EmailEnviado = false,
                    MensajeNotificacion = "Notificaciones desactivadas para este usuario."
                });
            }

            try
            {
                _notificacionService.EnviarNotificacionCambioEstado(
                    spResult.Correo_Cliente!,
                    spResult.Nombre_Cliente!,
                    spResult.Nombre_Estado_Anterior!,
                    spResult.Nombre_Estado_Nuevo!);

                RegistrarNotificacionEnBD(spResult.Id_Usuario_Cliente, "CAMBIO_ESTADO",
                    $"Actualización de tu servicio → {spResult.Nombre_Estado_Nuevo}",
                    $"Tu reservación cambió de '{spResult.Nombre_Estado_Anterior}' a '{spResult.Nombre_Estado_Nuevo}'.",
                    "ENVIADO", null);

                return Ok(new CambiarEstadoConNotificacionResponseModel
                {
                    Resultado = 1,
                    EmailEnviado = true,
                    MensajeNotificacion = "Notificación enviada correctamente."
                });
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                RegistrarNotificacionEnBD(spResult.Id_Usuario_Cliente, "CAMBIO_ESTADO",
                    $"Actualización de tu servicio → {spResult.Nombre_Estado_Nuevo}",
                    $"Tu reservación cambió de '{spResult.Nombre_Estado_Anterior}' a '{spResult.Nombre_Estado_Nuevo}'.",
                    "ERROR", errorMsg);

                return StatusCode(207, new CambiarEstadoConNotificacionResponseModel
                {
                    Resultado = 1,
                    EmailEnviado = false,
                    MensajeNotificacion = "Estado actualizado, pero no se pudo enviar el correo. El error fue registrado."
                });
            }
        }

        #endregion

        #region HU-C-003 Pagos PayPal (CORREGIDO)

        [Authorize]
        [HttpGet("ConsultarEstadoPago")]
        public IActionResult ConsultarEstadoPago(int idReservacion, int idUsuario)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var p = new DynamicParameters();
            p.Add("@id_reservacion", idReservacion);
            p.Add("@id_usuario", idUsuario);
            var resultado = context.QueryFirstOrDefault<EstadoPagoResponseModel>(
                "ConsultarEstadoPago", p,
                commandType: CommandType.StoredProcedure);
            if (resultado == null) return NotFound();
            return Ok(resultado);
        }

        [Authorize]
        [HttpPost("CrearOrdenPayPal")]
        public async Task<IActionResult> CrearOrdenPayPal([FromBody] PagoRequestModel modelo)
        {
            try
            {
                // ✅ 1. Validar monto
                if (modelo.Monto <= 0)
                    return BadRequest(new { error = "El monto debe ser mayor a 0." });

                // ✅ 2. Convertir colones a dólares (TIPO DE CAMBIO CONFIGURABLE)
                decimal tipoCambio = ObtenerTipoCambio(); // ✅ Método privado con configuración
                decimal montoUSD = modelo.Monto / tipoCambio;

                // ✅ 3. Redondear a 2 decimales (mínimo PayPal = 1 USD)
                montoUSD = Math.Round(montoUSD, 2);

                // ✅ 4. PayPal requiere mínimo 1 USD
                if (montoUSD < 1.00m)
                    return BadRequest(new
                    {
                        error = "El monto mínimo para pagar con PayPal es de $1.00 USD.",
                        montoEnColones = modelo.Monto,
                        montoEnDolares = montoUSD
                    });

                // ✅ 5. Configurar PayPal
                var clientId = _configuration["PayPal:ClientId"];
                var secret = _configuration["PayPal:Secret"];
                var mode = _configuration["PayPal:Mode"];

                var baseUrl = mode == "sandbox"
                    ? "https://api-m.sandbox.paypal.com"
                    : "https://api-m.paypal.com";

                using var http = new HttpClient();

                // ✅ 6. Obtener token OAuth
                var authBytes = System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secret}");
                var authBase64 = Convert.ToBase64String(authBytes);
                http.DefaultRequestHeaders.Add("Authorization", $"Basic {authBase64}");
                http.DefaultRequestHeaders.Add("Accept", "application/json");
                http.DefaultRequestHeaders.Add("Accept-Language", "en_US");

                var tokenContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var tokenResp = await http.PostAsync($"{baseUrl}/v1/oauth2/token", tokenContent);
                var tokenRespStr = await tokenResp.Content.ReadAsStringAsync();

                if (!tokenResp.IsSuccessStatusCode)
                    return StatusCode(500, new { error = "Error al obtener token de PayPal.", detalle = tokenRespStr });

                var tokenJson = System.Text.Json.JsonDocument.Parse(tokenRespStr).RootElement;

                if (!tokenJson.TryGetProperty("access_token", out var accessTokenElement))
                    return StatusCode(500, new { error = "Token de PayPal inválido.", detalle = tokenRespStr });

                var accessToken = accessTokenElement.GetString();

                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                http.DefaultRequestHeaders.Add("Accept", "application/json");

                // ✅ 7. Crear la orden en PayPal (CON MONTO EN USD CORRECTO)
                var montoUSDStr = montoUSD.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

                var orderBody = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[] {
                        new {
                            amount = new {
                                currency_code = "USD",
                                value = montoUSDStr
                            },
                            description = $"Reservación #{modelo.Id_Reservacion} - SGS Web Clean",
                            custom_id = modelo.Id_Reservacion.ToString(),
                            invoice_id = $"SGS-{modelo.Id_Reservacion}-{DateTime.Now.Ticks}"
                        }
                    },
                    application_context = new
                    {
                        return_url = _configuration["Valores:UrlUI"] + $"/Inicio/PagoExitoso?idReservacion={modelo.Id_Reservacion}",
                        cancel_url = _configuration["Valores:UrlUI"] + $"/Inicio/PagoCancelado?idReservacion={modelo.Id_Reservacion}",
                        brand_name = "SGS Web Clean",
                        landing_page = "NO_PREFERENCE",
                        user_action = "PAY_NOW"
                    }
                };

                var orderContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(orderBody),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var orderResp = await http.PostAsync($"{baseUrl}/v2/checkout/orders", orderContent);
                var orderRespStr = await orderResp.Content.ReadAsStringAsync();

                if (!orderResp.IsSuccessStatusCode)
                {
                    // ✅ Log del error para debugging
                    Console.WriteLine($"Error PayPal: {orderRespStr}");
                    return StatusCode(500, new
                    {
                        error = "Error al crear orden PayPal.",
                        detalle = orderRespStr,
                        montoEnviado = montoUSDStr,
                        montoOriginalCRC = modelo.Monto
                    });
                }

                var orderJson = System.Text.Json.JsonDocument.Parse(orderRespStr).RootElement;
                var orderId = orderJson.GetProperty("id").GetString();
                var approvalUrl = "";

                foreach (var link in orderJson.GetProperty("links").EnumerateArray())
                {
                    if (link.GetProperty("rel").GetString() == "approve")
                    {
                        approvalUrl = link.GetProperty("href").GetString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(approvalUrl))
                    return StatusCode(500, new { error = "No se obtuvo URL de aprobación de PayPal." });

                // ✅ 8. Devolver la orden con el monto convertido
                return Ok(new
                {
                    orderId,
                    approvalUrl,
                    montoOriginalCRC = modelo.Monto,
                    montoConvertidoUSD = montoUSD,
                    tipoCambio = tipoCambio
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al crear la orden de pago.",
                    detalle = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [Authorize]
        [HttpPost("CapturarPagoPayPal")]
        public async Task<IActionResult> CapturarPagoPayPal([FromBody] PagoRequestModel modelo)
        {
            try
            {
                var clientId = _configuration["PayPal:ClientId"];
                var secret = _configuration["PayPal:Secret"];
                var mode = _configuration["PayPal:Mode"];
                var baseUrl = mode == "sandbox"
                    ? "https://api-m.sandbox.paypal.com"
                    : "https://api-m.paypal.com";

                using var http = new HttpClient();

                // Obtener token
                var authBytes = System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secret}");
                var authBase64 = Convert.ToBase64String(authBytes);
                http.DefaultRequestHeaders.Add("Authorization", $"Basic {authBase64}");
                http.DefaultRequestHeaders.Add("Accept", "application/json");
                http.DefaultRequestHeaders.Add("Accept-Language", "en_US");

                var tokenResp = await http.PostAsync(
                    $"{baseUrl}/v1/oauth2/token",
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials")
                    }));

                var tokenRespStr = await tokenResp.Content.ReadAsStringAsync();
                var tokenJson = System.Text.Json.JsonDocument.Parse(tokenRespStr).RootElement;

                if (!tokenJson.TryGetProperty("access_token", out var accessTokenElement))
                    return StatusCode(500, new { error = "Token de PayPal inválido." });

                var accessToken = accessTokenElement.GetString();

                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                http.DefaultRequestHeaders.Add("Accept", "application/json");

                // Capturar pago
                var captureContent = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                var captureResp = await http.PostAsync(
                    $"{baseUrl}/v2/checkout/orders/{modelo.OrderId}/capture",
                    captureContent);

                if (!captureResp.IsSuccessStatusCode)
                {
                    var captureError = await captureResp.Content.ReadAsStringAsync();
                    return BadRequest(new { error = "No se pudo capturar el pago.", detalle = captureError });
                }

                // ✅ Obtener el monto capturado de PayPal
                var captureJson = System.Text.Json.JsonDocument.Parse(await captureResp.Content.ReadAsStringAsync()).RootElement;
                var montoUSD = decimal.Parse(
                    captureJson
                        .GetProperty("purchase_units")[0]
                        .GetProperty("payments")
                        .GetProperty("captures")[0]
                        .GetProperty("amount")
                        .GetProperty("value")
                        .GetString()!,
                    System.Globalization.CultureInfo.InvariantCulture
                );

                // ✅ Convertir USD a CRC para guardar en BD
                decimal tipoCambio = ObtenerTipoCambio();
                decimal montoCRC = montoUSD * tipoCambio;

                // ✅ Registrar en BD con el monto original en colones
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
                var p = new DynamicParameters();
                p.Add("@id_reservacion", modelo.Id_Reservacion);
                p.Add("@id_usuario", modelo.Id_Usuario);
                p.Add("@monto", montoCRC); // ✅ Guardamos en colones
                p.Add("@referencia_externa", modelo.OrderId);
                var resultado = context.ExecuteScalar<int>("RegistrarPagoReservacion", p,
                    commandType: CommandType.StoredProcedure);

                if (resultado == 2)
                    return BadRequest(new { error = "Esta reservación ya fue pagada anteriormente." });

                return Ok(new
                {
                    pagado = true,
                    orderId = modelo.OrderId,
                    montoPagadoUSD = montoUSD,
                    montoPagadoCRC = montoCRC,
                    tipoCambio = tipoCambio
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al procesar el pago.", detalle = ex.Message });
            }
        }

        #endregion

        #region Métodos privados

        private void RegistrarNotificacionEnBD(int idUsuario, string tipo,
            string asunto, string mensaje, string estadoEnvio, string? error)
        {
            try
            {
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
                var parametros = new DynamicParameters();
                parametros.Add("@id_usuario", idUsuario);
                parametros.Add("@tipo", tipo);
                parametros.Add("@asunto", asunto);
                parametros.Add("@mensaje", mensaje);
                parametros.Add("@estado_envio", estadoEnvio);
                parametros.Add("@error", error);
                context.Execute("RegistrarNotificacion", parametros,
                    commandType: CommandType.StoredProcedure);
            }
            catch { }
        }

        /// <summary>
        /// ✅ Obtiene el tipo de cambio desde la configuración
        /// Puedes obtenerlo de appsettings.json, una API externa o base de datos
        /// </summary>
        private decimal ObtenerTipoCambio()
        {
            // ✅ Opción 1: Desde appsettings.json
            var tipoCambioStr = _configuration["AppSettings:TipoCambioCRCtoUSD"];
            if (!string.IsNullOrEmpty(tipoCambioStr) && decimal.TryParse(tipoCambioStr, out var cambio))
                return cambio;

            // ✅ Opción 2: Valor por defecto (500 colones = 1 dólar)
            return 500m;

            // ✅ Opción 3: Desde una API externa (recomendado para producción)
            // return ObtenerTipoCambioDesdeAPI().Result;
        }

        #endregion
    }
}