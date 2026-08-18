using Microsoft.AspNetCore.Mvc;
using SGSWC.UI.Models;
using System.Net.Http.Headers;

namespace SGSWC.UI.Controllers
{
    [SeguridadRol(1, 2, 3, 4)]
    public class InicioController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _configuration;

        public InicioController(IHttpClientFactory http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        private HttpClient Cliente()
        {
            var client = _http.CreateClient();
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        #region Index

        [Seguridad]
        [HttpGet]
        public IActionResult Index()
        {
            using var client = Cliente();

            var urlApi = _configuration["Valores:UrlAPI"] + "Reserva/ConsultarServicios";

            var respuesta = client.GetAsync(urlApi).Result;

            if (respuesta.IsSuccessStatusCode)
            {
                var datos = respuesta.Content
                    .ReadFromJsonAsync<List<ServicioModel>>().Result;
                return View(datos);
            }
            return View(new List<ServicioModel>());
        }

        #endregion

        #region HU-C-001 Reservar Servicio

        [Seguridad]
        [HttpGet]
        public IActionResult ReservarServicio(int? idServicio)
        {
            using var client = Cliente();
            var url = _configuration["Valores:UrlAPI"] + "Reserva/ConsultarServicios";
            var resp = client.GetAsync(url).Result;
            var servicios = resp.IsSuccessStatusCode
                ? resp.Content.ReadFromJsonAsync<List<ServicioModel>>().Result ?? new()
                : new List<ServicioModel>();
            ViewBag.Servicios = servicios;

            var model = new ReservaModel();
            if (idServicio.HasValue)
            {
                model.Id_Servicio = idServicio.Value;
            }

            return View(model);
        }

        [Seguridad]
        [HttpPost]
        public IActionResult ReservarServicio(ReservaModel reserva)
        {
            // Escenario 2: campos obligatorios
            if (reserva.Id_Servicio == 0 || string.IsNullOrEmpty(reserva.Fecha) ||
                string.IsNullOrEmpty(reserva.Hora) || string.IsNullOrEmpty(reserva.Direccion_Servicio))
            {
                TempData["Error"] = "Todos los campos obligatorios deben completarse.";
                return RedirectToAction("ReservarServicio");
            }

            // Escenario 3: fecha pasada
            if (DateTime.TryParse(reserva.Fecha, out var fechaParsed) &&
                fechaParsed.Date < DateTime.Now.Date)
            {
                TempData["Error"] = "No se puede reservar en una fecha pasada.";
                return RedirectToAction("ReservarServicio");
            }

            using var client = Cliente();
            var urlServicios = _configuration["Valores:UrlAPI"] + "Reserva/ConsultarServicios";
            var respServicios = client.GetAsync(urlServicios).Result;
            var servicios = respServicios.IsSuccessStatusCode
                ? respServicios.Content.ReadFromJsonAsync<List<ServicioModel>>().Result ?? new()
                : new List<ServicioModel>();

            var svc = servicios.FirstOrDefault(s => s.Id_Servicio == reserva.Id_Servicio);
            reserva.Id_Usuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;

            var body = new
            {
                reserva.Id_Usuario,
                reserva.Id_Servicio,
                reserva.Fecha,
                reserva.Hora,
                reserva.Direccion_Servicio,
                reserva.Observaciones,
                Subtotal = svc?.Precio_Base ?? 0
            };

            var url = _configuration["Valores:UrlAPI"] + "Reserva/CrearReserva";
            var resp = client.PostAsJsonAsync(url, body).Result;

            if (resp.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Tu reserva fue registrada exitosamente.";
                return RedirectToAction("MisReservas");
            }

            TempData["Error"] = "No se pudo registrar la reserva. Intenta de nuevo.";
            ViewBag.Servicios = servicios;
            return View(reserva);
        }

        #endregion

        #region Mis Reservas

        [Seguridad]
        public IActionResult MisReservas()
        {
            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            try
            {
                using var client = Cliente();
                client.Timeout = TimeSpan.FromSeconds(5);
                var url = _configuration["Valores:UrlAPI"] + $"Reserva/ObtenerReservaciones?idUsuario={idUsuario}";
                var resp = client.GetAsync(url).Result;

                if (resp.IsSuccessStatusCode)
                {
                    var reservas = resp.Content.ReadFromJsonAsync<List<ReservaModel>>().Result ?? new();
                    return View(reservas);
                }

                TempData["Error"] = "Error al cargar las reservas. Intente más tarde.";
                return View(new List<ReservaModel>());
            }
            catch
            {
                TempData["Error"] = "No se pudo conectar con el servidor. Intente más tarde.";
                return View(new List<ReservaModel>());
            }
        }

        #endregion

        #region Detalle Reserva

        [Seguridad]
        public IActionResult DetalleReserva(int id)
        {
            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            using var client = Cliente();
            var url = _configuration["Valores:UrlAPI"] +
                $"Reserva/ObtenerDetalle?idReservacion={id}&idUsuario={idUsuario}";
            var resp = client.GetAsync(url).Result;
            if (!resp.IsSuccessStatusCode) return RedirectToAction("MisReservas");
            var reserva = resp.Content.ReadFromJsonAsync<ReservaModel>().Result;
            if (reserva == null) return RedirectToAction("MisReservas");
            return View(reserva);
        }

        #endregion

        #region HU-C-006 Cancelar Reserva

        [Seguridad]
        [HttpPost]
        public IActionResult CancelarReserva(int id, string Motivo, string? Detalle)
        {
            if (string.IsNullOrWhiteSpace(Motivo))
            {
                TempData["Error"] = "Debe indicar el motivo de cancelación.";
                return RedirectToAction("DetalleReserva", new { id });
            }

            if (Motivo == "Otro" && string.IsNullOrWhiteSpace(Detalle))
            {
                TempData["Error"] = "Debe describir el motivo de cancelación.";
                return RedirectToAction("DetalleReserva", new { id });
            }

            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            using var client = Cliente();
            var url = _configuration["Valores:UrlAPI"] + "Reserva/Cancelar";
            var resp = client.PostAsJsonAsync(url, new
            {
                Id_Reservacion = id,
                Id_Usuario = idUsuario,
                Motivo,
                Detalle
            }).Result;

            if (resp.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "La reserva fue cancelada correctamente.";
                return RedirectToAction("MisReservas");
            }

            TempData["Error"] = "No se puede cancelar. La reserva ya fue completada o cancelada.";
            return RedirectToAction("DetalleReserva", new { id });
        }

        #endregion

        #region HU-C-007 Modificar Fecha

        [Seguridad]
        [HttpPost]
        public IActionResult ReprogramarReserva(int id, string nuevaFecha, string nuevaHora, string Motivo)
        {
            if (string.IsNullOrWhiteSpace(Motivo))
            {
                TempData["Error"] = "Debe indicar el motivo del cambio de fecha.";
                return RedirectToAction("DetalleReserva", new { id });
            }

            if (DateTime.TryParse(nuevaFecha, out var fechaParsed) &&
                fechaParsed.Date < DateTime.Now.Date)
            {
                TempData["Error"] = "No se puede reprogramar a una fecha pasada.";
                return RedirectToAction("DetalleReserva", new { id });
            }

            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            using var client = Cliente();
            var url = _configuration["Valores:UrlAPI"] + "Reserva/ModificarFecha";
            var resp = client.PostAsJsonAsync(url, new
            {
                Id_Reservacion = id,
                Id_Usuario = idUsuario,
                Nueva_Fecha = nuevaFecha,
                Nueva_Hora = nuevaHora,
                Motivo
            }).Result;

            if (resp.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "La fecha fue actualizada correctamente.";
                return RedirectToAction("DetalleReserva", new { id });
            }

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = "La reserva no existe.";
                return RedirectToAction("MisReservas");
            }

            TempData["Error"] = "Solo se pueden modificar reservas en estado Pendiente.";
            return RedirectToAction("DetalleReserva", new { id });
        }

        #endregion

        #region HU-C-004 Reseñas

        /// <summary>
        /// Lista las reservas Completadas del cliente, indicando si cada una
        /// ya tiene reseña o no, para poder calificarla desde un solo lugar.
        /// </summary>
        [Seguridad]
        [HttpGet]
        public IActionResult Resenas()
        {
            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            using var client = Cliente();

            var urlReservas = _configuration["Valores:UrlAPI"] + $"Reserva/ObtenerReservaciones?idUsuario={idUsuario}";
            var respReservas = client.GetAsync(urlReservas).Result;
            var reservas = respReservas.IsSuccessStatusCode
                ? respReservas.Content.ReadFromJsonAsync<List<ReservaModel>>().Result ?? new()
                : new List<ReservaModel>();

            var urlResenas = _configuration["Valores:UrlAPI"] + $"Resena/ConsultarPorUsuario?idUsuario={idUsuario}";
            var respResenas = client.GetAsync(urlResenas).Result;
            var resenas = respResenas.IsSuccessStatusCode
                ? respResenas.Content.ReadFromJsonAsync<List<ResenaUsuarioModel>>().Result ?? new()
                : new List<ResenaUsuarioModel>();

            var completadas = reservas
                .Where(r => r.Estado.Equals("Completada", StringComparison.OrdinalIgnoreCase))
                .Select(r =>
                {
                    var resena = resenas.FirstOrDefault(x => x.Id_Reservacion == r.Id_Reservacion);
                    return new ReservaConResenaModel
                    {
                        Id_Reservacion = r.Id_Reservacion,
                        Fecha = r.Fecha,
                        Direccion_Servicio = r.Direccion_Servicio,
                        Total = r.Total,
                        TieneResena = resena != null,
                        Calificacion = resena?.Calificacion,
                        Comentario = resena?.Comentario,
                        Respuesta_Admin = resena?.Respuesta_Admin
                    };
                })
                .OrderByDescending(r => r.Fecha)
                .ToList();

            return View(completadas);
        }

        /// <summary>
        /// HU-C-004 - Escenario 1 y 2: registra la reseña de una reserva
        /// Completada. La validación de campos vacíos y de que la reserva
        /// esté Completada vive en la API (RegistrarResena).
        /// </summary>
        [Seguridad]
        [HttpPost]
        public IActionResult AgregarResena(int idReservacion, int calificacion, string comentario)
        {
            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            using var client = Cliente();

            var url = _configuration["Valores:UrlAPI"] + "Resena/RegistrarResena";
            var resp = client.PostAsJsonAsync(url, new
            {
                Id_Reservacion = idReservacion,
                Id_Usuario = idUsuario,
                Calificacion = calificacion,
                Comentario = comentario
            }).Result;

            if (resp.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Tu reseña fue registrada. ¡Gracias por tu opinión!";
            }
            else
            {
                // Escenario 2: la API devuelve 400 con el motivo exacto
                // (comentario vacío, calificación fuera de rango, etc).
                var detalle = resp.Content.ReadAsStringAsync().Result;
                TempData["Error"] = string.IsNullOrWhiteSpace(detalle)
                    ? "No se pudo registrar la reseña."
                    : detalle.Trim('"');
            }

            return RedirectToAction("Resenas");
        }

        #endregion

        #region HU-C-003 Pagos PayPal

        [Seguridad]
        [HttpGet]
        public IActionResult PagarReserva(int id)
        {
            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            try
            {
                using var client = Cliente();
                client.Timeout = TimeSpan.FromSeconds(5);
                var url = _configuration["Valores:UrlAPI"] +
                    $"Reserva/ConsultarEstadoPago?idReservacion={id}&idUsuario={idUsuario}";
                var resp = client.GetAsync(url).Result;

                if (!resp.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No se encontró la reservación.";
                    return RedirectToAction("MisReservas");
                }

                var estado = resp.Content.ReadFromJsonAsync<EstadoPagoUIModel>().Result;

                // Escenario 2: ya está pagado
                if (estado?.Estado_Pago == "pagado")
                {
                    TempData["Error"] = "Esta reservación ya fue pagada anteriormente.";
                    return RedirectToAction("DetalleReserva", new { id });
                }

                ViewBag.IdReservacion = id;
                ViewBag.Total = estado?.Total ?? 0;
                return View(estado);
            }
            catch
            {
                TempData["Error"] = "No se pudo conectar con el servidor. Intente más tarde.";
                return RedirectToAction("MisReservas");
            }
        }

        [Seguridad]
        [HttpPost]
        public IActionResult IniciarPagoPayPal(int idReservacion, decimal monto)
        {
            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            try
            {
                using var client = Cliente();
                client.Timeout = TimeSpan.FromSeconds(10);
                var url = _configuration["Valores:UrlAPI"] + "Reserva/CrearOrdenPayPal";
                var resp = client.PostAsJsonAsync(url, new
                {
                    Id_Reservacion = idReservacion,
                    Id_Usuario = idUsuario,
                    Monto = monto,
                    OrderId = ""
                }).Result;

                if (!resp.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No se pudo iniciar el pago. Intente más tarde.";
                    return RedirectToAction("PagarReserva", new { id = idReservacion });
                }

                var resultado = resp.Content.ReadFromJsonAsync<PayPalOrdenUIModel>().Result;
                return Redirect(resultado?.ApprovalUrl ?? "/");
            }
            catch
            {
                TempData["Error"] = "Error de conexión al iniciar el pago. Intente más tarde.";
                return RedirectToAction("PagarReserva", new { id = idReservacion });
            }
        }

        [Seguridad]
        [HttpGet]
        public IActionResult PagoExitoso(int idReservacion, string token)
        {
            var idUsuario = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            try
            {
                using var client = Cliente();
                client.Timeout = TimeSpan.FromSeconds(10);

                // Obtener monto de la reservación
                var urlEstado = _configuration["Valores:UrlAPI"] +
                    $"Reserva/ConsultarEstadoPago?idReservacion={idReservacion}&idUsuario={idUsuario}";
                var respEstado = client.GetAsync(urlEstado).Result;
                var estado = respEstado.Content.ReadFromJsonAsync<EstadoPagoUIModel>().Result;

                // Capturar pago
                var url = _configuration["Valores:UrlAPI"] + "Reserva/CapturarPagoPayPal";
                var resp = client.PostAsJsonAsync(url, new
                {
                    Id_Reservacion = idReservacion,
                    Id_Usuario = idUsuario,
                    OrderId = token,
                    Monto = estado?.Total ?? 0
                }).Result;

                if (resp.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Pago procesado exitosamente! Tu reservación está confirmada.";
                    return RedirectToAction("DetalleReserva", new { id = idReservacion });
                }

                TempData["Error"] = "No se pudo confirmar el pago. Contacta soporte.";
                return RedirectToAction("DetalleReserva", new { id = idReservacion });
            }
            catch
            {
                TempData["Error"] = "Error al confirmar el pago. Intente más tarde.";
                return RedirectToAction("MisReservas");
            }
        }

        [Seguridad]
        [HttpGet]
        public IActionResult PagoCancelado(int idReservacion)
        {
            TempData["Error"] = "El pago fue cancelado. Puedes intentarlo nuevamente.";
            return RedirectToAction("PagarReserva", new { id = idReservacion });
        }

        #endregion
    }
}