using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using SGSWC.UI.Models;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Utiles;

namespace SGSWC.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _configuration;
        private readonly Helper _helper = new Helper();
        public HomeController(IHttpClientFactory http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        #region Apartado Validar Sesión

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(UsuarioModel usuario)
        {
            var helper = new Helper();
            usuario.Contrasena_hash = helper.Encrypt(usuario.Contrasena_hash);
            using (var context = _http.CreateClient())
            {
                var urlApi = _configuration["Valores:UrlAPI"] + "Home/ValidarSesion";
                var respuesta = context.PostAsJsonAsync(urlApi, usuario).Result;

                if (respuesta.IsSuccessStatusCode)
                {
                    var datosApi = respuesta.Content.ReadFromJsonAsync<UsuarioModel>().Result;

                    if (datosApi != null)
                    {
                        HttpContext.Session.SetInt32("Id_Usuario", datosApi.Id_Usuario);
                        HttpContext.Session.SetString("NombreUsuario", datosApi.Nombre);
                        HttpContext.Session.SetString("NombreRol", datosApi.NombreRol);
                        HttpContext.Session.SetInt32("Id_Rol", datosApi.Id_Rol);
                        HttpContext.Session.SetString("Token", datosApi.Token);
                        HttpContext.Session.SetString("CorreoUsuario", usuario.Correo);

                        //guardar el flag
                        HttpContext.Session.SetInt32("DebeCambiarContrasena", datosApi.DebeCambiarContrasena ? 1 : 0);

                        if (datosApi.Id_Rol == 1)
                            return RedirectToAction("Index", "CRM");
                        else
                            return RedirectToAction("Index", "Inicio");
                    }
                }

                ViewBag.Mensaje = "Correo o contraseña incorrectos. Verifique sus datos e intente de nuevo.";
                return View();
            }
        }

        #endregion

        #region HU-SA-001 Registrar Usuario

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registro(UsuarioModel usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Contrasena_hash) ||
                usuario.Contrasena_hash.Length < 8 ||
                !usuario.Contrasena_hash.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
            {
                ViewBag.Mensaje = "La contraseña debe tener mínimo 8 caracteres y al menos un carácter especial (!@#$%).";
                return View(usuario);
            }

            var helper = new Helper();
            usuario.Contrasena_hash = helper.Encrypt(usuario.Contrasena_hash);

            using (var context = _http.CreateClient())
            {
                var urlApi = _configuration["Valores:UrlAPI"] + "Home/Registro";
                var respuesta = context.PostAsJsonAsync(urlApi, usuario).Result;

                if (respuesta.IsSuccessStatusCode)
                {
                    var resultado = respuesta.Content.ReadFromJsonAsync<int>().Result;

                    // Escenario 1: registro exitoso
                    if (resultado == 1)
                    {
                        TempData["Mensaje"] = "¡Cuenta creada exitosamente! Ya puedes iniciar sesión.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Escenario 2: correo duplicado
                    if (resultado == 0)
                    {
                        ViewBag.Mensaje = "Este correo ya está registrado. Intenta con otro o inicia sesión.";
                        return View(usuario);
                    }
                }

                ViewBag.Mensaje = "No se pudo completar el registro. Intente más tarde.";
                return View(usuario);
            }
        }

        #endregion

        #region Actions de Recuperar Acceso (comentado)
        /*
        [HttpGet]
        public IActionResult RecuperarAcceso()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RecuperarAcceso(UsuarioModel usuario)
        {
            using (var context = _http.CreateClient())
            {
                var urlApi = _configuration["Valores:UrlAPI"] + "Home/ValidarUsuario?Correo=" + usuario.Correo;
                var respuesta = context.GetAsync(urlApi).Result;

                if (respuesta.IsSuccessStatusCode)
                {
                    var datosApi = respuesta.Content.ReadFromJsonAsync<UsuarioModel>().Result;

                    if (datosApi != null)
                        return RedirectToAction("Index", "Home");
                }

                ViewBag.Mensaje = "No se ha recuperado el acceso";
                return View();
            }
        }
        */
        #endregion

        [Seguridad]
        [HttpGet]
        public IActionResult Principal()
        {
            if (HttpContext.Session.GetInt32("ConsecutivoPerfil") == 1)
            {
                return RedirectToAction("Index", "CRM");
            }

            using var context = _http.CreateClient();
            var urlApi = _configuration["Valores:UrlAPI"] + "Usuario/ConsultarUsuarios";
            context.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("Token"));
            var respuesta = context.GetAsync(urlApi).Result;

            if (respuesta.IsSuccessStatusCode)
            {
                var datosApi = respuesta.Content.ReadFromJsonAsync<List<UsuarioModel>>().Result;
                return View(datosApi);
            }

            ViewBag.Mensaje = "No hay usuarios registrados";
            return View(new List<UsuarioModel>());
        }

        [Seguridad]
        [HttpGet]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            // REDIRIGE AL LANDING PAGE (Usuarios/Index) EN LUGAR DEL LOGIN
            return RedirectToAction("Index", "Usuarios");
        }

        #region HU-C-008 CAMBIAR CORREO

        [HttpPost]
        public IActionResult ActualizarCorreo(int idUsuario, string nuevoCorreo)
        {
            if (idUsuario == 0 || string.IsNullOrWhiteSpace(nuevoCorreo))
            {
                TempData["Error"] = "Datos incompletos.";
                return RedirectToAction("Index", "Inicio");
            }

            // Escenario 2: formato de correo inválido
            if (!ValidarCorreo(nuevoCorreo))
            {
                TempData["Error"] = "Formato de correo inválido.";
                return RedirectToAction("Index", "Inicio");
            }

            //Validar que el correo no sea el mismo
            var correoActual = ObtenerCorreo(idUsuario);
            if (correoActual != null && String.Equals(correoActual, nuevoCorreo, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "El correo nuevo no puede ser igual al actual.";
                return RedirectToAction("Index", "Inicio");
            }

            using var context = _http.CreateClient();

            var urlApi = _configuration["Valores:UrlAPI"] + $"Usuario/ActualizarCorreo?idUsuario={idUsuario}&nuevoCorreo={nuevoCorreo}";

            var respuesta = context.PutAsync(urlApi, null).Result;

            if (respuesta.IsSuccessStatusCode)
            {
                HttpContext.Session.SetString("CorreoUsuario", nuevoCorreo);
                TempData["Mensaje"] = "Correo actualizado correctamente.";
                return RedirectToAction("Index", "Inicio");
            }

            TempData["Error"] = "No se pudo actualizar el correo. Intente más tarde.";
            return RedirectToAction("Index", "Inicio");
        }

        static bool ValidarCorreo(string correo)
        {
            string patron = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
            return Regex.IsMatch(correo, patron);
        }

        private string? ObtenerCorreo(int idUsuario)
        {
            using var client = _http.CreateClient();
            var urlApi = _configuration["Valores:UrlAPI"] + $"Usuario/ObtenerCorreo?idUsuario={idUsuario}";
            var respuesta = client.GetAsync(urlApi).Result;

            if (respuesta.IsSuccessStatusCode)
                return respuesta.Content.ReadAsStringAsync().Result.Trim('"');

            return null;
        }
        #endregion

        #region HU-SA-003 MANTENER SESIÓN ACTIVA
        [HttpGet]
        public IActionResult RenovarSesion()
        {
            HttpContext.Session.SetString(
                "LastPing",
                DateTime.Now.ToString());

            return Ok();
        }
        #endregion

        #region HU-SA-004 CAMBIAR CONTRASEÑA
        [HttpPut]
        public IActionResult CambiarContrasenia(int idUsuario, string contraseniaActual, string nuevaContrasenia)
        {
            string contraseniaActualHash = _helper.Encrypt(contraseniaActual);

            using var context = _http.CreateClient();

            var urlComparar = _configuration["Valores:UrlAPI"] +
                $"Usuario/CompararContrasenia?idUsuario={idUsuario}&contraseniaActual={Uri.EscapeDataString(contraseniaActualHash)}";

            var respuestaComparar = context.GetAsync(urlComparar).Result;

            if (!respuestaComparar.IsSuccessStatusCode)
                return BadRequest("La contraseña actual es incorrecta.");

            string nuevaContraseniaHash = _helper.Encrypt(nuevaContrasenia);

            var urlActualizar = _configuration["Valores:UrlAPI"] +
                $"Usuario/ActualizarContrasenia?idUsuario={idUsuario}&nuevaContrasenia={Uri.EscapeDataString(nuevaContraseniaHash)}";

            var respuestaActualizar = context.PutAsync(urlActualizar, null).Result;

            if (respuestaActualizar.IsSuccessStatusCode)
                return Ok("Contraseña actualizada correctamente.");

            return BadRequest("No se pudo actualizar la contraseña.");
        }
        #endregion

        #region HU-C-010 Recuperar Contraseña
        [HttpGet]
        public IActionResult RecuperarAcceso() => View();

        [HttpPost]
        public IActionResult RecuperarAcceso(string correo)
        {
            using var client = _http.CreateClient();
            var urlApi = _configuration["Valores:UrlAPI"] +
                         $"Usuario/RecuperarContrasena?correo={correo}";

            var respuesta = client.PostAsync(urlApi, null).Result;

            var mensaje = respuesta.Content.ReadAsStringAsync().Result;

            if (respuesta.IsSuccessStatusCode)
            {
                return Ok(mensaje);
            }

            return BadRequest(mensaje);
        }

        [HttpGet]
        public IActionResult CambiarIdioma(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                });

            return LocalRedirect(returnUrl ?? "/");
        }

        [Seguridad]
        [HttpPut]
        public IActionResult ActualizarContraseniaForzada(int idUsuario, string nuevaContrasenia)
        {
            var nuevaContraseniaHash = _helper.Encrypt(nuevaContrasenia);

            using var client = _http.CreateClient();
            var urlApi = _configuration["Valores:UrlAPI"] +
                $"Usuario/ActualizarContrasenia?idUsuario={idUsuario}&nuevaContrasenia={Uri.EscapeDataString(nuevaContraseniaHash)}";

            var urlApi2 = _configuration["Valores:UrlAPI"] +
                $"Usuario/ActualizarDebeCambiarContrasena?idUsuario={idUsuario}";

            var respuesta = client.PutAsync(urlApi, null).Result;

            if (respuesta.IsSuccessStatusCode)
            {
                var respuesta2 = client.PutAsync(urlApi2, null).Result;

                if (respuesta2.IsSuccessStatusCode)
                {
                    HttpContext.Session.SetInt32("DebeCambiarContrasena", 0);
                    return Ok("Contraseña actualizada correctamente.");
                }
                else
                {
                    return BadRequest("No se pudo actualizar el estado de cambio de contraseña.");
                }

            }

            return BadRequest("No se pudo actualizar la contraseña.");
        }

        #endregion
    }
}