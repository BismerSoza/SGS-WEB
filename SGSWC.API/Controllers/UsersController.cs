using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGSWC.API.Models;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace SGSWC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UsersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #region HU-SA-005 Gestión de Usuarios

        [HttpGet("ConsultarUsuarios")]
        public IActionResult ConsultarUsuarios()
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var resultado = context.Query<GestionUsuarioResponseModel>("ConsultarUsuariosAdmin",
                commandType: CommandType.StoredProcedure).ToList();
            return Ok(resultado);
        }

        [HttpPost("ActualizarRol")]
        public IActionResult ActualizarRol(ActualizarRolRequestModel modelo)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
            var parametros = new DynamicParameters();
            parametros.Add("@id_usuario", modelo.IdUsuario);
            parametros.Add("@id_rol", modelo.Id_Rol);
            var resultado = context.ExecuteScalar<int>("ActualizarRol",
                parametros, commandType: CommandType.StoredProcedure);
            return Ok(resultado);
        }

        #endregion

        #region HU-SA-006 Historial de Accesos

        [Authorize]
        [HttpGet("HistorialAccesos")]
        public IActionResult HistorialAccesos(string? fechaInicio, string? fechaFin)
        {
            try
            {
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
                var parametros = new DynamicParameters();
                parametros.Add("@fecha_inicio", string.IsNullOrEmpty(fechaInicio) ? (DateTime?)null : DateTime.Parse(fechaInicio));
                parametros.Add("@fecha_fin", string.IsNullOrEmpty(fechaFin) ? (DateTime?)null : DateTime.Parse(fechaFin));
                var resultado = context.Query<HistorialAccesoResponseModel>(
                    "ConsultarHistorialAccesos", parametros,
                    commandType: CommandType.StoredProcedure).ToList();
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Error al cargar el historial de accesos.");
            }
        }

        [HttpPost("RegistrarAcceso")]
        public IActionResult RegistrarAcceso(RegistrarAccesoRequestModel modelo)
        {
            try
            {
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
                var parametros = new DynamicParameters();
                parametros.Add("@id_usuario", modelo.Id_Usuario);
                parametros.Add("@correo", modelo.Correo);
                parametros.Add("@tipo_evento", modelo.Tipo_Evento);
                parametros.Add("@exitoso", modelo.Exitoso);
                parametros.Add("@ip", modelo.Ip);
                context.Execute("RegistrarAcceso", parametros, commandType: CommandType.StoredProcedure);
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        #endregion

        #region HU-RE-003 Comparar Servicios

        [Authorize]
        [HttpGet("CompararServicios")]
        public IActionResult CompararServicios()
        {
            try
            {
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);
                var resultado = context.Query<ServicioComparadoResponseModel>(
                    "CompararServiciosSolicitados",
                    commandType: CommandType.StoredProcedure).ToList();
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Error al cargar los datos de servicios.");
            }
        }

        #endregion

        #region HU-C-012 Notificaciones

        [Authorize]
        [HttpPost("EnviarNotificacion")]
        public IActionResult EnviarNotificacion(NotificacionRequestModel modelo)
        {
            try
            {
                using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

                var notificacionesActivas = context.ExecuteScalar<bool>(
                    "SELECT notificaciones_activas FROM Usuarios WHERE id_usuario = @id",
                    new { id = modelo.Id_Usuario });

                if (!notificacionesActivas)
                    return Ok(new { enviado = false, mensaje = "El usuario tiene las notificaciones desactivadas." });

                var correoUsuario = context.ExecuteScalar<string>(
                    "SELECT correo FROM Usuarios WHERE id_usuario = @id",
                    new { id = modelo.Id_Usuario });

                if (string.IsNullOrEmpty(correoUsuario))
                    return NotFound("Usuario no encontrado.");

                string estadoEnvio = "Enviado";
                string? errorEnvio = null;

                try
                {
                    var smtpHost = _configuration["Valores:CorreoSMTP"]!;
                    var smtpPassword = _configuration["Valores:ContrasennaSMTP"]!;

                    var mail = new MailMessage
                    {
                        From = new MailAddress(smtpHost, "SGS Web Clean"),
                        Subject = modelo.Asunto,
                        Body = modelo.Mensaje,
                        IsBodyHtml = true
                    };
                    mail.To.Add(correoUsuario);

                    using var smtp = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(smtpHost, smtpPassword),
                        EnableSsl = true
                    };
                    smtp.Send(mail);
                }
                catch (Exception ex)
                {
                    estadoEnvio = "Error";
                    errorEnvio = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                }

                var p = new DynamicParameters();
                p.Add("@id_usuario", modelo.Id_Usuario);
                p.Add("@tipo", modelo.Tipo);
                p.Add("@asunto", modelo.Asunto);
                p.Add("@mensaje", modelo.Mensaje);
                p.Add("@estado_envio", estadoEnvio);
                p.Add("@error", errorEnvio);
                context.Execute("RegistrarNotificacion", p, commandType: CommandType.StoredProcedure);

                if (estadoEnvio == "Error")
                    return StatusCode(500, new { enviado = false, error = errorEnvio });

                return Ok(new { enviado = true });
            }
            catch
            {
                return StatusCode(500, "Error al procesar la notificación.");
            }
        }

        #endregion
    }
}