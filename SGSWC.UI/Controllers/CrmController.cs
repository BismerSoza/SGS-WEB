using Microsoft.AspNetCore.Mvc;
using SGSWC.UI.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SGSWC.UI.Controllers
{
    [SeguridadRol(1, 2)]
    public class CrmController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _configuration;

        public CrmController(IHttpClientFactory http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        #region Dashboard - Index

        [Seguridad]
        [HttpGet]
        public IActionResult Index()
        {
            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var urlDashboard = _configuration["Valores:UrlAPI"] + "CRM/ConsultarDashboard";
            var respuestaDashboard = context.GetAsync(urlDashboard).Result;

            var modelo = respuestaDashboard.IsSuccessStatusCode
                ? respuestaDashboard.Content.ReadFromJsonAsync<DashboardModel>().Result
                  ?? new DashboardModel { ServicioTop = "N/A" }
                : new DashboardModel { ServicioTop = "N/A" };

            var urlPorMes = _configuration["Valores:UrlAPI"] + "CRM/ConsultarServiciosPorMes";
            var respuestaPorMes = context.GetAsync(urlPorMes).Result;

            var porMes = new int[12];
            if (respuestaPorMes.IsSuccessStatusCode)
            {
                var datos = respuestaPorMes.Content
                    .ReadFromJsonAsync<List<ServiciosPorMesModel>>().Result
                    ?? new List<ServiciosPorMesModel>();

                foreach (var item in datos)
                    porMes[item.Mes - 1] = item.Total;
            }
            ViewBag.ServiciosPorMes = System.Text.Json.JsonSerializer.Serialize(porMes);

            return View(modelo);
        }

        [HttpGet]
        public IActionResult ConsultarServiciosPorMes(int? anio = null, int? mes = null)
        {
            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] +
                      $"CRM/ConsultarServiciosPorMes?anio={anio}&mes={mes}";

            var respuesta = client.GetAsync(url).Result;

            if (!respuesta.IsSuccessStatusCode)
                return Json(new int[12]);

            var datos = respuesta.Content
                .ReadFromJsonAsync<List<ServiciosPorMesModel>>().Result
                ?? new List<ServiciosPorMesModel>();

            var porMes = new int[12];
            foreach (var item in datos)
                porMes[item.Mes - 1] = item.Total;

            return Json(porMes);
        }

        #endregion

        #region Gestión de Usuarios

        [Seguridad]
        [HttpGet]
        public IActionResult GestionUsuarios()
        {
            var usuarios = ObtenerUsuarios();
            return View(usuarios);
        }

        [Seguridad]
        [HttpPost]
        public IActionResult CambiarEstadoUsuario([FromBody] GestionUsuarioModel request)
        {
            using var context = _http.CreateClient();
            var urlApi = _configuration["Valores:UrlAPI"] + "Usuario/CambiarEstadoUsuario";

            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var respuesta = context.PutAsJsonAsync(urlApi, new
            {
                Id_Usuario = request.Id_Usuario,
                Activo = request.Activo,
                Id_Usuario_Sesion = HttpContext.Session.GetInt32("Id_Usuario") ?? 0
            }).Result;

            if (respuesta.IsSuccessStatusCode)
                return Json(new { exito = true });

            return Json(new { exito = false });
        }

        #endregion

        #region Métodos Privados

        private List<GestionUsuarioModel> ObtenerUsuarios()
        {
            using var context = _http.CreateClient();
            var idSesion = HttpContext.Session.GetInt32("Id_Usuario") ?? 0;
            var urlApi = _configuration["Valores:UrlAPI"] +
                           $"Usuario/ConsultarUsuarios?id_usuario_sesion={idSesion}";

            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var respuesta = context.GetAsync(urlApi).Result;

            if (respuesta.IsSuccessStatusCode)
            {
                return respuesta.Content
                    .ReadFromJsonAsync<List<GestionUsuarioModel>>().Result
                    ?? new List<GestionUsuarioModel>();
            }

            return new List<GestionUsuarioModel>();
        }



        #endregion
        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [Seguridad]
        [HttpGet]
        public IActionResult Calendario(int? anio, int? mes)
        {
            var anioConsulta = anio ?? DateTime.Now.Year;
            var mesConsulta = mes ?? DateTime.Now.Month;

            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] +
                      $"CRM/ConsultarReservacionesPorMes?anio={anioConsulta}&mes={mesConsulta}";

            var respuesta = context.GetAsync(url).Result;

            var servicios = respuesta.IsSuccessStatusCode
                ? respuesta.Content
                      .ReadFromJsonAsync<List<CalendarioReservaModel>>().Result
                      ?? new List<CalendarioReservaModel>()
                : new List<CalendarioReservaModel>();

            ViewBag.ServiciosJson = System.Text.Json.JsonSerializer.Serialize(servicios);
            ViewBag.AnioActual = anioConsulta;
            ViewBag.MesActual = mesConsulta;

            return View();
        }

        #region Calendario (HU-GC-001)

        [Seguridad]
        [HttpGet]
        public IActionResult ObtenerEmpleadosConDisponibilidad(
            string fecha, string hora, int duracionMin = 60, int? idReservacion = null)
        {
            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] +
                      $"CRM/ObtenerEmpleadosConDisponibilidad" +
                      $"?fecha={fecha}&hora={hora}&duracionMin={duracionMin}" +
                      (idReservacion.HasValue ? $"&idReservacion={idReservacion}" : "");

            var respuesta = context.GetAsync(url).Result;

            if (!respuesta.IsSuccessStatusCode)
                return Json(new List<object>());

            var datos = respuesta.Content
                .ReadFromJsonAsync<List<EmpleadoDisponibilidadModel>>().Result
                ?? new List<EmpleadoDisponibilidadModel>();

            return Json(datos);
        }

        [Seguridad]
        [HttpPost]
        public IActionResult AsignarEmpleado([FromBody] AsignarEmpleadoRequest request)
        {
            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] + "CRM/AsignarEmpleado";

            var respuesta = context.PostAsJsonAsync(url, request).Result;

            if (!respuesta.IsSuccessStatusCode)
                return Json(new { exito = false, mensaje = "Error al comunicarse con la API." });

            var resultado = respuesta.Content
                .ReadFromJsonAsync<AsignarEmpleadoResultado>().Result;

            return Json(new { exito = true, mensaje = resultado?.Mensaje ?? "Operación completada." });
        }

        #endregion

        [HttpGet]
        public IActionResult DetalleServicio()
        {
            return View();
        }

        #region HU-GC-004 Perfil de Cliente

        [Seguridad]
        [HttpGet]
        public IActionResult PerfilCliente(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Cliente no válido.";
                return RedirectToAction("GestionUsuarios");
            }

            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var urlApi = _configuration["Valores:UrlAPI"];

            var respCliente = context.GetAsync($"{urlApi}CRM/ConsultarClientePorId?idUsuario={id}").Result;
            if (!respCliente.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se encontró el cliente solicitado.";
                return RedirectToAction("GestionUsuarios");
            }
            var cliente = respCliente.Content.ReadFromJsonAsync<ClienteDetalleModel>(options).Result
                          ?? new ClienteDetalleModel();

            var respFrecuente = context.GetAsync($"{urlApi}CRM/ValidarClienteFrecuente?idUsuario={id}").Result;
            var estadoFrecuente = respFrecuente.IsSuccessStatusCode
                ? respFrecuente.Content.ReadFromJsonAsync<ClienteFrecuenteEstadoModel>(options).Result
                  ?? new ClienteFrecuenteEstadoModel { Id_Usuario = id }
                : new ClienteFrecuenteEstadoModel { Id_Usuario = id };

            var respHistorial = context.GetAsync($"{urlApi}CRM/ConsultarHistorialServiciosCliente?idUsuario={id}").Result;
            var historial = respHistorial.IsSuccessStatusCode
                ? respHistorial.Content.ReadFromJsonAsync<List<HistorialServicioClienteModel>>(options).Result
                  ?? new List<HistorialServicioClienteModel>()
                : new List<HistorialServicioClienteModel>();

            var modelo = new PerfilClienteViewModel
            {
                Cliente = cliente,
                EstadoFrecuente = estadoFrecuente,
                Historial = historial
            };

            return View(modelo);
        }

        #endregion

        #region HU-RE-009 Servicios Pendientes

        [Seguridad]
        [HttpGet]
        public IActionResult ServiciosPendientes()
        {
            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] + "CRM/ConsultarServiciosPendientes";
            var respuesta = client.GetAsync(url).Result;

            var servicios = new List<ServicioPendienteUIModel>();

            if (respuesta.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                servicios = respuesta.Content
                    .ReadFromJsonAsync<List<ServicioPendienteUIModel>>(options).Result
                    ?? new List<ServicioPendienteUIModel>();
            }
            else
            {
                ViewBag.Error = "No se pudo generar el reporte. Intente más tarde.";
            }

            return View(servicios);
        }

        [Seguridad]
        [HttpGet]
        public async Task<IActionResult> ExportarServiciosPendientes(string formato)
        {
            if (string.IsNullOrEmpty(formato))
                return RedirectToAction("ServiciosPendientes");

            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var endpoint = formato.ToLower() == "pdf"
                ? "CRM/ExportarServiciosPendientesPDF"
                : "CRM/ExportarServiciosPendientesExcel";

            var url = _configuration["Valores:UrlAPI"] + endpoint;

            try
            {
                var respuesta = await client.GetAsync(url);

                if (!respuesta.IsSuccessStatusCode)
                {
                    string errorMsg = "No hay datos disponibles para exportar.";
                    try
                    {
                        var errorContent = await respuesta.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(errorContent))
                        {
                            var errorObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(errorContent);
                            if (errorObj != null && errorObj.ContainsKey("mensaje"))
                                errorMsg = errorObj["mensaje"];
                        }
                    }
                    catch { }

                    TempData["Error"] = errorMsg;
                    return RedirectToAction("ServiciosPendientes");
                }
                var stream = await respuesta.Content.ReadAsStreamAsync();
                var contentType = respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var fileName = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                            ?? respuesta.Content.Headers.ContentDisposition?.FileName
                            ?? $"Servicios_Pendientes_{DateTime.Now:yyyyMMdd}.{formato.ToLower()}";

                fileName = fileName.Trim('"').Replace(" ", "_");

                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al generar el reporte. Intente nuevamente.";
                return RedirectToAction("ServiciosPendientes");
            }
        }

        #endregion

        #region HU-RE-004 - Reportes
        [Seguridad]
        [HttpGet]
        public IActionResult Reportes()
        {
            return View();
        }
        [Seguridad]
        [HttpGet]
        public IActionResult ExportarReporte(string formato, string? fechaDesde, string? fechaHasta)
        {
            if (string.IsNullOrEmpty(formato))
                return RedirectToAction("Reportes");
            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));
            var endpoint = formato.ToLower() == "pdf"
                ? "CRM/ExportarReportePDF"
                : "CRM/ExportarReporteExcel";
            var url = _configuration["Valores:UrlAPI"] + endpoint;
            var parametros = new List<string>();
            if (!string.IsNullOrEmpty(fechaDesde)) parametros.Add($"fechaDesde={fechaDesde}");
            if (!string.IsNullOrEmpty(fechaHasta)) parametros.Add($"fechaHasta={fechaHasta}");
            if (parametros.Any()) url += "?" + string.Join("&", parametros);
            var respuesta = client.GetAsync(url).Result;
            if (!respuesta.IsSuccessStatusCode)
            {
                TempData["ErrorReporte"] = "No hay datos para exportar en el rango seleccionado.";
                return RedirectToAction("Reportes");
            }
            var bytes = respuesta.Content.ReadAsByteArrayAsync().Result;
            var contentType = respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                        ?? respuesta.Content.Headers.ContentDisposition?.FileName
                        ?? $"Reporte_Servicios_{DateTime.Now:yyyyMMdd}.{formato.ToLower()}";
            return File(bytes, contentType, fileName.Trim('"'));
        }
        #endregion

        #region HU-RE-008 Clientes Frecuentes

        [Seguridad]
        [HttpGet]
        public IActionResult ClientesFrecuentes(string? fechaDesde = null, string? fechaHasta = null, bool generado = false)
        {
            var clientes = new List<ClienteFrecuenteUIModel>();

            if (generado)
            {
                try
                {
                    using var client = _http.CreateClient();
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("Token"));
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var parametros = new List<string> { "generado=true" };
                    if (!string.IsNullOrEmpty(fechaDesde)) parametros.Add($"fechaDesde={fechaDesde}");
                    if (!string.IsNullOrEmpty(fechaHasta)) parametros.Add($"fechaHasta={fechaHasta}");

                    var url = _configuration["Valores:UrlAPI"] + "CRM/ClientesFrecuentes?" + string.Join("&", parametros);
                    var respuesta = client.GetAsync(url).Result;

                    if (respuesta.IsSuccessStatusCode)
                        clientes = respuesta.Content.ReadFromJsonAsync<List<ClienteFrecuenteUIModel>>().Result ?? new();
                    else
                        ViewBag.Error = "No se pudo cargar el reporte. Intente más tarde.";
                }
                catch
                {
                    ViewBag.Error = "No se pudo establecer conexión con el servidor. Intente más tarde.";
                }
            }

            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            ViewBag.Generado = generado;
            return View(clientes);
        }

        #endregion

        #region HU-RE-012 Horarios Más Solicitados

        [Seguridad]
        [HttpGet]
        public IActionResult HorariosSolicitados(string? fechaDesde = null, string? fechaHasta = null, bool generado = false)
        {
            var horarios = new List<HorarioSolicitadoUIModel>();

            if (generado)
            {
                try
                {
                    using var client = _http.CreateClient();
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("Token"));
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var parametros = new List<string> { "generado=true" };
                    if (!string.IsNullOrEmpty(fechaDesde)) parametros.Add($"fechaDesde={fechaDesde}");
                    if (!string.IsNullOrEmpty(fechaHasta)) parametros.Add($"fechaHasta={fechaHasta}");

                    var url = _configuration["Valores:UrlAPI"] + "CRM/HorariosMasSolicitados?" + string.Join("&", parametros);
                    var respuesta = client.GetAsync(url).Result;

                    if (respuesta.IsSuccessStatusCode)
                        horarios = respuesta.Content.ReadFromJsonAsync<List<HorarioSolicitadoUIModel>>().Result ?? new();
                    else
                        ViewBag.Error = "No se pudo cargar el reporte. Intente más tarde.";
                }
                catch
                {
                    ViewBag.Error = "No se pudo establecer conexión con el servidor. Intente más tarde.";
                }
            }

            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            ViewBag.Generado = generado;
            return View(horarios);
        }

        #endregion

        #region HU-RE-006 Reporte Personalizado

        [Seguridad]
        [HttpGet]
        public async Task<IActionResult> ReportePersonalizado(string? fechaDesde, string? fechaHasta, string? estadoPago, int? idEstado, bool generado = false)
        {
            try
            {
                using var client = _http.CreateClient();
                var token = HttpContext.Session.GetString("Token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                // CORREGIDO: La ruta correcta es CRM/ReportePersonalizado
                var urlApi = _configuration["Valores:UrlAPI"] + "CRM/ReportePersonalizado";
                var parametros = new List<string>();

                if (!string.IsNullOrEmpty(fechaDesde))
                    parametros.Add($"fechaDesde={fechaDesde}");
                if (!string.IsNullOrEmpty(fechaHasta))
                    parametros.Add($"fechaHasta={fechaHasta}");
                if (!string.IsNullOrEmpty(estadoPago))
                    parametros.Add($"estadoPago={estadoPago}");
                if (idEstado.HasValue && idEstado.Value > 0)
                    parametros.Add($"idEstado={idEstado.Value}");

                if (parametros.Any())
                    urlApi += "?" + string.Join("&", parametros);

                ViewBag.FechaDesde = fechaDesde;
                ViewBag.FechaHasta = fechaHasta;
                ViewBag.EstadoPago = estadoPago;
                ViewBag.IdEstado = idEstado;
                ViewBag.Generado = generado;

                var respuesta = await client.GetAsync(urlApi);

                if (respuesta.IsSuccessStatusCode)
                {
                    var json = await respuesta.Content.ReadAsStringAsync();
                    var datos = JsonSerializer.Deserialize<List<ReporteServiciosModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (datos != null && datos.Any())
                    {
                        return View(datos);
                    }

                    ViewBag.Error = "No se encontraron servicios con los filtros seleccionados.";
                    return View(new List<ReporteServiciosModel>());
                }
                else
                {
                    ViewBag.Error = "No se pudo cargar el reporte en este momento. Por favor, intente más tarde.";
                    return View(new List<ReporteServiciosModel>());
                }
            }
            catch (Exception)
            {
                ViewBag.Error = "No se pudo conectar con el servidor. Por favor, intente más tarde.";
                ViewBag.Generado = generado;
                return View(new List<ReporteServiciosModel>());
            }
        }

        #endregion

        #region HU-RE-005 Métricas de Desempeño

        [Seguridad]
        [HttpGet]
        public IActionResult Rendimiento(int? anio = null, int? mes = null)
        {
            var anioConsulta = anio ?? DateTime.Now.Year;
            var mesConsulta = mes ?? DateTime.Now.Month;

            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] +
                      $"CRM/ConsultarMetricasDesempeno?anio={anioConsulta}&mes={mesConsulta}";

            var respuesta = client.GetAsync(url).Result;

            var modelo = new MetricasDesempenoModel { DatosDisponibles = false };

            if (respuesta.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                modelo = respuesta.Content.ReadFromJsonAsync<MetricasDesempenoModel>(options).Result
                    ?? new MetricasDesempenoModel { DatosDisponibles = false };
            }
            else
            {
                ViewBag.Error = "No se pudieron calcular las métricas. Intente más tarde.";
            }

            ViewBag.AnioConsulta = anioConsulta;
            ViewBag.MesConsulta = mesConsulta;

            return View(modelo);
        }

        #endregion

        #region HU-GC-005 Analizar Historial de Cliente

        [Seguridad]
        [HttpGet]
        public IActionResult AnalizarHistorialCliente(int idUsuario)
        {
            if (idUsuario <= 0)
                return Json(new { datosSuficientes = false, mensaje = "Cliente no válido." });

            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] +
                      $"CRM/AnalizarHistorialCliente?idUsuario={idUsuario}";

            var respuesta = context.GetAsync(url).Result;

            if (!respuesta.IsSuccessStatusCode)
                return Json(new { datosSuficientes = false, mensaje = "No se pudo generar el análisis. Intente más tarde." });

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resultado = respuesta.Content
                .ReadFromJsonAsync<AnalisisHistorialClienteModel>(options).Result
                ?? new AnalisisHistorialClienteModel { DatosSuficientes = false, Mensaje = "No hay suficientes datos para análisis" };

            return Json(resultado);
        }

        #endregion

        #region HU-RE-001 Reporte Mensual

        [Seguridad]
        [HttpGet]
        public IActionResult ExportarReporteMensual(string formato, int anio, int mes)
        {
            if (string.IsNullOrEmpty(formato) || anio <= 0 || mes < 1 || mes > 12)
            {
                TempData["ErrorReporte"] = "Debe seleccionar un período y un formato válidos.";
                return RedirectToAction("Reportes");
            }

            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var endpoint = formato.ToLower() == "pdf"
                ? "CRM/ExportarReporteMensualPDF"
                : "CRM/ExportarReporteMensualExcel";

            var url = _configuration["Valores:UrlAPI"] + $"{endpoint}?anio={anio}&mes={mes}";

            var respuesta = client.GetAsync(url).Result;

            if (!respuesta.IsSuccessStatusCode)
            {
                TempData["ErrorReporte"] = "No hay datos disponibles para el mes seleccionado.";
                return RedirectToAction("Reportes");
            }

            var bytes = respuesta.Content.ReadAsByteArrayAsync().Result;
            var contentType = respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                        ?? respuesta.Content.Headers.ContentDisposition?.FileName
                        ?? $"Reporte_Mensual_{anio}_{mes:D2}.{formato.ToLower()}";

            return File(bytes, contentType, fileName.Trim('"'));
        }

        #endregion

        #region HU-RE-011 - Reporte Clientes Nuevos
        [Seguridad]
        [HttpGet]
        public IActionResult ReporteClientesNuevos(string? fechaInicio, string? fechaFin)
        {
            var inicio = fechaInicio ?? DateTime.Now.ToString("yyyy-MM-01");
            var fin = fechaFin ?? DateTime.Now.ToString("yyyy-MM-dd");

            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var urlApi = _configuration["Valores:UrlAPI"] +
                $"crm/ReporteClientesNuevos?fechaInicio={inicio}&fechaFin={fin}";

            try
            {
                var respuesta = client.GetAsync(urlApi).Result;
                var modelo = new ReporteClientesNuevosResponseModel();

                if (respuesta.IsSuccessStatusCode)
                {
                    modelo = respuesta.Content
                        .ReadFromJsonAsync<ReporteClientesNuevosResponseModel>().Result
                        ?? new();
                }
                else
                {
                    ViewBag.Error = "No se pudo procesar la información. Intente más tarde.";
                }

                ViewBag.FechaInicio = inicio;
                ViewBag.FechaFin = fin;
                return View(modelo);
            }
            catch (Exception)
            {
                ViewBag.Error = "No se pudo procesar la información. Intente más tarde.";
                ViewBag.FechaInicio = inicio;
                ViewBag.FechaFin = fin;
                return View(new ReporteClientesNuevosResponseModel());
            }
        }
        #endregion


        #region HU-CR-002 - Monitoreo de Rendimiento del Sistema
        [Seguridad] 
        [HttpGet]
        public IActionResult Monitoreo()
        {
            return View();
        }

        [Seguridad]
        [HttpGet]
        public IActionResult ObtenerEstadoSistema()
        {
            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] + "CRM/EstadoSistema";

            var respuesta = client.GetAsync(url).Result;

            if (!respuesta.IsSuccessStatusCode)
                return StatusCode(500, new { mensaje = "No se pudo obtener el estado del sistema." });

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var modelo = respuesta.Content.ReadFromJsonAsync<EstadoSistemaModel>(options).Result;

            return Json(modelo);
        }
        #endregion

        #region HU-RE-007 - Reporte Financiero (Ingresos y Egresos)

        [Seguridad]
        [HttpGet]
        public IActionResult ReporteFinanciero()
        {
            return View();
        }

        [Seguridad]
        [HttpGet]
        public IActionResult ExportarReporteFinanciero(string formato, string? fechaDesde, string? fechaHasta, bool simularError = false)
        {
            if (string.IsNullOrEmpty(formato))
            {
                TempData["ErrorReporteFinanciero"] = "Debe seleccionar un formato válido.";
                return RedirectToAction("ReporteFinanciero");
            }

            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var endpoint = formato.ToLower() == "pdf"
                ? "CRM/ExportarReporteFinancieroPDF"
                : "CRM/ExportarReporteFinancieroExcel";

            var url = _configuration["Valores:UrlAPI"] + endpoint;
            var parametros = new List<string>();
            if (!string.IsNullOrEmpty(fechaDesde)) parametros.Add($"fechaDesde={fechaDesde}");
            if (!string.IsNullOrEmpty(fechaHasta)) parametros.Add($"fechaHasta={fechaHasta}");
            if (simularError)
                parametros.Add("simularError=true");
            if (parametros.Any()) url += "?" + string.Join("&", parametros);

            HttpResponseMessage respuesta;
            try
            {
                respuesta = client.GetAsync(url).Result;
            }
            catch (Exception)
            {
                // Escenario 3: Validar error de procesamiento
                TempData["ErrorReporteFinanciero"] = "Ocurrió un error al procesar el reporte. Intente nuevamente.";
                return RedirectToAction("ReporteFinanciero");
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                // Escenario 2: Validar ausencia de registros (o Escenario 3 si el error es del servidor)
                TempData["ErrorReporteFinanciero"] = respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? "No hay datos disponibles"
                    : "Ocurrió un error al procesar el reporte. Intente nuevamente.";
                return RedirectToAction("ReporteFinanciero");
            }

            var bytes = respuesta.Content.ReadAsByteArrayAsync().Result;
            var contentType = respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                        ?? respuesta.Content.Headers.ContentDisposition?.FileName
                        ?? $"Reporte_Financiero_{DateTime.Now:yyyyMMdd}.{formato.ToLower()}";

            return File(bytes, contentType, fileName.Trim('"'));
        }

        [Seguridad]
        public IActionResult ProbarErrorReporte()
        {
            return ExportarReporteFinanciero(
                "pdf",
                null,
                null,
                true);
        }

        #region HU-RE-013 - Reporte Calificaciones Bajas
        [Seguridad]
        [HttpGet]
        public IActionResult ReporteCalificacionesBajas(
        string? fechaInicio, string? fechaFin, int umbral = 3)
        {
            var inicio = fechaInicio ?? DateTime.Now.ToString("yyyy-MM-01");
            var fin = fechaFin ?? DateTime.Now.ToString("yyyy-MM-dd");
            var modelo = new ReporteCalificacionesBajasResponseModel();

            try
            {
                using var client = _http.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        HttpContext.Session.GetString("Token"));

                var urlApi = _configuration["Valores:UrlAPI"] +
                    $"crm/ReporteCalificacionesBajas?fechaInicio={inicio}&fechaFin={fin}&umbral={umbral}";

                var respuesta = client.GetAsync(urlApi).Result;

                if (respuesta.IsSuccessStatusCode)
                    modelo = respuesta.Content
                        .ReadFromJsonAsync<ReporteCalificacionesBajasResponseModel>().Result
                        ?? new();
                else
                    ViewBag.Error = "No se pudo procesar la información. Intente más tarde.";
            }
            catch (Exception)
            {
                ViewBag.Error = "No se pudo procesar la información. Intente más tarde.";
            }

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            ViewBag.Umbral = umbral;
            return View(modelo);
        }
        #endregion

        #region HU-RE-002 - Reporte Estadísticas de Satisfacción
        [Seguridad]
        [HttpGet]
        public IActionResult ReporteEstadisticasSatisfaccion(
    string? fechaInicio, string? fechaFin)
        {
            var inicio = fechaInicio ?? DateTime.Now.ToString("yyyy-MM-01");
            var fin = fechaFin ?? DateTime.Now.ToString("yyyy-MM-dd");
            var modelo = new ReporteEstadisticasSatisfaccionResponseModel();

            try
            {
                using var client = _http.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer",
                        HttpContext.Session.GetString("Token"));

                var urlApi = _configuration["Valores:UrlAPI"] +
                    $"crm/ReporteEstadisticasSatisfaccion?fechaInicio={inicio}&fechaFin={fin}";

                var respuesta = client.GetAsync(urlApi).Result;

                if (respuesta.IsSuccessStatusCode)
                    modelo = respuesta.Content
                        .ReadFromJsonAsync<ReporteEstadisticasSatisfaccionResponseModel>().Result
                        ?? new();
                else
                    ViewBag.Error = "No se pudo cargar la información. Intente más tarde.";
            }
            catch (Exception)
            {
                ViewBag.Error = "No se pudo cargar la información. Intente más tarde.";
            }

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            return View(modelo);
        }
        #endregion


        #region HU-PG-001 / HU-PG-002 Gestión de Pagos

        [Seguridad]
        [HttpGet]
        public IActionResult GestionPagos()
        {
            var pagos = ObtenerPagos();
            return View(pagos);
        }

        private List<PagoUIModel> ObtenerPagos()
        {
            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] + "CRM/ConsultarPagos";
            var respuesta = context.GetAsync(url).Result;

            if (!respuesta.IsSuccessStatusCode)
                return new List<PagoUIModel>();

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return respuesta.Content
                .ReadFromJsonAsync<List<PagoUIModel>>(options).Result
                ?? new List<PagoUIModel>();
        }
        [Seguridad]
        [HttpPost]
        public IActionResult CambiarEstadoPago([FromBody] CambiarEstadoPagoRequest request)
        {
            using var context = _http.CreateClient();
            context.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var url = _configuration["Valores:UrlAPI"] + "CRM/CambiarEstadoPago";
            var respuesta = context.PutAsJsonAsync(url, request).Result;

            if (respuesta.StatusCode == System.Net.HttpStatusCode.Conflict)
                return Json(new { exito = false, bloqueado = true, mensaje = "Este servicio ya se encuentra pagado." });

            if (!respuesta.IsSuccessStatusCode)
                return Json(new { exito = false, mensaje = "No se pudo actualizar el estado de pago." });

            return Json(new { exito = true, mensaje = "Estado de pago actualizado correctamente." });
        }
        [Seguridad]
        [HttpGet]
        public IActionResult ExportarEstadoPagos(string formato)
        {
            if (string.IsNullOrEmpty(formato))
                return RedirectToAction("GestionPagos");

            using var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    HttpContext.Session.GetString("Token"));

            var endpoint = formato.ToLower() == "pdf"
                ? "CRM/ExportarEstadoPagosPDF"
                : "CRM/ExportarEstadoPagosExcel";

            var url = _configuration["Valores:UrlAPI"] + endpoint;

            try
            {
                var respuesta = client.GetAsync(url).Result;

                if (!respuesta.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No hay datos para exportar.";
                    return RedirectToAction("GestionPagos");
                }

                var bytes = respuesta.Content.ReadAsByteArrayAsync().Result;
                var contentType = respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var fileName = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                            ?? respuesta.Content.Headers.ContentDisposition?.FileName
                            ?? $"Estado_Pagos_{DateTime.Now:yyyyMMdd}.{formato.ToLower()}";

                return File(bytes, contentType, fileName.Trim('"'));
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al generar el reporte.";
                return RedirectToAction("GestionPagos");
            }
        }

        #endregion
    }
}
    #endregion