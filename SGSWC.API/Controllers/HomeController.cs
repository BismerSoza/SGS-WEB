using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using SGSWC.API.Models;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Utiles;

namespace SGSWC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly Helper _helper = new Helper();
        public HomeController(IConfiguration configuration, IHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;

        }
        [HttpPost]
        [Route("Registro")]
        public IActionResult Registro(RegistroUsuarioRequestModel usuario)
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@nombre", usuario.Nombre);
                parametros.Add("@correo", usuario.Correo);
                parametros.Add("@contrasena_hash", usuario.Contrasena_hash);

                var resultado = context.ExecuteScalar<int>("Registro", parametros,
                    commandType: System.Data.CommandType.StoredProcedure);
                return Ok(resultado);
            }
        }


        [HttpPost]
        [Route("ValidarSesion")]
        public IActionResult ValidarSesion(ValidarSesionRequestModel usuario)
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@correo", usuario.Correo);
                parametros.Add("@contrasena_hash", usuario.Contrasena_hash);

                var resultado = context.QueryFirstOrDefault<DatosUsuarioResponseModel>("ValidarSesion", parametros,
                    commandType: System.Data.CommandType.StoredProcedure);

                if (resultado != null)
                {
                    var pLogin = new DynamicParameters();
                    pLogin.Add("@id_usuario", resultado.Id_Usuario);
                    pLogin.Add("@correo", usuario.Correo);
                    pLogin.Add("@tipo_evento", "Login");
                    pLogin.Add("@exitoso", true);
                    pLogin.Add("@ip", (string?)null);
                    context.Execute("RegistrarAcceso", pLogin,
                        commandType: System.Data.CommandType.StoredProcedure);

                    resultado.Token = GenerarToken(resultado.Id_Usuario, resultado.Nombre, resultado.Id_Rol);
                    return Ok(resultado);
                }

                var pFallido = new DynamicParameters();
                pFallido.Add("@id_usuario", (int?)null);
                pFallido.Add("@correo", usuario.Correo);
                pFallido.Add("@tipo_evento", "Login fallido");
                pFallido.Add("@exitoso", false);
                pFallido.Add("@ip", (string?)null);
                context.Execute("RegistrarAcceso", pFallido,
                    commandType: System.Data.CommandType.StoredProcedure);

                return NotFound();
            }
        }

        [HttpGet]
        [Route("ValidarUsuario")]
        public IActionResult ValidarUsuario([Required] string CorreoElectronico)
        {
            using (var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]))
            {
                var helper = new Helper();
                var parametros = new DynamicParameters();
                parametros.Add("@correo", CorreoElectronico);
                var resultado = context.QueryFirstOrDefault<DatosUsuarioResponseModel>("ValidarUsuario", parametros);

                if (resultado != null)
                {
                    //Actualizar Contraseña
                    var ContrasennaGenerada = GenerarContrasenna();

                    var parametrosActualizar = new DynamicParameters();
                    parametrosActualizar.Add("@correo", resultado.Id_Usuario);
                    parametrosActualizar.Add("@contrasena_hash", helper.Encrypt(ContrasennaGenerada));
                    var resultadoActualizar = context.Execute("ActualizarContrasenna", parametrosActualizar);

                    if (resultadoActualizar > 0)
                    {
                        //Enviar Correo
                        var ruta = Path.Combine(_environment.ContentRootPath, "PlantillaCorreo.html");
                        var html = System.IO.File.ReadAllText(ruta, UTF8Encoding.UTF8);

                        html = html.Replace("{{Nombre}}", resultado.Nombre);
                        html = html.Replace("{{Contrasenna}}", ContrasennaGenerada);

                        EnviarCorreo("Recuperar Acceso", html, resultado.Correo);
                        return Ok(resultado);
                    }
                }

                return NotFound();
            }
        }

        private string GenerarToken(int usuarioId, string nombre, int rol)
        {
            var key = _configuration["Valores:KeyJWT"]!;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("IdUsuario", usuarioId.ToString()),
                new Claim("nombre", nombre),
                new Claim("rol", rol.ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void EnviarCorreo(string subject, string body, string destinatario)
        {
            var correoSMTP = _configuration["Valores:CorreoSMTP"]!;
            var contrasennaSMTP = _configuration["Valores:ContrasennaSMTP"]!;

            if (string.IsNullOrEmpty(contrasennaSMTP))
                return;

            var mensaje = new MailMessage
            {
                From = new MailAddress(correoSMTP),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mensaje.To.Add(destinatario);

            using var smtp = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(correoSMTP, contrasennaSMTP),
                EnableSsl = true
            };

            smtp.Send(mensaje);
        }

        private string GenerarContrasenna()
        {
            int longitud = 10;
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder resultado = new();

            using var rng = RandomNumberGenerator.Create();
            byte[] buffer = new byte[1];

            while (resultado.Length < longitud)
            {
                rng.GetBytes(buffer);
                int valor = buffer[0] % caracteres.Length;
                resultado.Append(caracteres[valor]);
            }

            return resultado.ToString();
        }
    }
}
