using Microsoft.AspNetCore.Mvc;
using SGSWC.UI.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SGSWC.UI.Controllers
{
    public class Usuarios : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _configuration;

        public Usuarios(IHttpClientFactory http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        private HttpClient Cliente()
        {
            var client = _http.CreateClient();
            client.BaseAddress = new Uri(_configuration["Valores:UrlAPI"]);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                using var client = Cliente();
                var urlApi = "Reserva/ConsultarServicios";

                var respuesta = await client.GetAsync(urlApi);

                if (respuesta.IsSuccessStatusCode)
                {
                    var json = await respuesta.Content.ReadAsStringAsync();
                    var datos = JsonSerializer.Deserialize<List<ServicioModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return View(datos ?? new List<ServicioModel>());
                }
                ViewBag.Error = "No se pudieron cargar los servicios en este momento.";
                return View(new List<ServicioModel>());
            }
            catch (HttpRequestException e)
            {
                ViewBag.Error = "No se pudo conectar con el servidor. Intente más tarde.";
                return View(new List<ServicioModel>());
            }
            catch (Exception e)
            {
                ViewBag.Error = "Ocurrió un error al cargar los servicios.";
                return View(new List<ServicioModel>());
            }
        }
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                using var client = Cliente();
                var urlApi = $"Reserva/ConsultarServicio/{id}";

                var respuesta = await client.GetAsync(urlApi);

                if (respuesta.IsSuccessStatusCode)
                {
                    var json = await respuesta.Content.ReadAsStringAsync();
                    var servicio = JsonSerializer.Deserialize<ServicioModel>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (servicio != null)
                        return View(servicio);
                }

                return RedirectToAction("Service");
            }
            catch
            {
                return RedirectToAction("Service");
            }
        }

        public async Task<IActionResult> Service()
        {
            try
            {
                using var client = Cliente();
                var urlApi = "Reserva/ConsultarServicios";

                var respuesta = await client.GetAsync(urlApi);

                if (respuesta.IsSuccessStatusCode)
                {
                    var json = await respuesta.Content.ReadAsStringAsync();
                    var datos = JsonSerializer.Deserialize<List<ServicioModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return View(datos ?? new List<ServicioModel>());
                }

                ViewBag.Error = "No se pudieron cargar los servicios en este momento.";
                return View(new List<ServicioModel>());
            }
            catch
            {
                ViewBag.Error = "Ocurrió un error al cargar los servicios.";
                return View(new List<ServicioModel>());
            }
        }
    }
}