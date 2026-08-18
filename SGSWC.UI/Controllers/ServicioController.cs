using Microsoft.AspNetCore.Mvc;
using SGSWC.UI.Models;
using System.Net.Http.Headers;

namespace SGSWC.UI.Controllers
{
    [SeguridadRol(1, 2)]
    public class ServicioController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _configuration;

        public ServicioController(IHttpClientFactory http, IConfiguration configuration)
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

        [HttpGet]
        public IActionResult Index()
        {
            using var client = Cliente();

            var urlApi = _configuration["Valores:UrlAPI"] + "Servicio/ConsultarServiciosConEstado";

            var respuesta = client.GetAsync(urlApi).Result;

            if (respuesta.IsSuccessStatusCode)
            {
                var datos = respuesta.Content
                    .ReadFromJsonAsync<List<EstadoServicioModel>>().Result;
                return View(datos);
            }

            return View(new List<EstadoServicioModel>());
        }

        [HttpPost]
        public IActionResult CambiarEstadoServicio(int idReservacion, int idEstadoNuevo)
        {
            using var client = Cliente();

            var model = new CambiarEstadoConNotificacionModel
            {
                IdReservacion = idReservacion,
                IdEstadoNuevo = idEstadoNuevo,
                IdUsuarioAdmin = HttpContext.Session.GetInt32("Id_Usuario") ?? 0,
                Motivo = null
            };

            var urlApi = _configuration["Valores:UrlAPI"] + "Reserva/CambiarEstadoConNotificacion";

            var respuesta = client.PostAsJsonAsync(urlApi, model).Result;

            // HTTP 200 — éxito completo
            if (respuesta.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var resultado = respuesta.Content
                    .ReadFromJsonAsync<CambiarEstadoConNotificacionRespuestaModel>().Result;

                if (resultado?.EmailEnviado == true)
                    TempData["Mensaje"] = "Estado actualizado y notificación enviada al cliente.";
                else
                    TempData["Mensaje"] = "Estado actualizado. " + resultado?.MensajeNotificacion;
            }
            // HTTP 207 — estado actualizado pero email falló
            else if (respuesta.StatusCode == System.Net.HttpStatusCode.MultiStatus)
            {
                TempData["Advertencia"] = "Estado actualizado, pero no se pudo enviar el correo al cliente. El error quedó registrado.";
            }
            // HTTP 400 — transición no permitida
            else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                TempData["Error"] = "No se permite esa transición de estados.";
            }
            // HTTP 404 — reservación no existe
            else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = "La reservación no existe.";
            }
            else
            {
                TempData["Error"] = "No fue posible actualizar el estado del servicio.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Historial(int idReservacion)
        {
            using var client = Cliente();

            var urlApi = _configuration["Valores:UrlAPI"] +
                $"Servicio/ConsultarHistorialEstado?idReservacion={idReservacion}";

            var respuesta = client.GetAsync(urlApi).Result;

            if (respuesta.IsSuccessStatusCode)
            {
                var datos = respuesta.Content
                    .ReadFromJsonAsync<List<HistorialEstadoServicioModel>>().Result;
                return View(datos);
            }

            return View(new List<HistorialEstadoServicioModel>());
        }
    }
}