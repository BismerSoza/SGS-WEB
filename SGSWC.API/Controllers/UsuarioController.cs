using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGSWC.API.Models;
using System.Data;
using System.Text.Json;
using Utiles;

namespace SGSWC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public UsuarioController(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        #region Consultar Usuarios

        [Authorize]
        [HttpGet]
        [Route("ConsultarUsuarios")]
        public IActionResult ConsultarUsuarios([FromQuery] int id_usuario_sesion)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var parametros = new DynamicParameters();
            parametros.Add("@id_usuario_sesion", id_usuario_sesion);

            var resultado = context
                .Query<UsuarioResponseModel>("ConsultarUsuarios", parametros,
                    commandType: CommandType.StoredProcedure)
                .ToList();

            if (resultado.Any())
                return Ok(resultado);

            return NotFound();
        }

        #endregion

        #region Cambiar Estado Usuario

        [Authorize]
        [HttpPut]
        [Route("CambiarEstadoUsuario")]
        public IActionResult CambiarEstadoUsuario(CambiarEstadoUsuarioRequestModel request)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var parametros = new DynamicParameters();
            parametros.Add("@id_usuario", request.Id_Usuario);
            parametros.Add("@activo", request.Activo);
            parametros.Add("@id_usuario_sesion", request.Id_Usuario_Sesion);

            var resultado = context.Execute("CambiarEstadoUsuario", parametros,
                commandType: CommandType.StoredProcedure);

            if (resultado > 0)
                return Ok(resultado);

            return BadRequest();
        }

        #endregion

        #region HU-C-008 CAMBIAR CORREO

        [HttpGet]
        [Route("ObtenerCorreo")]
        public IActionResult ObtenerCorreo(int idUsuario)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();
            p.Add("@idUsuario", idUsuario);

            var resultado = context.QueryFirstOrDefault<string>(
                "SELECT correo FROM Usuarios WHERE id_usuario = @idUsuario",
                p
            );

            if (resultado == null) return NotFound("Usuario no encontrado");
            return Ok(JsonSerializer.Serialize(resultado));

        }


        #region HU-C-008 CAMBIAR CORREO

        //[HttpGet]
        //[Route("ObtenerCorreo")]
        //public IActionResult ObtenerCorreo(int idUsuario)
        //{
        //    using var context = new SqlConnection(
        //        _configuration["ConnectionStrings:BDConnection"]);

        //    var p = new DynamicParameters();
        //    p.Add("@idUsuario", idUsuario);

        //    var resultado = context.QueryFirstOrDefault<string>(
        //        "SELECT correo FROM Usuarios WHERE id_usuario = @idUsuario",
        //        p
        //    );

        //    if (resultado == null) return NotFound("Usuario no encontrado");
        //    return Ok(JsonSerializer.Serialize(resultado));

        //}


        [HttpPut]
        [Route("ActualizarCorreo")]
        public IActionResult ActualizarCorreo(int idUsuario, string nuevoCorreo)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();

            p.Add("@idUsuario", idUsuario);
            p.Add("@nuevoCorreo", nuevoCorreo);

            var resultado = context.Execute(
                 "UPDATE Usuarios SET correo = @nuevoCorreo WHERE id_usuario = @idUsuario",
                 p
             );

            if (resultado > 0) return Ok("Actualizado correctamente");
            return NotFound("Usuario no encontrado");
        }
        #endregion

        #endregion

        #region HU-C-009 CAMBIAR CONTRASEÑA
        [HttpGet]
        [Route("CompararContrasenia")]
        public IActionResult CompararContrasenia(int idUsuario, string contraseniaActual)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();
            p.Add("@idUsuario", idUsuario);
            p.Add("@contrasenia", contraseniaActual);

            var resultado = context.QueryFirstOrDefault<string>(
                "SELECT contrasena_hash FROM Usuarios WHERE id_usuario = @idUsuario",
                p
            );

            if (resultado == null) return NotFound("Usuario no encontrado");

            if (String.Equals(resultado, contraseniaActual))
            {
                return Ok("La contraseña es correcta");
            }

            return BadRequest("La contraseña es incorrecta");

        }

        [HttpPut]
        [Route("ActualizarContrasenia")]
        public IActionResult ActualizarContrasenia(int idUsuario, string nuevaContrasenia)
        {
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();
            p.Add("@idUsuario", idUsuario);
            p.Add("@nuevaContrasenia", nuevaContrasenia);

            var filas = context.Execute(
                "UPDATE Usuarios SET contrasena_hash = @nuevaContrasenia WHERE id_usuario = @idUsuario", p);

            if (filas > 0)
                return Ok("Contraseña actualizada.");

            return BadRequest("No se pudo actualizar.");
        }

        [HttpPut]
        [Route("ActualizarDebeCambiarContrasena")]
        public IActionResult ActualizarDebeCambiarContrasena(int idUsuario)
        {
            //si se llama a este metodo es porque el usuario ya tiene que cambiar la contrasenia obligatoriamente
            //por ello, si la cambia hay que cambiar el estado de la columna debe_cambiar_contrasena a 0,
            //para que no se le vuelva a pedir que la cambie
            using var context = new SqlConnection(
                _configuration["ConnectionStrings:BDConnection"]);

            var p = new DynamicParameters();
            p.Add("@idUsuario", idUsuario);
            p.Add("@debeCambiarContrasena", 0); //Asigna al valor BIT en 0 / false

            var filas = context.Execute(
                "UPDATE Usuarios SET debe_cambiar_contrasena = @debeCambiarContrasena WHERE id_usuario = @idUsuario", p);

            if (filas > 0)
                return Ok("Estado debe?actualizar?contrasena actualizado.");

            return BadRequest("Estado ebe?actualizar?contrasena no actualizado.");
        }

        #endregion

        #region HU-C-010 RECUPERAR CONTRASEÑA
        [HttpPost("RecuperarContrasena")]
        public IActionResult RecuperarContrasena(string correo)
        {
            using var context = new SqlConnection(_configuration["ConnectionStrings:BDConnection"]);

            var contrasenaTemporal = Helper.GenerarContrasenaTemporal();

            var helper = new Helper();
            var contrasenaHash = helper.Encrypt(contrasenaTemporal);

            var p = new DynamicParameters();
            p.Add("@correo", correo);
            p.Add("@nueva_contrasena", contrasenaHash);
            var resultado = context.ExecuteScalar<int>("RestablecerContrasenaTemporal", p,
                commandType: CommandType.StoredProcedure);

            //validacion en el lado del SP, si el resultado es 0, significa que el correo no esta registrado en la base de datos
            if (resultado == 0) return NotFound("Correo no registrado.");

            _emailService.EnviarContrasenaTemporal(correo, contrasenaTemporal);

            return Ok("Correo enviado correctamente.");
        }
        #endregion
    }

}