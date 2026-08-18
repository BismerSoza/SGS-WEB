using Microsoft.AspNetCore.Mvc;
using SGSWC.UI.Models;
using System.Net.Http.Headers;

namespace SGSWC.UI.Controllers
{
    [SeguridadRol(1, 2)]
    public class UsersController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _configuration;

        public UsersController(IHttpClientFactory http, IConfiguration configuration)
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

        #region HU-SA-005 Gestión de Usuarios

        [HttpGet]
        public IActionResult Index()
        {
            using var client = _http.CreateClient();
            var urlApi = _configuration["Valores:UrlAPI"] + "Users/ConsultarUsuarios";
            var respuesta = client.GetAsync(urlApi).Result;
            var usuarios = new List<GestionUsuarioModel>();
            if (respuesta.IsSuccessStatusCode)
                usuarios = respuesta.Content.ReadFromJsonAsync<List<GestionUsuarioModel>>().Result ?? new();
            if (TempData["Mensaje"] != null) ViewBag.Mensaje = TempData["Mensaje"];
            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            return View(usuarios);
        }

        [HttpPost]
        public IActionResult AsignarRol(int IdUsuario, int Rol)
        {
            if (Rol == 0)
            {
                TempData["Error"] = "Rol inválido.";
                return RedirectToAction("Index");
            }
            using var client = _http.CreateClient();
            var urlApi = _configuration["Valores:UrlAPI"] + "Users/ActualizarRol";
            var payload = new { IdUsuario, Id_Rol = Rol };
            var respuesta = client.PostAsJsonAsync(urlApi, payload).Result;
            if (respuesta.IsSuccessStatusCode)
            {
                var resultado = respuesta.Content.ReadFromJsonAsync<int>().Result;
                if (resultado == 1) { TempData["Mensaje"] = "Rol asignado correctamente."; return RedirectToAction("Index"); }
                if (resultado == 2) { TempData["Error"] = "Rol inválido."; return RedirectToAction("Index"); }
            }
            TempData["Error"] = "No se pudo actualizar el rol. Intente más tarde.";
            return RedirectToAction("Index");
        }

        #endregion

        #region HU-SA-006 Historial de Accesos

        [HttpGet]
        public IActionResult AccessLog(string? fechaInicio, string? fechaFin)
        {
            using var client = Cliente();
            var url = _configuration["Valores:UrlAPI"] +
                $"Users/HistorialAccesos?fechaInicio={fechaInicio}&fechaFin={fechaFin}";
            var respuesta = client.GetAsync(url).Result;
            var historial = new List<HistorialAccesoUIModel>();
            if (respuesta.IsSuccessStatusCode)
                historial = respuesta.Content.ReadFromJsonAsync<List<HistorialAccesoUIModel>>().Result ?? new();
            else
                ViewBag.Error = "No se pudo cargar el historial de accesos.";
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;
            return View(historial);
        }

        #endregion

        #region HU-RE-003 Comparar Servicios

        [HttpGet]
        public IActionResult CompararServicios()
        {
            using var client = Cliente();
            var url = _configuration["Valores:UrlAPI"] + "Users/CompararServicios";
            var respuesta = client.GetAsync(url).Result;
            var servicios = new List<ServicioComparadoUIModel>();
            if (respuesta.IsSuccessStatusCode)
                servicios = respuesta.Content.ReadFromJsonAsync<List<ServicioComparadoUIModel>>().Result ?? new();
            else
                ViewBag.Error = "No se pudo cargar los datos. Intente más tarde.";
            return View(servicios);
        }

        #endregion



    }
}